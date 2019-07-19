﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GroupMeClient.Extensions;
using GroupMeClientApi.Models;

namespace GroupMeClient.ViewModels.Controls
{
    public class GroupContentsControlViewModel : ViewModelBase, IDragDropTarget, IDisposable
    {
        public GroupContentsControlViewModel()
        {
            this.Messages = new ObservableCollection<MessageControlViewModel>();
            this.ReloadSem = new SemaphoreSlim(1, 1);
            this.SendMessage = new RelayCommand(async () => await this.SendMessageAsync(), true);
            this.ReloadView = new RelayCommand<ScrollViewer>(async (s) => await this.LoadMoreAsync(s), true);
            this.ClosePopup = new RelayCommand(this.ClosePopupHandler);
        }

        public GroupContentsControlViewModel(IMessageContainer messageContainer)
            : this()
        {
            this.MessageContainer = messageContainer;
            this.TopBarAvatar = new AvatarControlViewModel(this.MessageContainer, this.MessageContainer.Client.ImageDownloader);

            _ = this.Loaded();
        }

        private IMessageContainer messageContainer;
        private AvatarControlViewModel topBarAvatar;
        private string typedMessageContents;
        private SendImageControlViewModel imageSendDialog;

        public ICommand CloseGroup { get; set; }
        public ICommand SendMessage { get; set; }
        public ICommand ReloadView { get; set; }
        public ICommand ClosePopup { get; set; }

        public ObservableCollection<MessageControlViewModel> Messages { get; }

        private SemaphoreSlim ReloadSem { get; }

        public IMessageContainer MessageContainer
        {
            get { return this.messageContainer; }
            set { this.Set(() => this.MessageContainer, ref this.messageContainer, value); }
        }

        public string Title => this.MessageContainer.Name;

        public string Id => this.MessageContainer.Id;

        public AvatarControlViewModel TopBarAvatar
        {
            get { return this.topBarAvatar; }
            set { this.Set(() => this.TopBarAvatar, ref this.topBarAvatar, value); }
        }

        public string TypedMessageContents
        {
            get { return this.typedMessageContents; }
            set { this.Set(() => this.TypedMessageContents, ref this.typedMessageContents, value); }
        }

        public SendImageControlViewModel ImageSendDialog
        {
            get { return this.imageSendDialog; }
            set { this.Set(() => this.ImageSendDialog, ref this.imageSendDialog, value); }
        }

        private Message FirstDisplayedMessage { get; set; } = null;

