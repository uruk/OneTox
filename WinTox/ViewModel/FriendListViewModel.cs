﻿using SharpTox.Core;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using WinTox.Model;

namespace WinTox.ViewModel
{
    internal class FriendListViewModel
    {
        public FriendListViewModel()
        {
            Friends = new ObservableCollection<FriendViewModel>();
            foreach (var friendNumber in App.ToxModel.Friends)
            {
                Friends.Add(new FriendViewModel(friendNumber));
            }

            App.ToxModel.FriendNameChanged += FriendNameChangedHandler;
            App.ToxModel.FriendStatusMessageChanged += FriendStatusMessageChangedHandler;
            App.ToxModel.FriendStatusChanged += FriendStatusChangedHandler;
            App.ToxModel.FriendConnectionStatusChanged += FriendConnectionStatusChangedHandler;
            App.ToxModel.FriendListModified += FriendListModifiedHandler;
        }

        // We need to run the event handlers from the UI thread.
        // Otherwise the PropertyChanged events wouldn't work in FriendViewModel.

        private readonly CoreDispatcher _dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

        private void FriendNameChangedHandler(object sender, ToxEventArgs.NameChangeEventArgs e)
        {
            _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { FindFriend(e.FriendNumber).Name = e.Name; });
        }

        private void FriendStatusMessageChangedHandler(object sender, ToxEventArgs.StatusMessageEventArgs e)
        {
            _dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () => { FindFriend(e.FriendNumber).StatusMessage = e.StatusMessage; });
        }

        private void FriendStatusChangedHandler(object sender, ToxEventArgs.StatusEventArgs e)
        {
            _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { FindFriend(e.FriendNumber).Status = e.Status; });
        }

        private void FriendConnectionStatusChangedHandler(object sender, ToxEventArgs.FriendConnectionStatusEventArgs e)
        {
            _dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () => { FindFriend(e.FriendNumber).IsOnline = e.Status != ToxConnectionStatus.None; });
        }

        private void FriendListModifiedHandler(int friendNumber, ExtendedTox.FriendListModificationType modificationType)
        {
            _dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    switch (modificationType)
                    {
                        case ExtendedTox.FriendListModificationType.Add:
                            Friends.Add(new FriendViewModel(friendNumber));
                            return;

                        case ExtendedTox.FriendListModificationType.Remove:
                            Friends.Remove(FindFriend(friendNumber));
                            return;

                        case ExtendedTox.FriendListModificationType.Reset:
                            Friends.Clear();
                            foreach (var friendN in App.ToxModel.Friends)
                            {
                                Friends.Add(new FriendViewModel(friendN));
                            }
                            return;
                    }
                });
        }

        private FriendViewModel FindFriend(int friendNumber)
        {
            foreach (var friend in Friends)
            {
                if (friend.FriendNumber == friendNumber)
                    return friend;
            }
            return null;
        }

        public ObservableCollection<FriendViewModel> Friends { get; set; }
    }
}
