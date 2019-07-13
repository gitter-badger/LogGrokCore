﻿using ReactiveUI;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using LogGrokCore.Data;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore
{
    internal class DocumentViewModel : ReactiveObject
    {
        private readonly GridViewFactory _viewFactory;
        private readonly LineIndex _lineIndex;
        private readonly string _filePath;
        
        public DocumentViewModel(
            LogFile logFile, 
            IItemProvider<string> lineProvider,
            ILineParser lineParser,
            Loader loader,
            LineIndex lineIndex,
            GridViewFactory viewFactory)
        {
            _lineIndex = lineIndex;
            _filePath = logFile.FilePath;
            Title = Path.GetFileName(_filePath);

            var lineCollection =
                new VirtualList<string, LineViewModel>(lineProvider, 
                    (str, index) => new LineViewModel(index, str, lineParser));
            Lines = new GrowingCollectionAdapter<LineViewModel>(lineCollection);
                
            CopyPathToClipboardCommand = new DelegateCommand(() => TextCopy.Clipboard.SetText(_filePath));
            OpenContainingFolderCommand = new DelegateCommand(OpenContainingFolder);

            _viewFactory = viewFactory;
            UpdateDocumentWhileLoading();
        }

        public ViewBase CustomView => _viewFactory.CreateView();

        public ICommand CopyPathToClipboardCommand { get; }
        public ICommand OpenContainingFolderCommand { get; }
        public string Title { get; }

        public GrowingCollectionAdapter<LineViewModel> Lines { get; }

        private async void UpdateDocumentWhileLoading()
        {
            var delay = 10;
            while (!_lineIndex.IsFinished)
            {
                await Task.Delay(delay);
                if (delay < 1000)
                    delay *= 2;
                Lines.UpdateCount();
            }
            Lines.UpdateCount();
        }
        private void OpenContainingFolder()
        {
            var filePath = _filePath;

            var cmdLine = File.Exists(filePath)
                ? $"/select, {filePath}"
                : $"/select, {Directory.GetParent(filePath).FullName}";

            _ = Process.Start("explorer.exe", cmdLine);
        }
    }
}