        public async Task LoadNewMessages()
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                // the code that's accessing UI properties
                await this.LoadMoreAsync(null, true);
            });
        }

        public void UpdateMessageLikes(Message message)
        {
            var msgVm = this.Messages.FirstOrDefault(m => m.Id == message.Id);

            // Only update the display copy and leave the cached copy alone
            // Cached copy is never used for displaying 'Like' status
            msgVm.Message.FavoritedBy.Clear();
            foreach (var liker in message.FavoritedBy)
            {
                msgVm.Message.FavoritedBy.Add(liker);
            }

            msgVm.UpdateDisplay();
        }

        private async Task Loaded()
        {
            await this.LoadMoreAsync();
        }

        private async Task LoadMoreAsync(ScrollViewer scrollViewer = null, bool updateNewest = false)
        {
            await this.ReloadSem.WaitAsync();

            try
            {
                ICollection<Message> results;

                if (this.FirstDisplayedMessage == null || updateNewest)
                {
                    // load the most recent messages
                    results = await this.MessageContainer.GetMessagesAsync();
                }
                else
                {
                    // load the 20 (GroupMe default) messages preceeding the oldest (first) one displayed
                    results = await this.MessageContainer.GetMessagesAsync(GroupMeClientApi.MessageRetreiveMode.BeforeId, this.FirstDisplayedMessage.Id);
                }

                this.UpdateDisplay(scrollViewer, results);
            }
            finally
            {
                this.ReloadSem.Release();
            }
        }

        private void UpdateDisplay(ScrollViewer scrollViewer, ICollection<Message> messages)
        {
            double originalHeight = scrollViewer?.ExtentHeight ?? 0.0;
            if (originalHeight != 0)
            {
                // prevent the At Top event from firing while we are adding new messages
                scrollViewer.ScrollToVerticalOffset(1);
            }

            foreach (var msg in messages)
            {
                var oldMsg = this.Messages.FirstOrDefault(m => m.Id == msg.Id);

                if (oldMsg == null)
                {
                    // add new message
                    this.Messages.Add(new MessageControlViewModel(msg));
                }
                else
                {
                    // update and existing one if needed
                    oldMsg.Message = msg;
                }
            }

            if (originalHeight != 0)
            {
                // Calculate the offset where the last message the user was looking at is
                // Scroll back to there so new messages appear on top, above screen
                scrollViewer.UpdateLayout();
                double newHeight = scrollViewer?.ExtentHeight ?? 0.0;
                double difference = newHeight - originalHeight;

                scrollViewer.ScrollToVerticalOffset(difference);
            }

            if (messages.Count > 0)
            {
                this.FirstDisplayedMessage = messages.Last();
            }
        }

        private async Task SendMessageAsync()
        {
            var newMessage = Message.CreateMessage(this.TypedMessageContents);
            await this.SendMessageAsync(newMessage);
        }

        private async Task SendImageMessageAsync()
        {
            this.ImageSendDialog.IsSending = true;

            var contents = this.ImageSendDialog.TypedMessageContents;
            byte[] image;

            using (var ms = new MemoryStream())
            {
                this.ImageSendDialog.ImageStream.Seek(0, SeekOrigin.Begin);
                await this.ImageSendDialog.ImageStream.CopyToAsync(ms);
                image = ms.ToArray();
            }

            GroupMeClientApi.Models.Attachments.ImageAttachment attachment;
            attachment = await GroupMeClientApi.Models.Attachments.ImageAttachment.CreateImageAttachment(image, this.MessageContainer);

            var attachmentsList = new List<GroupMeClientApi.Models.Attachments.Attachment> { attachment };

            var message = Message.CreateMessage(contents, attachmentsList);
            await this.SendMessageAsync(message);

            this.ClosePopupHandler();
        }

        private async Task<bool> SendMessageAsync(Message newMessage)
        {
            this.TypedMessageContents = string.Empty;

            bool success;

            success = await this.MessageContainer.SendMessage(newMessage);
            if (success)
            {
                this.MessageContainer.Messages.Add(newMessage);
                await this.LoadMoreAsync(null, true);
            }

            return success;
        }

        private void ShowImageSendDialog(byte[] image)
        {
            var ms = new MemoryStream(image);
            this.ShowImageSendDialog(ms);
        }

        private void ShowImageSendDialog(Stream image)
        {
            var dialog = new SendImageControlViewModel()
            {
                ImageStream = image,
                TypedMessageContents = this.TypedMessageContents,
                SendMessage = new RelayCommand(async () => await this.SendImageMessageAsync(), true),
            };

            this.ImageSendDialog = dialog;
        }

        private void ClosePopupHandler()
        {
            (this.ImageSendDialog as IDisposable)?.Dispose();
            this.ImageSendDialog = null;
        }

        void IDisposable.Dispose()
        {
            this.Messages.Clear();
            //foreach (var msg in this.Messages)
            //{
            //    (msg as IDisposable)?.Dispose();
            //}
        }

        void IDragDropTarget.OnFileDrop(string[] filepaths)
        {
            string[] supportedExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp" };

            foreach (var file in filepaths)
            {
                if (supportedExtensions.Contains(Path.GetExtension(file).ToLower()))
                {
                    this.ShowImageSendDialog(File.OpenRead(file));
                    break;
                }
            }
        }

        void IDragDropTarget.OnImageDrop(byte[] image)
        {
            var memoryStream = new MemoryStream(image);
            this.ShowImageSendDialog(memoryStream);
        }
    }
}