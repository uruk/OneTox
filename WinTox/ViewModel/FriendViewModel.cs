﻿using System;
using SharpTox.Core;
using WinTox.Common;

namespace WinTox.ViewModel
{
    public class FriendViewModel : ViewModelBase, IToxUserViewModel
    {
        private bool _isOnline;
        private string _name;
        private RelayCommand _removeFriendCommand;
        private ToxUserStatus _status;
        private string _statusMessage;

        public FriendViewModel(int friendNumber)
        {
            Conversation = new ConversationViewModel(this);

            FriendNumber = friendNumber;

            Name = App.ToxModel.GetFriendName(friendNumber);
            if (Name == String.Empty)
            {
                Name = App.ToxModel.GetFriendPublicKey(friendNumber).ToString().Substring(0, 10);
            }

            StatusMessage = App.ToxModel.GetFriendStatusMessage(friendNumber);
            if (StatusMessage == String.Empty)
            {
                StatusMessage = "Friend request sent.";
            }

            Status = App.ToxModel.GetFriendStatus(friendNumber);
            IsOnline = App.ToxModel.IsFriendOnline(friendNumber);
        }

        public ConversationViewModel Conversation { get; private set; }
        public int FriendNumber { get; private set; }

        public RelayCommand RemoveFriendCommand
        {
            get
            {
                return _removeFriendCommand
                       ?? (_removeFriendCommand = new RelayCommand(
                           (object parameter) =>
                           {
                               ToxErrorFriendDelete error;
                               App.ToxModel.DeleteFriend(FriendNumber, out error);
                               // TODO: Handle errors!!!
                           }));
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get { return _statusMessage; }
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public ToxUserStatus Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public bool IsOnline
        {
            get { return _isOnline; }
            set
            {
                _isOnline = value;
                OnPropertyChanged();
            }
        }

        public void ReceiveMessage(ToxEventArgs.FriendMessageEventArgs e)
        {
            Conversation.ReceiveMessage(e);
        }
    }
}