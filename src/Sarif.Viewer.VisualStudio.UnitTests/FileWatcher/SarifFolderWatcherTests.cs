// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.FileMonitor;
using Microsoft.Sarif.Viewer.Services;
using Microsoft.Sarif.Viewer.Shell;

using Moq;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class SarifFolderWatcherTests : SarifViewerPackageUnitTests
    {

        [Fact]
        public void SarifFolder_DoesNot_Exist()
        {
            string sarifDirectory = ".sarif";
            string solutionDirectory = @"C:\some-solution-folder";
            string combinedPath = Path.Combine(solutionDirectory, sarifDirectory);

            // folder doesnt exist
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

            var mockFileWatcher = new Mock<IFileWatcher>();

            var mockLoadService = new Mock<ILoadSarifLogService>();
            var mockCloseService = new Mock<ICloseSarifLogService>();

            var monitor = new SarifFolderMonitor(mockFileSystem.Object, mockFileWatcher.Object, mockLoadService.Object, mockCloseService.Object);
            monitor.StartWatch(solutionDirectory);

            // methods should not be called
            mockLoadService.Verify(m => m.LoadSarifLogs(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()), Times.Never);
            mockLoadService.Verify(m => m.LoadSarifLog(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);

            monitor.StopWatch();

            mockCloseService.Verify(m => m.CloseSarifLogs(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Fact]
        public void SarifFolder_Does_Exist_WithoutSarifFile()
        {
            string sarifDirectory = ".sarif";
            string solutionDirectory = @"C:\some-solution-folder";
            string combinedPath = Path.Combine(solutionDirectory, sarifDirectory);
            string searchPattern = Constants.SarifFileSearchPattern;

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
            mockFileSystem.Setup(fs => fs.DirectoryExists(solutionDirectory)).Returns(true);
            mockFileSystem.Setup(fs => fs.DirectoryExists(combinedPath)).Returns(true);
            // no sarif files
            mockFileSystem.Setup(fs => fs.DirectoryGetFiles(combinedPath, searchPattern)).Returns(new string[] { });

            var mockFileWatcher = new Mock<IFileWatcher>();

            var mockLoadService = new Mock<ILoadSarifLogService>();
            var mockCloseService = new Mock<ICloseSarifLogService>();

            var monitor = new SarifFolderMonitor(mockFileSystem.Object, mockFileWatcher.Object, mockLoadService.Object, mockCloseService.Object);
            monitor.StartWatch(solutionDirectory);

            mockLoadService.Verify(m => m.LoadSarifLogs(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()), Times.Once);
            mockLoadService.Verify(m => m.LoadSarifLog(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
            mockFileWatcher.Verify(m => m.Start(), Times.Once);

            monitor.StopWatch();

            mockCloseService.Verify(m => m.CloseSarifLogs(It.IsAny<IEnumerable<string>>()), Times.Once);
            mockFileWatcher.Verify(m => m.Stop(), Times.Once);
        }

        [Fact]
        public void SarifFolder_Does_Exist_WithSarifFiles()
        {
            string sarifDirectory = ".sarif";
            string solutionDirectory = @"C:\some-solution-folder";
            string combinedPath = Path.Combine(solutionDirectory, sarifDirectory);
            string searchPattern = Constants.SarifFileSearchPattern;
            string[] sarifFiles = new string[]
            {
                Path.Combine(combinedPath, "Test1.sarif"),
                Path.Combine(combinedPath, "Test2.sarif"),
                Path.Combine(combinedPath, "Test3.sarif"),
            };

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
            mockFileSystem.Setup(fs => fs.DirectoryExists(solutionDirectory)).Returns(true);
            mockFileSystem.Setup(fs => fs.DirectoryExists(combinedPath)).Returns(true);
            mockFileSystem.Setup(fs => fs.DirectoryGetFiles(combinedPath, searchPattern)).Returns(sarifFiles);

            var mockFileWatcher = new Mock<IFileWatcher>();

            var mockLoadService = new Mock<ILoadSarifLogService>();
            var mockCloseService = new Mock<ICloseSarifLogService>();

            var monitor = new SarifFolderMonitor(mockFileSystem.Object, mockFileWatcher.Object, mockLoadService.Object, mockCloseService.Object);
            monitor.StartWatch(solutionDirectory);

            mockLoadService.Verify(m => m.LoadSarifLogs(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()), Times.Once);
            mockLoadService.Verify(m => m.LoadSarifLog(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
            mockFileWatcher.Verify(m => m.Start(), Times.Once);

            monitor.StopWatch();

            mockCloseService.Verify(m => m.CloseSarifLogs(It.IsAny<IEnumerable<string>>()), Times.Once);
            mockFileWatcher.Verify(m => m.Stop(), Times.Once);
        }

        [Fact]
        public void SarifFolder_Add_NewSarifFiles()
        {
            string sarifDirectory = ".sarif";
            string solutionDirectory = @"C:\some-solution-folder";
            string combinedPath = Path.Combine(solutionDirectory, sarifDirectory);
            string searchPattern = Constants.SarifFileSearchPattern;
            var sarifFiles = new List<string>
            {
                Path.Combine(combinedPath, "Test1.sarif"),
                Path.Combine(combinedPath, "Test2.sarif"),
                Path.Combine(combinedPath, "Test3.sarif"),
            };

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
            mockFileSystem.Setup(fs => fs.DirectoryExists(solutionDirectory)).Returns(true);
            mockFileSystem.Setup(fs => fs.DirectoryExists(combinedPath)).Returns(true);
            mockFileSystem.Setup(fs => fs.DirectoryGetFiles(combinedPath, searchPattern)).Returns(sarifFiles);

            var mockFileWatcher = new Mock<IFileWatcher>();

            var mockLoadService = new Mock<ILoadSarifLogService>();
            var mockCloseService = new Mock<ICloseSarifLogService>();

            var monitor = new SarifFolderMonitor(mockFileSystem.Object, mockFileWatcher.Object, mockLoadService.Object, mockCloseService.Object);
            monitor.StartWatch(solutionDirectory);

            mockLoadService.Verify(m => m.LoadSarifLogs(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()), Times.Once);
            mockLoadService.Verify(m => m.LoadSarifLog(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
            mockFileWatcher.Verify(m => m.Start(), Times.Once);

            // simulating add one sarif file
            string newFile = Path.Combine(combinedPath, "NewAdded1.sarif");
            var eventArg = new FileSystemEventArgs(WatcherChangeTypes.Created, newFile, string.Empty);

            // trigger file created event 1st time
            mockFileWatcher.Raise(fw => fw.FileCreated += null, eventArg);
            mockLoadService.Verify(m => m.LoadSarifLog(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);

            // trigger file created event 2nd time
            newFile = Path.Combine(combinedPath, "NewAdded2.sarif");
            eventArg = new FileSystemEventArgs(WatcherChangeTypes.Created, newFile, string.Empty);
            mockFileWatcher.Raise(fw => fw.FileCreated += null, eventArg);

            newFile = Path.Combine(combinedPath, "NewAdded3.sarif");
            eventArg = new FileSystemEventArgs(WatcherChangeTypes.Created, newFile, string.Empty);
            mockFileWatcher.Raise(fw => fw.FileCreated += null, eventArg);

            // 2 files created, expect method to be called 1 + 2 times
            mockLoadService.Verify(m => m.LoadSarifLog(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Exactly(3));

            monitor.StopWatch();

            mockCloseService.Verify(m => m.CloseSarifLogs(It.IsAny<IEnumerable<string>>()), Times.Once);
            mockFileWatcher.Verify(m => m.Stop(), Times.Once);
        }

        [Fact]
        public void SarifFolder_Delete_ExistingSarifFiles()
        {
            string sarifDirectory = ".sarif";
            string solutionDirectory = @"C:\some-solution-folder";
            string combinedPath = Path.Combine(solutionDirectory, sarifDirectory);
            string searchPattern = Constants.SarifFileSearchPattern;
            var sarifFiles = new List<string>
            {
                Path.Combine(combinedPath, "Test1.sarif"),
                Path.Combine(combinedPath, "Test2.sarif"),
                Path.Combine(combinedPath, "Test3.sarif"),
            };

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
            mockFileSystem.Setup(fs => fs.DirectoryExists(solutionDirectory)).Returns(true);
            mockFileSystem.Setup(fs => fs.DirectoryExists(combinedPath)).Returns(true);
            mockFileSystem.Setup(fs => fs.DirectoryGetFiles(combinedPath, searchPattern)).Returns(sarifFiles);

            var mockFileWatcher = new Mock<IFileWatcher>();

            var mockLoadService = new Mock<ILoadSarifLogService>();
            var mockCloseService = new Mock<ICloseSarifLogService>();

            var monitor = new SarifFolderMonitor(mockFileSystem.Object, mockFileWatcher.Object, mockLoadService.Object, mockCloseService.Object);
            monitor.StartWatch(solutionDirectory);

            mockLoadService.Verify(m => m.LoadSarifLogs(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()), Times.Once);
            mockLoadService.Verify(m => m.LoadSarifLog(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
            mockFileWatcher.Verify(m => m.Start(), Times.Once);

            // simulate deleting one sarif file
            string newFile = Path.Combine(combinedPath, "Test1.sarif");
            var eventArg = new FileSystemEventArgs(WatcherChangeTypes.Deleted, newFile, string.Empty);

            // trigger file deleted event 1st time
            mockFileWatcher.Raise(fw => fw.FileDeleted += null, eventArg);
            mockCloseService.Verify(m => m.CloseSarifLogs(It.IsAny<IEnumerable<string>>()), Times.Once);


            // trigger file created event 2nd time
            newFile = Path.Combine(combinedPath, "Test2.sarif");
            eventArg = new FileSystemEventArgs(WatcherChangeTypes.Deleted, newFile, string.Empty);
            mockFileWatcher.Raise(fw => fw.FileDeleted += null, eventArg);

            newFile = Path.Combine(combinedPath, "Test3.sarif");
            eventArg = new FileSystemEventArgs(WatcherChangeTypes.Deleted, newFile, string.Empty);
            mockFileWatcher.Raise(fw => fw.FileDeleted += null, eventArg);

            // 2 files deleted, method expected to be called 1 + 2 times
            mockCloseService.Verify(m => m.CloseSarifLogs(It.IsAny<IEnumerable<string>>()), Times.Exactly(3));

            monitor.StopWatch();

            mockCloseService.Verify(m => m.CloseSarifLogs(It.IsAny<IEnumerable<string>>()), Times.Exactly(4));
            mockFileWatcher.Verify(m => m.Stop(), Times.Once);
        }
    }
}
