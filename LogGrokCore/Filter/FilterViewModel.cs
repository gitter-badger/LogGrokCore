using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using LogGrokCore.Data;
using LogGrokCore.Data.Index;

namespace LogGrokCore.Filter
{
    internal class FilterViewModel : ViewModelBase 
    {
        private readonly string _fieldName;
        private readonly FilterSettings _filterSettings;
        private readonly Indexer _indexer;
        private string? _textFilter;
        private int _indexedFieldIndex;
        public FilterViewModel(
            string fieldName, 
            FilterSettings filterSettings,
            Indexer indexer,
            LogMetaInformation metaInformation)
        {
            _fieldName = fieldName;
            _filterSettings = filterSettings;
            _indexer = indexer;

            _indexedFieldIndex = metaInformation.GetIndexedFieldIndexByName(fieldName);
            
            _indexer.NewComponentAdded += OnNewComponentAdded;
            
            var fieldValues = 
                _indexer.GetAllComponents(_indexedFieldIndex);
            
            Elements = new ObservableCollection<ElementViewModel>(
                fieldValues.Select(fieldValue => new ElementViewModel(fieldValue, true, () => 0)));
            
        }      

        private readonly ConcurrentBag<(int componentNumber, IndexKey key)> _newComponentsQueue = new();
        private DispatcherOperation? _addComponentDispatcherOperation;
        
        private void OnNewComponentAdded((int componentNumber, IndexKey key) newComponent)
        {
            var (componentIndex, _) = newComponent;
            if (componentIndex != _indexedFieldIndex)
            {
                return;
            }

            _newComponentsQueue.Add(newComponent);
            
            void ProcessNewComponents()
            {
                while ( _newComponentsQueue.TryTake(out var valueTuple))
                {
                    var (componentNumber, key) = valueTuple;
                    var newElement = new ElementViewModel(key.GetComponent(componentNumber).ToString(), 
                        true, () => 0);
                    Elements.Add(newElement);
                }
            }

            _addComponentDispatcherOperation ??=
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    ProcessNewComponents();
                    _addComponentDispatcherOperation = null;
                    ProcessNewComponents();
                }, DispatcherPriority.Normal);
        }

        public string? TextFilter
        {
            get => _textFilter;
            set => SetAndRaiseIfChanged(ref _textFilter, value);
        }

        public ObservableCollection<ElementViewModel> Elements { get; } 
        
        
        // IndexedFilter, 
                    // ndexer : GenericIndexer, 
                    // [NotNull] syncContext : SynchronizationContext)
        // {
        //     _componentKey = componentKey;
        //     _indexedFilter = indexedFilter;
        //
        //     _elementFactory =
        //   (component : Text, isActive : bool) => ElementViewModel(component.ToString(), 
        //                                                           component, isActive,
        //                                         () => indexer.GetIndexCountForComponent(componentKey, component));
        //
        //     Categories = indexer.GetAllComponents(componentKey);
        //
        //     Elements = ObservableCollection(Categories.Select(_elementFactory(_, true)));
        //     
        //     indexedFilter.Changed += () => OnFilterChanged();
        //
        //     Subscribe(indexer, syncContext);
        // }
        //
        // public OnFilterChanged() : void
        // {
        //     def exclusions = HashSet(_indexedFilter.GetExclusions(_componentKey));
        //     
        //     foreach (element in Elements)
        //     {
        //         element.IsActive = !exclusions.Contains(element.Category)
        //     }
        //     
        //     RaisePropertyChanged(IsFilterApplied);
        // }
        //
        // public TextFilter : String
        // {
        //     get; set;   
        // }
        //
        // public Elements : ObservableCollection[ElementViewModel] { get; private set; }
        //
        // public RefreshActiveChanged : ICommand 
        // { 
        //     get 
        //     {
        //         DelegateCommand(RefreshExcludedCategories)
        //
        //     }
        // }
        //
        // public DeselectAll : ICommand
        // {
        //     get
        //     {
        //         DelegateCommand(() => _indexedFilter.ExcludeAllExcept(_componentKey, []));
        //     }
        // }
        //
        // public SelectAll : ICommand
        // {
        //     get
        //     {
        //         DelegateCommand(() => _indexedFilter.SetExclusions(_componentKey, []));
        //     }
        // }
        //
        // public SelectOnlySearchResults : ICommand
        // {
        //     get
        //     {
        //         DelegateCommand.[IEnumerable](items =>
        //         {
        //                                       def changedElements : IEnumerable[ElementViewModel] = items.Cast();
        //                                       def activeCategories = changedElements.Select(o => o.Category);
        //                                       Elements = ObservableCollection(
        //                                                  Categories.Select(c => _elementFactory(c, activeCategories.Contains(c))));
        //
        //                                       RefreshExcludedCategories();
        //                                       RaisePropertyChanged(IsFilterApplied);
        //         })
        //     }
        // }
        //
        // public IsFilterApplied : bool
        // {
        //     get 
        //     {
        //         GetExcludedCategories().Any()
        //     }
        // }
        //
        // public AddNewCategory(category : Text) : void
        // {
        //     Categories = [category].Concat(Categories).ToList();
        //     def newCategories = Categories.Except(Elements.Select(_.Category).Where(c => Categories.Contains(c)));
        //
        //     foreach(category in newCategories) 
        //         Elements.Add(_elementFactory(category, true))
        // }
        //
        // protected OnExcludedCategoriesChanged(excludedCategories : IEnumerable[Text]) : void
        // {
        //     _indexedFilter.SetExclusions(_componentKey, excludedCategories);
        // }
        //
        // protected Categories : IEnumerable[Text] { get; set; };
        //
        // private GetExcludedCategories() : IEnumerable[Text]
        // {
        //     Elements.Where(vm => !vm.IsActive).Select(vm => vm.Category)
        // }
        //
        // private RefreshExcludedCategories() : void
        // {
        //     OnExcludedCategoriesChanged(GetExcludedCategories());
        // }
        //
        // private Subscribe(indexer : GenericIndexer,  syncContext : SynchronizationContext) : void
        // {
        //     indexer.NewComponentFound += _ => 
        //                                  syncContext.Post(_ => 
        //         {
        //                                                   foreach(t in indexer.GetAllComponents(_componentKey).ToList().Except(Categories))
        //                                                       AddNewCategory(t);
        //         }, null);
        // }
        //
        // private _elementFactory : (Text*bool) -> ElementViewModel;
        // private _componentKey : string;
        // private _indexedFilter : IndexedFilter;
    }
}
