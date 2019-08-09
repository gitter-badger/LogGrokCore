using System;
using System.IO;
using LogGrokCore.Data;

namespace LogGrokCore
{
    internal class DocumentViewModel : ViewModelBase
    {
        private bool _isCurrentDocument;

        public DocumentViewModel(
            LogModelFacade logModelFacade,
            LogViewModel logViewModel, 
            SearchViewModel searchViewModel)
        {
            Title = 
                Path.GetFileName(logModelFacade.FilePath) 
                    ?? throw new InvalidOperationException($"Invalid path: {logModelFacade.FilePath}");

            LogViewModel = logViewModel;
            SearchViewModel = searchViewModel;
        }

        public string Title { get; }

        public LogViewModel LogViewModel { get; }

        public SearchViewModel SearchViewModel { get; }

        public bool IsCurrentDocument
        {
            get => _isCurrentDocument;
            set => SetAndRaiseIfChanged(ref _isCurrentDocument, value);
        }
    }
}