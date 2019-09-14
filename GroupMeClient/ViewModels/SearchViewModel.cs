﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GroupMeClient.ViewModels.Controls;
using GroupMeClientApi.Models;
using GroupMeClientPlugin.GroupChat;
using Microsoft.EntityFrameworkCore;

namespace GroupMeClient.ViewModels
{
    /// <summary>
    /// <see cref="SearchViewModel"/> provides a ViewModel for the <see cref="Controls.SearchView"/> view.
    /// </summary>
    public class SearchViewModel : ViewModelBase, GroupMeClientPlugin.GroupChat.ICachePluginUIIntegration
    {
        private ViewModelBase popupDialog;
        private string searchTerm = string.Empty;
        private string selectedGroupName = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchViewModel"/> class.
        /// </summary>
        /// <param name="groupMeClient">The client to use.</param>
        /// <param name="cacheContext">The cache database to use.</param>
        public SearchViewModel(GroupMeClientApi.GroupMeClient groupMeClient, Caching.CacheContext cacheContext)
        {
            this.GroupMeClient = groupMeClient ?? throw new System.ArgumentNullException(nameof(groupMeClient));
            this.CacheContext = cacheContext ?? throw new ArgumentNullException(nameof(cacheContext));

            this.AllGroupsChats = new ObservableCollection<GroupControlViewModel>();

            this.ResultsView = new PaginatedMessagesControlViewModel()
            {
                MessageSelectedCommand = new RelayCommand<MessageControlViewModelBase>(this.MessageSelected),
                ShowLikers = false,
                NewestAtBottom = false,
            };

            this.ContextView = new PaginatedMessagesControlViewModel()
            {
                ShowTitle = false,
                ShowLikers = true,
                SyncAndUpdate = true,
                NewestAtBottom = true,
            };

            this.ClosePopup = new RelayCommand(this.CloseLittlePopup);
            this.EasyClosePopup = new RelayCommand(this.CloseLittlePopup);

            this.GroupChatCachePlugins = new ObservableCollection<GroupMeClientPlugin.GroupChat.IGroupChatCachePlugin>();
            this.GroupChatCachePluginActivated =
                new RelayCommand<GroupMeClientPlugin.GroupChat.IGroupChatCachePlugin>(this.ActivateGroupPlugin);

            foreach (var plugin in Plugins.PluginManager.Instance.GroupChatCachePlugins)
            {
                this.GroupChatCachePlugins.Add(plugin);
            }

            this.Loaded = new RelayCommand(this.StartIndexing);
        }

        /// <summary>
        /// Gets the action that should be executed when the search page loads.
        /// </summary>
        public ICommand Loaded { get; private set; }

        /// <summary>
        /// Gets a listing of all available Groups and Chats.
        /// </summary>
        public ObservableCollection<GroupControlViewModel> AllGroupsChats { get; }

        /// <summary>
        /// Gets the action to be be performed when the big popup has been closed.
        /// </summary>
        public ICommand ClosePopup { get; }

        /// <summary>
        /// Gets the action to be be performed when the big popup has been closed indirectly.
        /// This typically is from the user clicking in the gray area around the popup to dismiss it.
        /// </summary>
        public ICommand EasyClosePopup { get; }

        /// <summary>
        /// Gets the ViewModel for the paginated search results.
        /// </summary>
        public PaginatedMessagesControlViewModel ResultsView { get; }

        /// <summary>
        /// Gets the ViewModel for the in-context message view.
        /// </summary>
        public PaginatedMessagesControlViewModel ContextView { get; }

        /// <summary>
        /// Gets the collection of ViewModels for <see cref="Message"/>s to be displayed.
        /// </summary>
        public ObservableCollection<GroupMeClientPlugin.GroupChat.IGroupChatCachePlugin> GroupChatCachePlugins { get; }

        /// <summary>
        /// Gets the action to be performed when a Plugin in the
        /// Options Menu is activated.
        /// </summary>
        public ICommand GroupChatCachePluginActivated { get; }

        /// <summary>
        /// Gets or sets the plugin that should be automatically executed when indexing is complete.
        /// This property is only used for UI-automation tasks. If null, the UI will be displayed normally
        /// when loading is complete. If a plugin is specified, the group specified in <see cref="ActivatePluginForGroupOnLoad"/>
        /// will be used as a parameter.
        /// </summary>
        public IGroupChatCachePlugin ActivatePluginOnLoad { get; set; }

