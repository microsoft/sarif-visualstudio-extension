// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Sarifer;
using Microsoft.CodeAnalysis.Sarif.Sarifer.FileWatcher;
using Microsoft.Sarif.Viewer.Interop;

using Moq;

using Xunit;

namespace Sarif.Sarifer.UnitTests
{
    public class SarifFolderWatcherTests : SariferUnitTestBase
    {

        [Fact]
        public void SarifFolder_DoesNot_Exist()
        {
            string sarifDirectory = ".sarif";
            string solutionDirectory = @"C:\some-solution-folder";
            string combinedPath = Path.Combine(solutionDirectory, sarifDirectory);

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

            var mockFileWatcher = new Mock<IFileWatcher>();

            var mockViewerInterop = new Mock<ISarifViewerInterop>();

            var monitor = new SarifFolderMonitor(mockViewerInterop.Object, mockFileSystem.Object, mockFileWatcher.Object);
            monitor.StartWatch(solutionDirectory);

            mockViewerInterop.Verify(m => m.OpenSarifLogAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
            mockViewerInterop.Verify(m => m.OpenSarifLogAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);

            monitor.StopWatch();

            mockViewerInterop.Verify(m => m.CloseSarifLogAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Fact]
        public void SarifFolder_Does_Exist_WithoutSarifFile()
        {
            SariferOption.InitializeForUnitTests();

            string sarifDirectory = ".sarif";
            string solutionDirectory = @"C:\some-solution-folder";
            string combinedPath = Path.Combine(solutionDirectory, sarifDirectory);
            string searchPattern = Constants.SarifFileSearchPattern;

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
            mockFileSystem.Setup(fs => fs.DirectoryExists(solutionDirectory)).Returns(true);
            mockFileSystem.Setup(fs => fs.DirectoryExists(combinedPath)).Returns(true);
            mockFileSystem.Setup(fs => fs.DirectoryGetFiles(combinedPath, searchPattern)).Returns(new string[] { });

            var mockFileWatcher = new Mock<IFileWatcher>();

            var mockViewerInterop = new Mock<ISarifViewerInterop>();

            var monitor = new SarifFolderMonitor(mockViewerInterop.Object, mockFileSystem.Object, mockFileWatcher.Object);
            monitor.StartWatch(solutionDirectory);

            mockViewerInterop.Verify(m => m.OpenSarifLogAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
            mockViewerInterop.Verify(m => m.OpenSarifLogAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
            mockFileWatcher.Verify(m => m.Start(), Times.Once);

            monitor.StopWatch();

            mockViewerInterop.Verify(m => m.CloseSarifLogAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
            mockFileWatcher.Verify(m => m.Stop(), Times.Once);
        }

        [Fact]
        public void SarifFolder_Does_Exist_WithSarifFiles()
        {
            SariferOption.InitializeForUnitTests();

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

            var mockViewerInterop = new Mock<ISarifViewerInterop>();

            var monitor = new SarifFolderMonitor(mockViewerInterop.Object, mockFileSystem.Object, mockFileWatcher.Object);
            monitor.StartWatch(solutionDirectory);

            mockViewerInterop.Verify(m => m.OpenSarifLogAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
            mockViewerInterop.Verify(m => m.OpenSarifLogAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
            mockFileWatcher.Verify(m => m.Start(), Times.Once);

            monitor.StopWatch();

            mockViewerInterop.Verify(m => m.CloseSarifLogAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
            mockFileWatcher.Verify(m => m.Stop(), Times.Once);
        }

        [Fact]
        public void SarifFolder_Add_NewSarifFiles()
        {
            SariferOption.InitializeForUnitTests();

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

            var mockViewerInterop = new Mock<ISarifViewerInterop>();

            var monitor = new SarifFolderMonitor(mockViewerInterop.Object, mockFileSystem.Object, mockFileWatcher.Object);
            monitor.StartWatch(solutionDirectory);

            mockViewerInterop.Verify(m => m.OpenSarifLogAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
            mockViewerInterop.Verify(m => m.OpenSarifLogAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
            mockFileWatcher.Verify(m => m.Start(), Times.Once);

            // simulating add one sarif file
            string newFile = Path.Combine(combinedPath, "NewAdded1.sarif");
            var eventArg = new FileSystemEventArgs(WatcherChangeTypes.Created, newFile, string.Empty);

            // trigger file created event 1st time
            mockFileWatcher.Raise(fw => fw.SarifLogFileCreated += null, eventArg);
            mockViewerInterop.Verify(m => m.OpenSarifLogAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);

            // trigger file created event 2nd time
            newFile = Path.Combine(combinedPath, "NewAdded2.sarif");
            eventArg = new FileSystemEventArgs(WatcherChangeTypes.Created, newFile, string.Empty);
            mockFileWatcher.Raise(fw => fw.SarifLogFileCreated += null, eventArg);

            newFile = Path.Combine(combinedPath, "NewAdded3.sarif");
            eventArg = new FileSystemEventArgs(WatcherChangeTypes.Created, newFile, string.Empty);
            mockFileWatcher.Raise(fw => fw.SarifLogFileCreated += null, eventArg);

            // 2 files created, expect method to be called 1 + 2 times
            mockViewerInterop.Verify(m => m.OpenSarifLogAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Exactly(3));

            monitor.StopWatch();

            mockViewerInterop.Verify(m => m.CloseSarifLogAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
            mockFileWatcher.Verify(m => m.Stop(), Times.Once);
        }

        [Fact]
        public void SarifFolder_Delete_ExistingSarifFiles()
        {
            SariferOption.InitializeForUnitTests();

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

            var mockViewerInterop = new Mock<ISarifViewerInterop>();

            var monitor = new SarifFolderMonitor(mockViewerInterop.Object, mockFileSystem.Object, mockFileWatcher.Object);
            monitor.StartWatch(solutionDirectory);

            mockViewerInterop.Verify(m => m.OpenSarifLogAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
            mockViewerInterop.Verify(m => m.OpenSarifLogAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
            mockFileWatcher.Verify(m => m.Start(), Times.Once);

            // simulate deleting one sarif file
            string newFile = Path.Combine(combinedPath, "Test1.sarif");
            var eventArg = new FileSystemEventArgs(WatcherChangeTypes.Deleted, newFile, string.Empty);

            // trigger file deleted event 1st time
            mockFileWatcher.Raise(fw => fw.SarifLogFileDeleted += null, eventArg);
            mockViewerInterop.Verify(m => m.CloseSarifLogAsync(It.IsAny<IEnumerable<string>>()), Times.Once);


            // trigger file created event 2nd time
            newFile = Path.Combine(combinedPath, "Test2.sarif");
            eventArg = new FileSystemEventArgs(WatcherChangeTypes.Deleted, newFile, string.Empty);
            mockFileWatcher.Raise(fw => fw.SarifLogFileDeleted += null, eventArg);

            newFile = Path.Combine(combinedPath, "Test3.sarif");
            eventArg = new FileSystemEventArgs(WatcherChangeTypes.Deleted, newFile, string.Empty);
            mockFileWatcher.Raise(fw => fw.SarifLogFileDeleted += null, eventArg);

            // 2 files deleted, method expected to be called 1 + 2 times
            mockViewerInterop.Verify(m => m.CloseSarifLogAsync(It.IsAny<IEnumerable<string>>()), Times.Exactly(3));

            monitor.StopWatch();

            mockViewerInterop.Verify(m => m.CloseSarifLogAsync(It.IsAny<IEnumerable<string>>()), Times.Exactly(4));
            mockFileWatcher.Verify(m => m.Stop(), Times.Once);
        }
    }
}
