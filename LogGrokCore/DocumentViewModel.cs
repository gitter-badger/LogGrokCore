using System;
using System.IO;
using LogGrokCore.Colors;
using LogGrokCore.Controls;
using LogGrokCore.Data;
using LogGrokCore.Search;

namespace LogGrokCore
{
    internal class DocumentViewModel : ViewModelBase
    {
        private readonly Selection _markedLines;
        private bool _isCurrentDocument;

        public DocumentViewModel(
            LogModelFacade logModelFacade,
            LogViewModel logViewModel, 
            SearchViewModel searchViewModel,
            Selection markedLines,
            ColorSettings colorSettings)
        {
            Title = 
                Path.GetFileName(logModelFacade.LogFile.FilePath) 
                    ?? throw new InvalidOperationException($"Invalid path: {logModelFacade.LogFile.FilePath}");

            LogViewModel = logViewModel;
            SearchViewModel = searchViewModel;
            ColorSettings = colorSettings;
            
            SearchViewModel.CurrentLineChanged += lineNumber => LogViewModel.NavigateTo(lineNumber);
            SearchViewModel.CurrentSearchChanged += regex => LogViewModel.HighlightRegex = regex;

            _markedLines = markedLines;
        }

        public string Title { get; }

        public LogViewModel LogViewModel { get; }

        public SearchViewModel SearchViewModel { get; }

        public ColorSettings ColorSettings { get; }

        public bool IsCurrentDocument
        {
            get => _isCurrentDocument;
            set => SetAndRaiseIfChanged(ref _isCurrentDocument, value);
        }
    }
}