        /// <summary>
        /// Gets or sets a value indicating which group should be passed as a parameter to an automatically executed
        /// plugin. See <see cref="ActivatePluginOnLoad"/> for more information.
        /// </summary>
        public IMessageContainer ActivatePluginForGroupOnLoad { get; set; }

        /// <summary>
        /// Gets the Big Dialog that should be displayed as a popup.
        /// Gets null if no dialog should be displayed.
        /// </summary>
        public ViewModelBase PopupDialog
        {
            get { return this.popupDialog; }
            private set { this.Set(() => this.PopupDialog, ref this.popupDialog, value); }
        }

        /// <summary>
        /// Gets or sets the string entered to search for.
        /// </summary>
        public string SearchTerm
        {
            get
            {
                return this.searchTerm;
            }

            set
            {
                this.Set(() => this.SearchTerm, ref this.searchTerm, value);
                this.UpdateSearchResults();
            }
        }

        /// <summary>
        /// Gets the name of the selected group.
        /// </summary>
        public string SelectedGroupName
        {
            get { return this.selectedGroupName; }
            private set { this.Set(() => this.SelectedGroupName, ref this.selectedGroupName, value); }
        }

        private GroupMeClientApi.GroupMeClient GroupMeClient { get; }

        private Caching.CacheContext CacheContext { get; }

        private IMessageContainer SelectedGroupChat { get; set; }

        private Task IndexingTask { get; set; }

        private CancellationTokenSource CancellationTokenSource { get; set; }

        /// <inheritdoc/>
        void ICachePluginUIIntegration.GotoContextView(Message message, IMessageContainer container)
        {
            this.OpenNewGroupChat(container);
            this.UpdateContextView(message);
        }

        private void StartIndexing()
        {
            if (this.IndexingTask != null && !(this.IndexingTask.IsCompleted || this.IndexingTask.IsCanceled))
            {
                // handle cancellation and restart
                this.CancellationTokenSource.Cancel();
                this.IndexingTask.ContinueWith(async (l) =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(() => this.StartIndexing());
                });
                return;
            }

            this.CancellationTokenSource = new CancellationTokenSource();
            this.IndexingTask = this.IndexGroups();
        }

        private async Task IndexGroups()
        {
            var loadingDialog = new LoadingControlViewModel();
            this.PopupDialog = loadingDialog;

            var groups = await this.GroupMeClient.GetGroupsAsync();
            var chats = await this.GroupMeClient.GetChatsAsync();
            var groupsAndChats = Enumerable.Concat<IMessageContainer>(groups, chats);

            this.AllGroupsChats.Clear();

            foreach (var group in groupsAndChats)
            {
                this.CancellationTokenSource.Token.ThrowIfCancellationRequested();

                if (this.ActivatePluginForGroupOnLoad != null && this.ActivatePluginOnLoad != null)
                {
                    // if a plugin is set to automatically execute for only a single group,
                    // index only that group to improve performance
                    if (this.ActivatePluginForGroupOnLoad.Id != group.Id)
                    {
                        continue;
                    }
                }

                loadingDialog.Message = $"Indexing {group.Name}";
                await this.IndexGroup(group);

                // Add Group/Chat to the list
                var vm = new GroupControlViewModel(group)
                {
                    GroupSelected = new RelayCommand<GroupControlViewModel>((s) => this.OpenNewGroupChat(s.MessageContainer), (g) => true, true),
                };
                this.AllGroupsChats.Add(vm);
            }

            this.PopupDialog = null;

            // Check to see if a plugin should be automatically executed.
            if (this.ActivatePluginForGroupOnLoad != null && this.ActivatePluginOnLoad != null)
            {
                var cache = this.GetMessagesForGroup(this.ActivatePluginForGroupOnLoad);
                _ = this.ActivatePluginOnLoad.Activated(this.ActivatePluginForGroupOnLoad, cache, this);

                this.ActivatePluginForGroupOnLoad = null;
                this.ActivatePluginOnLoad = null;
            }
        }

