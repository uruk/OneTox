﻿using System.Collections.Specialized;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using OneTox.ViewModel;
using OneTox.ViewModel.Friends;

namespace OneTox.View.Pages
{
    public sealed partial class MainPage : Page
    {
        private MainViewModel _mainViewModel;

        public MainPage()
        {
            InitializeComponent();

            ChangeLayoutBasedOnWindowWidth(Window.Current.Bounds.Width);
        }

        private void MainPageLoaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged += WindowSizeChanged;

            VisualStateManager.GoToState(this, "ChatState", false);

            _mainViewModel = DataContext as MainViewModel;
            _mainViewModel.FriendList.Friends.CollectionChanged += FriendsCollectionChangedHandler;

            // TODO: Remember which friend we talked to the last time before shutting down the app and resume with selecting him/her.
            // TODO: Handle the case when the user doesn't have any friends yet with a splash screen or something like that!
            if (_mainViewModel.FriendList.Friends.Count > 0)
            {
                FriendList.SelectedItem = _mainViewModel.FriendList.Friends[0];
            }
        }

        private void MainPageUnloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged -= WindowSizeChanged;
        }

        private void WindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            ChangeLayoutBasedOnWindowWidth(e.Size.Width);
        }

        private void ChangeLayoutBasedOnWindowWidth(double width)
        {
            if (width < 930)
            {
                LeftPanel.Visibility = Visibility.Collapsed;
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    AppViewBackButtonVisibility.Visible;
                VisualStateManager.GoToState(ChatBlock, "NarrowState", true);
                VisualStateManager.GoToState(ProfileSettingsBlock, "NarrowState", true);
            }
            else
            {
                LeftPanel.Visibility = Visibility.Visible;
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    AppViewBackButtonVisibility.Collapsed;
                VisualStateManager.GoToState(ChatBlock, "WideState", true);
                VisualStateManager.GoToState(ProfileSettingsBlock, "WideState", true);
            }
        }

        private void FriendListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FriendList.SelectedItem == null)
                return;

            VisualStateManager.GoToState(this, "ChatState", true);

            ChatBlock.SetDataContext(FriendList.SelectedItem as FriendViewModel);
        }

        private void AddFriendButtonClick(object sender, RoutedEventArgs e)
        {
            FriendList.SelectedItem = null;
            VisualStateManager.GoToState(this, "AddFriendState", true);
        }

        private void SettingsButtonClick(object sender, RoutedEventArgs e)
        {
            FriendList.SelectedItem = null;
            VisualStateManager.GoToState(this, "SettingsState", true);
        }

        private void FriendsCollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldStartingIndex == -1)
                return;

            if (FriendList.SelectedItem == null) // It means that we just removed the currently selected friend.
            {
                // So select the one right above it:
                FriendList.SelectedItem = (e.OldStartingIndex - 1) > 0
                    ? _mainViewModel.FriendList.Friends[e.OldStartingIndex - 1]
                    : _mainViewModel.FriendList.Friends[0];
            }
        }
    }
}