// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LediReader
{
    public class InstanceStorageService
    {
        const string AppName = "LediReader";
        const string LockFileName = "instlock.txt";
        string _localAppDataPath;
        Guid _instance;
        string _localInstancePath;
        string _lockFilePath;
        Stream _lockFile;
        bool _isDisposed;

        /// <inheritdoc/>
        public string InstanceStoragePath => _localInstancePath;

        public InstanceStorageService()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _localAppDataPath = Path.Combine(localAppData, AppName);
            if (!(_localAppDataPath.StartsWith("\\")))
                _localAppDataPath = @"\\?\" + _localAppDataPath;
            _instance = Guid.NewGuid();
            _localInstancePath = Path.Combine(_localAppDataPath, "Inst_" + _instance.ToString());
            System.IO.Directory.CreateDirectory(_localInstancePath);
            _lockFilePath = Path.Combine(_localInstancePath, LockFileName);
            _lockFile = new FileStream(_lockFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

            RemoveAbandonedInstanceStorageServiceInstances();
        }


        public void RemoveAbandonedInstanceStorageServiceInstances()
        {
            var dir = new DirectoryInfo(_localAppDataPath);
            if (!dir.Exists)
                return;

            var directories = dir.GetDirectories("Inst_*", SearchOption.TopDirectoryOnly);

            foreach (var subdir in directories)
            {
                if (subdir.FullName != _localInstancePath)
                {
                    TryRemoveInstanceStorageDirectory(subdir);
                }
            }
        }

        private void TryRemoveInstanceStorageDirectory(DirectoryInfo dir)
        {
            bool isCandidateForRemoving = true;
            var lockFile = Path.Combine(dir.FullName, LockFileName);
            if (File.Exists(lockFile))
            {
                FileStream stream = null;
                try
                {
                    stream = new FileStream(lockFile, FileMode.Open, FileAccess.Write, FileShare.None);
                }
                catch (Exception ex)
                {
                    isCandidateForRemoving = false;
                }
                finally
                {
                    stream?.Dispose();
                }
            }

            if (isCandidateForRemoving)
            {
                try
                {
                    Directory.Delete(dir.FullName, true);
                }
                catch (Exception)
                {

                }
            }
        }


        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _lockFile?.Dispose();
                _lockFile = null;

                try
                {
                    Directory.Delete(_localInstancePath, true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception during shutdown of InstanceStorageService\r\nDetails:\r\n{ex.ToString()}");
                }
            }
        }
    }

}
