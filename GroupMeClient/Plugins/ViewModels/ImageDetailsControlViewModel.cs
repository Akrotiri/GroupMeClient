﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GroupMeClient.Plugins.Views;
using GroupMeClient.ViewModels.Controls;
using GroupMeClientApi;
using GroupMeClientApi.Models;

namespace GroupMeClient.Plugins.ViewModels
{
    /// <summary>
    /// <see cref="ImageDetailsControlViewModel"/> provides a ViewModel for the <see cref="ImageDetailsControl"/> control.
    /// </summary>
    public class ImageDetailsControlViewModel : ViewModelBase
    {
        private bool isLoading;
        private Stream imageData;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageDetailsControlViewModel"/> class.
        /// </summary>
        /// <param name="message">The <see cref="Message"/> containing the image to be shown.</param>
        /// <param name="imageIndex">The index of this image attachment to display.</param>
        /// <param name="downloader">The <see cref="GroupMeClientApi.ImageDownloader"/> that should be used to download images.</param>
        public ImageDetailsControlViewModel(Message message, int imageIndex, ImageDownloader downloader)
        {
            this.Message = message;
            this.ImageDownloader = downloader;
            this.ImageIndex = imageIndex;

            this.Clicked = new RelayCommand(this.ClickedAction);

            this.SenderAvatar = new AvatarControlViewModel(this.Message, this.ImageDownloader);
            this.ImageUrl = ImageGalleryWindowViewModel.GetAttachmentContentUrls(this.Message.Attachments)[this.ImageIndex];

            _ = this.LoadImage();
        }

        /// <summary>
        /// Gets the <see cref="Message"/> containing the displayed image.
        /// </summary>
        public Message Message { get; }

        /// <summary>
        /// Gets the <see cref="AvatarControlViewModel"/> for the user who sent this image.
        /// </summary>
        public AvatarControlViewModel SenderAvatar { get; }

        /// <summary>
        /// Gets the command to be performed when the image is clicked.
        /// </summary>
        public ICommand Clicked { get; }

        /// <summary>
        /// Gets a stream containing the image data to display.
        /// </summary>
        public Stream ImageData
        {
            get => this.imageData;
            private set => this.Set(() => this.ImageData, ref this.imageData, value);
        }

        /// <summary>
        /// Gets a value indicating whether the image is still loading.
        /// </summary>
        public bool IsLoading
        {
            get => this.isLoading;
            private set => this.Set(() => this.IsLoading, ref this.isLoading, value);
        }

        private ImageDownloader ImageDownloader { get; }

        private int ImageIndex { get; }

        private string ImageUrl { get; }

        private async Task LoadImage()
        {
            this.IsLoading = true;

            var image = await this.ImageDownloader.DownloadPostImageAsync(this.ImageUrl);

            if (image == null)
            {
                return;
            }

            this.ImageData = new MemoryStream(image);

            this.IsLoading = false;
        }

        private void ClickedAction()
        {
            var vm = new ViewImageControlViewModel(this.ImageUrl, this.ImageDownloader);

            var request = new Messaging.DialogRequestMessage(vm);
            Messenger.Default.Send(request);
        }
    }
}
