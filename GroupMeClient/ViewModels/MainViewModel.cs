﻿using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GroupMeClient.Notifications.Display;
using GroupMeClient.Plugins;
using GroupMeClient.Updates;
using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;

namespace GroupMeClient.ViewModels
{
    /// <summary>
    /// <see cref="MainViewModel"/> is the top-level ViewModel for the GroupMe Desktop Client.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private HamburgerMenuItemCollection menuItems = new HamburgerMenuItemCollection();
        private HamburgerMenuItemCollection menuOptionItems = new HamburgerMenuItemCollection();
        private HamburgerMenuItem selectedItem;
        private ViewModelBase popupDialog;
        private int unreadCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        public MainViewModel()
        {
            this.InitializeClient();
        }

        /// <summary>
        /// Gets or sets the list of main items shown in the hamburger menu.
        /// </summary>
        public HamburgerMenuItemCollection MenuItems
        {
            get { return this.menuItems; }
            set { this.Set(() => this.MenuItems, ref this.menuItems, value); }
        }

        /// <summary>
        /// Gets or sets the list of options items shown in the hamburger menu (at the bottom).
        /// </summary>
        public HamburgerMenuItemCollection MenuOptionItems
        {
            get { return this.menuOptionItems; }
            set { this.Set(() => this.MenuOptionItems, ref this.menuOptionItems, value); }
        }

        /// <summary>
        /// Gets or sets the currently selected Hamburger Menu Tab.
        /// </summary>
        public HamburgerMenuItem SelectedItem
        {
            get { return this.selectedItem; }
            set { this.Set(() => this.SelectedItem, ref this.selectedItem, value); }
        }

        /// <summary>
        /// Gets or sets the Popup Dialog that should be displayed.
        /// Null specifies that no popup is shown.
        /// </summary>
        public ViewModelBase PopupDialog
        {
            get { return this.popupDialog; }
            set { this.Set(() => this.PopupDialog, ref this.popupDialog, value); }
        }

        /// <summary>
        /// Gets or sets the action to be be performed when the big popup has been closed.
        /// </summary>
        public ICommand ClosePopup { get; set; }

        /// <summary>
        /// Gets or sets the number of unread notifications that should be displayed in the
        /// taskbar badge.
        /// </summary>
        public int UnreadCount
        {
            get { return this.unreadCount; }
            set { this.Set(() => this.UnreadCount, ref this.unreadCount, value); }
        }

        /// <summary>
        /// Gets or sets the action to be be performed when the big popup has been closed indirectly.
        /// This typically is from the user clicking in the gray area around the popup to dismiss it.
        /// </summary>
        public ICommand EasyClosePopup { get; set; }

        private string DataRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MicroCube", "GroupMe Desktop Client");

        private string SettingsPath => Path.Combine(this.DataRoot, "settings.json");

        private string CachePath => Path.Combine(this.DataRoot, "cache.db");

        private string ImageCachePath => Path.Combine(this.DataRoot, "ImageCache");

        private string PluginsPath => Path.Combine(this.DataRoot, "Plugins");

        private GroupMeClientApi.GroupMeClient GroupMeClient { get; set; }

        private Caching.CacheContext CacheContext { get; set; }

        private Settings.SettingsManager SettingsManager { get; set; }

        private NotificationRouter NotificationRouter { get; set; }

        private UpdateAssist UpdateAssist { get; set; }

        private ChatsViewModel ChatsViewModel { get; set; }

        private SearchViewModel SearchViewModel { get; set; }

        private SettingsViewModel SettingsViewModel { get; set; }

        private LoginViewModel LoginViewModel { get; set; }

        private ProgressRing ReconnectingSpinner { get; } = new ProgressRing() { IsActive = false, Width = 20, Foreground = System.Windows.Media.Brushes.White };

        private int DisconnectedComponentCount { get; set; }

        private void InitializeClient()
        {
            Directory.CreateDirectory(this.DataRoot);

            this.SettingsManager = new Settings.SettingsManager(this.SettingsPath);
            this.SettingsManager.LoadSettings();

            PluginManager.Instance.LoadPlugins(this.PluginsPath);

            Messenger.Default.Register<Messaging.UnreadRequestMessage>(this, this.UpdateNotificationCount);
            Messenger.Default.Register<Messaging.DisconnectedRequestMessage>(this, this.UpdateDisconnectedComponentsCount);
            Messenger.Default.Register<Messaging.IndexAndRunPluginRequestMessage>(this, this.IndexAndRunCommand);

            if (string.IsNullOrEmpty(this.SettingsManager.CoreSettings.AuthToken))
            {
                // Startup in Login Mode
                this.LoginViewModel = new LoginViewModel(this.SettingsManager)
                {
                    LoginCompleted = new RelayCommand(this.InitializeClient),
                };

                this.CreateMenuItemsLoginOnly();
            }
            else
            {
                // Startup Regularly
                this.GroupMeClient = new GroupMeClientApi.GroupMeClient(this.SettingsManager.CoreSettings.AuthToken);
                this.CacheContext = new Caching.CacheContext(this.CachePath);
                this.GroupMeClient.ImageDownloader = new GroupMeClientApi.CachedImageDownloader(this.ImageCachePath);

                this.NotificationRouter = new NotificationRouter(this.GroupMeClient);

                this.ChatsViewModel = new ChatsViewModel(this.GroupMeClient, this.SettingsManager);
                this.SearchViewModel = new SearchViewModel(this.GroupMeClient, this.CacheContext);
                this.SettingsViewModel = new SettingsViewModel(this.SettingsManager);

                this.RegisterNotifications();

                this.CreateMenuItemsRegular();
            }

            Messenger.Default.Register<Messaging.DialogRequestMessage>(this, this.OpenBigPopup);
            this.ClosePopup = new RelayCommand(this.CloseBigPopup);
            this.EasyClosePopup = new RelayCommand(this.CloseBigPopup);

            this.UpdateAssist = new UpdateAssist();
            Application.Current.MainWindow.Closing += new CancelEventHandler(this.MainWindow_Closing);
        }

