﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SharpTox.Core;
using System.Threading.Tasks;

namespace WinTox.Model
{
    /// <summary>
    ///     Implements the Singleton pattern. (https://msdn.microsoft.com/en-us/library/ff650849.aspx)
    /// </summary>
    internal class FileTransferManager : DataTransferManager
    {
        private static FileTransferManager _instance;

        private FileTransferManager()
        {
            ToxModel.Instance.FriendConnectionStatusChanged += FriendConnectionStatusChangedHandler;
        }

        public static FileTransferManager Instance
        {
            get { return _instance ?? (_instance = new FileTransferManager()); }
        }

        #region Debug

        protected new void AddTransfer(int friendNumber, int fileNumber, Stream stream, long dataSizeInBytes,
            TransferDirection direction, long transferredBytes = 0)
        {
            base.AddTransfer(friendNumber, fileNumber, stream, dataSizeInBytes, direction, transferredBytes);

            Debug.WriteLine(
                "File {0}load added! \t friend number: {1}, \t file number: {2}, \t total file transfers: {3}",
                direction, friendNumber, fileNumber, Transfers.Count);
        }

        protected new void RemoveTransfer(TransferId transferId)
        {
            var direction = Transfers[transferId].Direction;

            base.RemoveTransfer(transferId);

            Debug.WriteLine(
                "File {0}load removed! \t friend number: {1}, \t file number: {2}, \t total file transfers: {3}",
                direction, transferId.FriendNumber, transferId.FileNumber, Transfers.Count);
        }

        #endregion

        #region File transfer resuming between core restarts

        public async Task StoreUnfinishedTransfers()
        {
            foreach (var transfer in Transfers)
            {
                await FileTransferResumer.Instance.ConfirmTransfer(transfer.Key.FriendNumber, transfer.Key.FileNumber, transfer.Value.TransferredBytes);
            }
        }

        private async Task RestoreUnfinishedUploadsForFriend(int friendNumber)
        {
            var resumeDataOfFinishedUploads = await FileTransferResumer.Instance.GetResumeDataOfSavedUploadsForFriend(friendNumber);
            foreach (var resumeData in resumeDataOfFinishedUploads)
            {
                bool successfulFileSend;
                var fileInfo = ToxModel.Instance.FileSend(resumeData.FriendNumber, ToxFileKind.Data, resumeData.FileStream.Length, resumeData.FileName,
                    resumeData.FileId, out successfulFileSend);

                if (successfulFileSend)
                {
                    AddTransfer(resumeData.FriendNumber, fileInfo.Number, resumeData.FileStream, resumeData.FileStream.Length, TransferDirection.Up, resumeData.TransferredBytes);

                    if (FileUploadAdded != null)
                        FileUploadAdded(this, new ToxEventArgs.FileSendRequestEventArgs(friendNumber, fileInfo.Number, ToxFileKind.Data, resumeData.FileStream.Length, resumeData.FileName));
                }
                else
                {
                    resumeData.FileStream.Dispose();
                }
            }
        }

        public event EventHandler<ToxEventArgs.FileSendRequestEventArgs> FileUploadAdded;

        #endregion

        #region Common

        public void CancelTransfer(int friendNumber, int fileNumber)
        {
            SendCancelControl(friendNumber, fileNumber);

            var transferId = new TransferId(friendNumber, fileNumber);
            if (Transfers.ContainsKey(transferId))
            {
                RemoveTransfer(transferId);
                FileTransferResumer.Instance.RemoveTransfer(friendNumber, fileNumber);
            }
        }

        public void PauseTransfer(int friendNumber, int fileNumber)
        {
            ToxModel.Instance.FileControl(friendNumber, fileNumber, ToxFileControl.Pause);
        }

        public void ResumeTransfer(int friendNumber, int fileNumber)
        {
            SendResumeControl(friendNumber, fileNumber);
        }

        protected override void HandleFileControl(ToxFileControl fileControl, TransferId transferId)
        {
            if (FileControlReceived != null)
                FileControlReceived(this,
                    new ToxEventArgs.FileControlEventArgs(transferId.FriendNumber, transferId.FileNumber, fileControl));

            switch (fileControl)
            {
                case ToxFileControl.Cancel:
                    RemoveTransfer(transferId);
                    FileTransferResumer.Instance.RemoveTransfer(transferId.FriendNumber, transferId.FileNumber);
                    return;
            }
        }

        public Dictionary<int, double> GetTransferProgressesOfFriend(int friendNumber)
        {
            var progressDict = new Dictionary<int, double>();
            foreach (var transfer in Transfers)
            {
                if (transfer.Key.FriendNumber == friendNumber)
                    progressDict.Add(transfer.Key.FileNumber, transfer.Value.Progress);
            }
            return progressDict;
        }