        private async Task IndexGroup(IMessageContainer container)
        {
            var groupState = this.CacheContext.IndexStatus.Find(container.Id);

            if (groupState == null)
            {
                groupState = new Caching.CacheContext.GroupIndexStatus()
                {
                    Id = container.Id,
                };
                this.CacheContext.IndexStatus.Add(groupState);
            }

            var newestMessages = await container.GetMessagesAsync();
            this.CacheContext.AddMessages(newestMessages);

            long.TryParse(groupState.LastIndexedId, out var lastIndexId);
            long.TryParse(newestMessages.Last().Id, out var retreiveFrom);

            while (lastIndexId < retreiveFrom && !this.CancellationTokenSource.IsCancellationRequested)
            {
                // not up-to-date, we need to retreive the delta
                var results = await container.GetMaxMessagesAsync(GroupMeClientApi.MessageRetreiveMode.BeforeId, retreiveFrom.ToString());
                this.CacheContext.AddMessages(results);

                if (results.Count == 0)
                {
                    // we've hit the top.
                    break;
                }

                long.TryParse(results.Last().Id, out var latestRetreivedOldestId);
                retreiveFrom = latestRetreivedOldestId;
            }

            groupState.LastIndexedId = newestMessages.First().Id; // everything is downloaded
            await this.CacheContext.SaveChangesAsync(this.CancellationTokenSource.Token);
        }

        private void OpenNewGroupChat(IMessageContainer group)
        {
            this.SelectedGroupChat = group;
            this.SearchTerm = string.Empty;
            this.SelectedGroupName = group.Name;
            this.ContextView.Messages = null;
        }

        private void MessageSelected(MessageControlViewModelBase message)
        {
            if (message != null)
            {
                this.UpdateContextView(message.Message);
            }
        }

        private IQueryable<Message> GetMessagesForGroup(IMessageContainer group)
        {
            if (group is Group g)
            {
                return this.CacheContext.Messages
                    .AsNoTracking()
                    .Where(m => m.GroupId == g.Id);
            }
            else if (group is Chat c)
            {
                // Chat.Id returns the Id of the other user
                // However, GroupMe messages are natively returned with a Conversation Id instead
                // Conversation IDs are user1+user2.
                var sampleMessage = c.Messages.FirstOrDefault();

                return this.CacheContext.Messages
                    .AsNoTracking()
                    .Where(m => m.ConversationId == sampleMessage.ConversationId);
            }
            else
            {
                return Enumerable.Empty<Message>().AsQueryable();
            }
        }

        private void UpdateSearchResults()
        {
            this.ResultsView.Messages = null;

            var messagesForGroupChat = this.GetMessagesForGroup(this.SelectedGroupChat);

            var results = messagesForGroupChat
                .Where(m => m.Text.ToLower().Contains(this.SearchTerm.ToLower()))
                .OrderByDescending(m => m.Id);

            this.ResultsView.AssociateWith = this.SelectedGroupChat;
            this.ResultsView.Messages = results;
            this.ResultsView.ChangePage(0);
        }

        private void UpdateContextView(Message message)
        {
            this.ContextView.Messages = null;

            var messagesForGroupChat = this.GetMessagesForGroup(this.SelectedGroupChat)
                .OrderBy(m => m.Id);

            this.ContextView.AssociateWith = this.SelectedGroupChat;
            this.ContextView.Messages = messagesForGroupChat;
            this.ContextView.EnsureVisible(message);
        }

        private void ActivateGroupPlugin(IGroupChatCachePlugin plugin)
        {
            var cache = this.GetMessagesForGroup(this.SelectedGroupChat);
            _ = plugin.Activated(this.SelectedGroupChat, cache, this);
        }

        private void CloseLittlePopup()
        {
            if (this.PopupDialog is LoadingControlViewModel)
            {
                if (this.IndexingTask != null && !(this.IndexingTask.IsCompleted || this.IndexingTask.IsCanceled))
                {
                    // handle cancellation and restart
                    this.CancellationTokenSource.Cancel();
                    this.IndexingTask.ContinueWith((l) =>
                    {
                        Application.Current.Dispatcher.Invoke(() => this.CloseLittlePopup());
                    });
                    return;
                }
            }

            if (this.PopupDialog is IDisposable d)
            {
                d.Dispose();
            }

            this.PopupDialog = null;
        }
    }
}