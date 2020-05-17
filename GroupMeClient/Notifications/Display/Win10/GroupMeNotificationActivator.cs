﻿using System;
using System.Runtime.InteropServices;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp.Notifications;

namespace GroupMeClient.Notifications.Display.Win10
{
    /// <summary>
    /// Provides a COM Interface to support activation when a user clicks on a Windows 10 Toast Notfication.
    /// </summary>
    /// <remarks>
    /// Squirrel automatically generates a CLSID based on the NuGet package name.
    /// For 'GroupMeDesktopClient', this generated GUID is '3d1bf80b-078b-5aee-b9a0-fc40af7fc030'.
    /// </remarks>
    [ClassInterface(ClassInterfaceType.None)]
    [ComSourceInterfaces(typeof(INotificationActivationCallback))]
    [ComVisible(true)]
    [Guid("3d1bf80b-078b-5aee-b9a0-fc40af7fc030")]
    public class GroupMeNotificationActivator : NotificationActivator
    {
        /// <inheritdoc/>
        public override void OnActivated(string invokedArgs, NotificationUserInput userInput, string appUserModelId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (invokedArgs.Length == 0)
                {
                    // Perform a normal launch
                    this.OpenWindowIfNeeded();
                }

                var args = QueryString.Parse(invokedArgs);
                var action = (Win10ToastNotificationsProvider.LaunchActions)Enum.Parse(typeof(Win10ToastNotificationsProvider.LaunchActions), args["action"]);

                switch (action)
                {
                    case Win10ToastNotificationsProvider.LaunchActions.ShowGroup:
                        this.OpenWindowIfNeeded();
                        var command = new Messaging.ShowChatRequestMessage(args["conversationId"]);
                        Messenger.Default.Send(command);
                        break;

                    case Win10ToastNotificationsProvider.LaunchActions.LikeMessage:
                        break;
                }
            });
        }

        private void OpenWindowIfNeeded()
        {
            // Make sure we have a window open (in case user clicked toast while app closed)
            if (App.Current.Windows.Count == 0)
            {
                new MainWindow().Show();
            }

            // Activate the window, bringing it to focus
            App.Current.Windows[0].Activate();

            // And make sure to maximize the window too, in case it was currently minimized
            App.Current.Windows[0].WindowState = WindowState.Normal;
        }
    }
}
