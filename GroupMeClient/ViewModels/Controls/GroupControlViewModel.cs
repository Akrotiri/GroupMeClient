﻿using System;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GroupMeClientApi.Models;

namespace GroupMeClient.ViewModels.Controls
{
    public class GroupControlViewModel : ViewModelBase
    {
        public GroupControlViewModel()
        {
        }

        public GroupControlViewModel(IMessageContainer messageContainer)
        {
            this.MessageContainer = messageContainer;
            this.Avatar = new AvatarControlViewModel(this.MessageContainer, this.MessageContainer.Client.ImageDownloader);
        }

        private IMessageContainer messageContainer;
        private AvatarControlViewModel avatar;

        public ICommand GroupSelected { get; set; }

        public IMessageContainer MessageContainer
        {
            get { return this.messageContainer; }
            set
            {
                this.Set(() => this.MessageContainer, ref this.messageContainer, value);
                this.RaisePropertyChangeForAll();
            }
        }

        public AvatarControlViewModel Avatar
        {
            get { return this.avatar; }
            set { this.Set(() => this.Avatar, ref this.avatar, value); }
        }

        public string LastUpdatedFriendlyTime
        {
            get
            {
                var updatedAtTime = this.LastUpdated;

                var elapsedTime = DateTime.Now.Subtract(updatedAtTime).Duration();
                if (elapsedTime < TimeSpan.FromDays(1))
                {
                    return updatedAtTime.ToShortTimeString();
                }
                else
                {
                    return updatedAtTime.ToString("MMM d");
                }
            }
        }

        public string QuickPreview
        {
            get
            {
                var latestPreviewMessage = this.MessageContainer.LatestMessage;

                var sender = latestPreviewMessage.Name;
                var attachments = latestPreviewMessage.Attachments;
                var message = latestPreviewMessage.Text;

                bool wasImageSent = false;
                foreach (var attachment in attachments)
                {
                    if (attachment.GetType() == typeof(GroupMeClientApi.Models.Attachments.ImageAttachment))
                    {
                        wasImageSent = true;
                    }
                }

                if (wasImageSent)
                {
                    return $"{sender} shared an picture";
                }
                else
                {
                    return $"{sender}: {message}";
                }
            }
        }

        public string Title => this.MessageContainer.Name;

        public DateTime LastUpdated => this.MessageContainer.UpdatedAtTime;

        public string Id => this.MessageContainer.Id;

        private void RaisePropertyChangeForAll()
        {
            // since RaisePropertyChanged(string.empty) doesn't seem to work correctly...
            this.RaisePropertyChanged(nameof(this.Avatar));
            this.RaisePropertyChanged(nameof(this.LastUpdatedFriendlyTime));
            this.RaisePropertyChanged(nameof(this.QuickPreview));
            this.RaisePropertyChanged(nameof(this.Title));
            this.RaisePropertyChanged(nameof(this.LastUpdated));
        }
    }
}