        private void RegisterNotifications()
        {
            this.NotificationRouter.RegisterNewSubscriber(this.ChatsViewModel);
            this.NotificationRouter.RegisterNewSubscriber(PopupNotificationProvider.CreatePlatformNotificationProvider());
            this.NotificationRouter.RegisterNewSubscriber(PopupNotificationProvider.CreateInternalNotificationProvider());
        }

        private void CreateMenuItemsRegular()
        {
            var chatsTab = new HamburgerMenuIconItem()
            {
                Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.MessageText },
                Label = "Chats",
                ToolTip = "View Groups and Chats.",
                Tag = this.ChatsViewModel,
            };

            var secondTab = new HamburgerMenuIconItem()
            {
                Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.EmailSearch },
                Label = "Search",
                ToolTip = "Search all Groups and Chats.",
                Tag = this.SearchViewModel,
            };

            var settingsTab = new HamburgerMenuIconItem()
            {
                Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.SettingsOutline },
                Label = "Settings",
                ToolTip = "GroupMe Settings",
                Tag = this.SettingsViewModel,
            };

            var loadingSpinner = new HamburgerMenuIconItem()
            {
                Icon = this.ReconnectingSpinner,
                Label = "Loading...",
                ToolTip = "Loading...",
                Tag = null,
                IsEnabled = false,
            };

            // Add new Tabs
            this.MenuItems.Add(chatsTab);
            this.MenuItems.Add(secondTab);

            this.MenuItems.Add(loadingSpinner);

            // Add new Options
            this.MenuOptionItems.Add(settingsTab);

            // Set the section to the Chats tab
            this.SelectedItem = chatsTab;

            // Remove the old Tabs and Options AFTER the new one has been bound
            // There should be a better way to do this...
            var newTopOptionIndex = this.MenuOptionItems.IndexOf(settingsTab);
            for (int i = 0; i < newTopOptionIndex; i++)
            {
                this.MenuOptionItems.RemoveAt(0);
            }

            var newTopIndex = this.MenuItems.IndexOf(chatsTab);
            for (int i = 0; i < newTopIndex; i++)
            {
                this.MenuItems.RemoveAt(0);
            }
        }

        private void CreateMenuItemsLoginOnly()
        {
            this.MenuItems = new HamburgerMenuItemCollection
            {
                new HamburgerMenuIconItem()
                {
                    Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.Login },
                    Label = "Login",
                    ToolTip = "Login To GroupMe",
                    Tag = this.LoginViewModel,
                },
            };

            this.MenuOptionItems = new HamburgerMenuItemCollection();

            this.SelectedItem = this.MenuItems[0];
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!this.UpdateAssist.CanShutdown)
            {
                this.UpdateAssist.UpdateMonitor.Task.ContinueWith(this.UpdateCompleted);
                e.Cancel = true;

                var updatingTab = new HamburgerMenuIconItem()
                {
                    Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.Loading },
                    Label = "Updating",
                    ToolTip = "Updating",
                    Tag = new UpdatingViewModel(),
                };

                this.MenuItems.Add(updatingTab);
                this.SelectedItem = updatingTab;
            }
        }

        private void UpdateCompleted(Task<bool> result)
        {
            // safe to shutdown now.
            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.MainWindow.Close();
            });
        }

        private void OpenBigPopup(Messaging.DialogRequestMessage dialog)
        {
            this.PopupDialog = dialog.Dialog;
        }

        private void CloseBigPopup()
        {
            if (this.PopupDialog is IDisposable d)
            {
                d.Dispose();
            }

            this.PopupDialog = null;
        }

        private void UpdateNotificationCount(Messaging.UnreadRequestMessage update)
        {
            this.UnreadCount = update.Count;
        }

        private void UpdateDisconnectedComponentsCount(Messaging.DisconnectedRequestMessage update)
        {
            this.DisconnectedComponentCount += update.Disconnected ? 1 : -1;
            this.DisconnectedComponentCount = Math.Max(this.DisconnectedComponentCount, 0); // make sure it never goes negative
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.ReconnectingSpinner.IsActive = this.DisconnectedComponentCount > 0;
            });
        }

        private void IndexAndRunCommand(Messaging.IndexAndRunPluginRequestMessage cmd)
        {
            this.SearchViewModel.ActivatePluginOnLoad = cmd.Plugin;
            this.SearchViewModel.ActivatePluginForGroupOnLoad = cmd.MessageContainer;

            // Find Search Tab entry and set as active
            foreach (var menuItem in this.MenuItems)
            {
                if (menuItem.Tag == this.SearchViewModel)
                {
                    this.SelectedItem = menuItem;
                    break;
                }
            }
        }
    }
}