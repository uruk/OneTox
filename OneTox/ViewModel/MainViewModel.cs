﻿using System;
using System.Collections.Specialized;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using OneTox.Helpers;
using OneTox.ViewModel.FriendRequests;
using OneTox.ViewModel.Friends;
using SharpTox.Core;

namespace OneTox.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        private Visibility _friendRequestsListVisibility;

        public MainViewModel()
        {
            FriendList = new FriendListViewModel();

            FriendRequests = new FriendRequestsViewModel();
            FriendRequests.FriendRequestReceived += FriendRequestReceivedHandler;
            FriendRequests.Items.CollectionChanged += FriendRequestsCollectionChangedHandler;
            DecideFriendRequestsListVisibility();
        }

        public FriendListViewModel FriendList { get; }
        public FriendRequestsViewModel FriendRequests { get; }

        public Visibility FriendRequestsListVisibility
        {
            get { return _friendRequestsListVisibility; }
            set
            {
                if (value == _friendRequestsListVisibility)
                    return;
                _friendRequestsListVisibility = value;
                RaisePropertyChanged();
            }
        }

        private async void FriendRequestReceivedHandler(object sender, ToxEventArgs.FriendRequestEventArgs e)
        {
            // TODO: Turn it into a toast notification.
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var message = "From: " + e.PublicKey + "\n" + "Message: " + e.Message;
                var msgDialog = new MessageDialog(message, "Friend request received");
                msgDialog.Commands.Add(new UICommand("Accept", null, FriendRequestsViewModel.FriendRequestAnswer.Accept));
                msgDialog.Commands.Add(new UICommand("Decline", null,
                    FriendRequestsViewModel.FriendRequestAnswer.Decline));
                msgDialog.Commands.Add(new UICommand("Later", null, FriendRequestsViewModel.FriendRequestAnswer.Later));
                var answer = await msgDialog.ShowAsync();
                FriendRequests.HandleFriendRequestAnswer((FriendRequestsViewModel.FriendRequestAnswer) answer.Id, e);
            });
        }

        private void FriendRequestsCollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            DecideFriendRequestsListVisibility();
        }

        private void DecideFriendRequestsListVisibility()
        {
            FriendRequestsListVisibility = FriendRequests.Items.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}