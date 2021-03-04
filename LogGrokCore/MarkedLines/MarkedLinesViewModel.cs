﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace LogGrokCore.MarkedLines
{
    internal class MarkedLinesViewModel : ViewModelBase
    {
        private readonly ObservableCollection<MarkedLineViewModel> _markedLines = new();
        private readonly ObservableCollection<DocumentViewModel> _documents;
        private readonly HashSet<DocumentViewModel> _alreadySubscribed = new();
        
        public ObservableCollection<MarkedLineViewModel> Documents => _markedLines;

        public DelegateCommand CopyLinesCommand { get; }


        public MarkedLinesViewModel(ObservableCollection<DocumentViewModel> documents)
        {
            _documents = documents;
            SubscribeToNewDocumentChanges(_documents);

            _documents.CollectionChanged += (_, _) =>
            {
                SubscribeToNewDocumentChanges(_documents);
                UpdateLinesCollection();
            };

            CopyLinesCommand = new DelegateCommand(
                o =>
                {
                    var document = (DocumentViewModel) o;
                    var linesToCopy =
                        Documents.Where(m => m.Document == document)
                            .OrderBy(m => m.Index)
                            .Select(m => m.ToString());

                    TextCopy.ClipboardService.SetText(string.Join("\r\n", linesToCopy).Trim('\0'));
                });
            
            var view = (CollectionView)CollectionViewSource.GetDefaultView(Documents);
            var groupDescription = new PropertyGroupDescription("Document");
            view.GroupDescriptions?.Add(groupDescription);
            UpdateLinesCollection();
        }

        
        private void SubscribeToNewDocumentChanges(ObservableCollection<DocumentViewModel> documents)
        {
            var newDocuments = new HashSet<DocumentViewModel>(documents);
            _alreadySubscribed.RemoveWhere(d => !newDocuments.Contains(d));
            foreach (var documentViewModel in documents)
            {
                if (_alreadySubscribed.Contains(documentViewModel)) continue;
                documentViewModel.MarkedLineViewModels.CollectionChanged +=
                    (_, _) => UpdateLinesCollection();
                documentViewModel.MarkedLinesChanged += UpdateLinesCollection; 
                _alreadySubscribed.Add(documentViewModel);
            }
        }

        private void UpdateLinesCollection()
        {
            var allDocuments = new HashSet<DocumentViewModel>(_documents);
            for (var i = _markedLines.Count - 1; i >= 0; i--)
            {
                if (!allDocuments.Contains(_markedLines[i].Document))
                    _markedLines.RemoveAt(i);
            }

            var index = 0;
            
            foreach (var (document, (lineNumber, text)) in _documents.SelectMany(
                document => document.MarkedLineViewModels.Select(line => (document, line))))
            {
                if (_markedLines.Count <= index || _markedLines[index].Document != document)
                {
                    _markedLines.Insert(index, new MarkedLineViewModel(document, lineNumber, text));
                    index++;
                    continue;
                }

                while (_markedLines[index].Index < lineNumber)
                {
                    _markedLines.RemoveAt(index);
                }

                if (_markedLines[index].Index > lineNumber)
                {
                    _markedLines.Insert(index, new MarkedLineViewModel(document, lineNumber, text));
                }

                index++;
            }

            while (_markedLines.Count > index)
            {
                _markedLines.RemoveAt(index);
            }
        }
    }
}