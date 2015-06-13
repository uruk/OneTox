﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using SharpTox.Core;
using WinTox.ViewModel.ProfileSettings;
using ZXing;
using ZXing.Common;

namespace WinTox.View
{
    public sealed partial class ProfileSettingsFlyout : SettingsFlyout
    {
        private readonly ProfileSettingsViewModel _viewModel;
        private Timer _copyClipboardTimer;

        public ProfileSettingsFlyout()
        {
            InitializeComponent();
            _viewModel = DataContext as ProfileSettingsViewModel;
            StatusComboBox.ItemsSource = Enum.GetValues(typeof (ToxUserStatus)).Cast<ToxUserStatus>();
        }

        private void NameTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox.Text == String.Empty)
                textBox.Text = _viewModel.Name;
        }

        private void CopyButtonClick(object sender, RoutedEventArgs e)
        {
            CopyToxIdToClipboard();
            ShowCopyConfirm();
        }

        private void CopyToxIdToClipboard()
        {
            var dataPackage = new DataPackage {RequestedOperation = DataPackageOperation.Copy};
            dataPackage.SetText(_viewModel.Id.ToString());
            Clipboard.SetContent(dataPackage);
        }

        private void ShowCopyConfirm()
        {
            ClipboardCopyConfirm.Visibility = Visibility.Visible;

            if (_copyClipboardTimer == null)
            {
                _copyClipboardTimer =
                    new Timer(
                        state => CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            () => { ClipboardCopyConfirm.Visibility = Visibility.Collapsed; }),
                        null, 3000, Timeout.Infinite);
            }
            else
            {
                _copyClipboardTimer.Change(3000, Timeout.Infinite);
            }
        }

        private void QrCodeButtonClick(object sender, RoutedEventArgs e)
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Height = 300,
                    Width = 300
                }
            };
            var qrCode = writer.Write(_viewModel.Id.ToString()).ToBitmap() as WriteableBitmap;
            QrCodeImage.Source = qrCode;
        }

        private void NospamButtonClick(object sender, RoutedEventArgs e)
        {
            _viewModel.RandomizeNospam();
        }

        private async void ProfileSettingsFlyoutLostFocus(object sender, RoutedEventArgs e)
        {
            await _viewModel.SaveDataAsync();
        }

        private async void UserAvatarTapped(object sender, TappedRoutedEventArgs e)
        {
            var newAvatarFile = await PickUserAvatar();
            if (newAvatarFile != null)
            {
                var errorMessage = await _viewModel.LoadUserAvatar(newAvatarFile);
                if (errorMessage != String.Empty)
                {
                    var msgDialog = new MessageDialog(errorMessage, "Unsuccesfull loading");
                    await msgDialog.ShowAsync();
                }
            }

            // Show the settings again when we return, in case the user want to do more than just changing the picture.
            App.ShowProfileSettingsFlyout();
        }

        private async Task<StorageFile> PickUserAvatar()
        {
            var openPicker = new FileOpenPicker();
            openPicker.FileTypeFilter.Add(".png");
            return await openPicker.PickSingleFileAsync();
        }

        private async void RemoveAvatarButtonClick(object sender, RoutedEventArgs e)
        {
            await _viewModel.RemoveAvatar();
        }
    }
}