        private async void FriendConnectionStatusChangedHandler(object sender, ToxEventArgs.FriendConnectionStatusEventArgs e)
        {
            if (!ToxModel.Instance.IsFriendOnline(e.FriendNumber))
            {
                var transfers = Transfers.ToArray();
                foreach (var transfer in transfers)
                {
                    if (transfer.Key.FriendNumber == e.FriendNumber)
                    {
                        RemoveTransfer(new TransferId(transfer.Key.FriendNumber, transfer.Key.FileNumber));
                        await FileTransferResumer.Instance.ConfirmTransfer(transfer.Key.FriendNumber, transfer.Key.FileNumber, transfer.Value.TransferredBytes);

                        // TODO: This belove is very ugly and might be dangerous. Find a better solution for it!!!
                        // If a friend goes offline, we "lie" to the ViewModel saying that the friend canceled the transfer.
                        if (FileControlReceived != null)
                            FileControlReceived(this,
                                new ToxEventArgs.FileControlEventArgs(e.FriendNumber, transfer.Key.FileNumber,
                                    ToxFileControl.Cancel));
                    }
                }
            }
            else
            {
                if (ToxModel.Instance.LastConnectionStatusOfFriend(e.FriendNumber) != ToxConnectionStatus.None)
                    return;

                await RestoreUnfinishedUploadsForFriend(e.FriendNumber);
            }
        }

        private void RaiseTransferFinished(int friendNumber, int fileNumber)
        {
            if (TransferFinished != null)
                TransferFinished(this, new TransferFinishedEventArgs(friendNumber, fileNumber));
        }

        public event EventHandler<ToxEventArgs.FileControlEventArgs> FileControlReceived;

        public class TransferFinishedEventArgs : ToxEventArgs.FileBaseEventArgs
        {
            public TransferFinishedEventArgs(int friendNumber, int fileNumber) : base(friendNumber, fileNumber)
            {
            }
        }

        public event EventHandler<TransferFinishedEventArgs> TransferFinished;

        #endregion

        #region Sending

        public bool SendFile(int friendNumber, Stream stream, string fileName, out int fileNumber)
        {
            bool successfulFileSend;
            var fileInfo = ToxModel.Instance.FileSend(friendNumber, ToxFileKind.Data, stream.Length, fileName,
                out successfulFileSend);

            if (successfulFileSend)
            {
                AddTransfer(friendNumber, fileInfo.Number, stream, stream.Length, TransferDirection.Up);
            }

            fileNumber = fileInfo.Number;
            return successfulFileSend;
        }

        protected override void HandleFinishedUpload(TransferId transferId, int friendNumber, int fileNumber)
        {
            RemoveTransfer(transferId);
            RaiseTransferFinished(friendNumber, fileNumber);
        }

        #endregion

        #region Receiving

        public event EventHandler<ToxEventArgs.FileSendRequestEventArgs> FileSendRequestReceived;

        public void ReceiveFile(int friendNumber, int fileNumber, Stream saveStream)
        {
            var transferId = new TransferId(friendNumber, fileNumber);

            // Replace the dummy stream set previously in FileSendRequestReceivedHandler():
            Transfers[transferId].ReplaceStream(saveStream);

            ResumeTransfer(friendNumber, fileNumber);
        }

        protected override async void FileSendRequestReceivedHandler(object sender,
            ToxEventArgs.FileSendRequestEventArgs e)
        {
            if (e.FileKind != ToxFileKind.Data)
                return;

            var fileId = ToxModel.Instance.FileGetId(e.FriendNumber, e.FileNumber);
            var resumeData = await FileTransferResumer.Instance.GetDownloadData(fileId);
            if (resumeData != null)
            {
                AddTransfer(e.FriendNumber, e.FileNumber, resumeData.FileStream, e.FileSize, TransferDirection.Down, resumeData.TransferredBytes);
                ToxModel.Instance.FileSeek(e.FriendNumber, e.FileNumber, resumeData.TransferredBytes);
                ResumeTransfer(e.FriendNumber, e.FileNumber);

                if (FileSendRequestReceived != null)
                    FileSendRequestReceived(this, e);

                // TODO: This belove is very ugly and might be dangerous. Find a better solution for it!!!
                // If we resume a download from a previous run, we "lie" to the ViewModel saying that the friend resumed the transfer.
                if (FileControlReceived != null)
                    FileControlReceived(this,
                        new ToxEventArgs.FileControlEventArgs(e.FriendNumber, e.FileNumber,
                            ToxFileControl.Resume));
            }
            else
            {
                // We add a transfer with a null value instead of an actual stream here. We will replace it with an actual file stream
                // in ReceiveFile() when the user accepts the request and chooses a location to save the file to.
                AddTransfer(e.FriendNumber, e.FileNumber, null, e.FileSize, TransferDirection.Down);

                if (FileSendRequestReceived != null)
                    FileSendRequestReceived(this, e);
            }
        }

        protected override void HandleFinishedDownload(TransferId transferId, int friendNumber, int fileNumber)
        {
            RemoveTransfer(transferId);
            RaiseTransferFinished(friendNumber, fileNumber);
        }

        #endregion
    }
}