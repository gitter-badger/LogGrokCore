using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LogGrokCore.Data.IndexTree;

namespace LogGrokCore.Data.Index
{

    public class Indexer : IDisposable
    {
        private readonly ConcurrentDictionary<IndexKey, IndexTree<int, SimpleLeaf<int>>> _indices =
            new(1, 16384);

        private readonly ConcurrentDictionary<int, HashSet<IndexKey>> _components = new();
        
        private readonly CountIndex<IndexTree<int, SimpleLeaf<int>>> _countIndex 
            = new();
        
        public IEnumerable<string> GetAllComponents(int componentNumber)
        {
            return _components[componentNumber].Select(key => key.GetComponent(componentNumber).ToString()); 
        }

        public IIndexedLinesProvider GetIndexedLinesProvider(IReadOnlyDictionary<int, IReadOnlyList<string>> excludedComponents)
        {
            return new IndexedLinesProvider(this, _countIndex.Counts, 
                CountIndex<IndexTree<int, SimpleLeaf<int>>>.Granularity, excludedComponents);
        }

        public IndexTree<int, SimpleLeaf<int>> GetIndex(IndexKey key) => _indices[key];
        
        public void Add(IndexKey key, int lineNumber)
        {
            var index = _indices.GetOrAdd(key,
                static indexedKey =>
                {
                    indexedKey.MakeLocalCopy();
                    return CreateIndexTree();
                });
            
            index.Add(lineNumber);    
            _countIndex.Add(lineNumber, _indices);

            var newIndexCreated = key.HasLocalBuffer;
            if (newIndexCreated)
                UpdateComponents(key);
        }

        public event Action<(int compnentNumber, IndexKey key)>? NewComponentAdded;
        
        private class ComponentComparer : IEqualityComparer<IndexKey>
        {
            private readonly int _index;

            public ComponentComparer(int index) => _index = index;

            public bool Equals(IndexKey? x, IndexKey? y) =>
                x != null && y != null &&
                x.GetComponent(_index).SequenceEqual(y.GetComponent(_index));

            public int GetHashCode(IndexKey obj) => string.GetHashCode(obj.GetComponent(_index));
        }
        
        private void UpdateComponents(IndexKey key)
        {
            for (var componentIndex = 0; componentIndex < key.ComponentCount; componentIndex++)
            {
                var componentSet = _components.GetOrAdd(componentIndex,
                    static index => new HashSet<IndexKey>(new ComponentComparer(index)));

                if (componentSet.Add(key))
                    NewComponentAdded?.Invoke((componentIndex, key)); 
                    
            }
        }

        private static IndexTree<int, SimpleLeaf<int>> CreateIndexTree()
        {
            return new IndexTree<int, SimpleLeaf<int>>(16, 
                static value => new SimpleLeaf<int>(value, 0));
        }

        public void Dispose()
        {
            _indices.Clear();
        }

        public void Finish()
        {
            _countIndex.Finish(_indices);
        }

        public Indexer CreateFilteredIndexer(IAsyncEnumerable<IEnumerable<int>> filterSequence)
        {
            var filteredIndexer = new Indexer();
            FillFilteredIndexer(this, filteredIndexer, filterSequence);
            return filteredIndexer;
        }

        private static async void FillFilteredIndexer(Indexer source, Indexer target,
            IAsyncEnumerable<IEnumerable<int>> filterSequences)
        {
            await foreach (var filterSequence in filterSequences)
            {
                using var filterEnumerator = filterSequence.GetEnumerator();
                if (!filterEnumerator.MoveNext()) continue;
                MergeAndFilterIndices(target, source._indices, filterEnumerator);
            }
        }

        private static void MergeAndFilterIndices(
            Indexer target,
            IDictionary<IndexKey, IndexTree<int, SimpleLeaf<int>>> source, 
            IEnumerator<int> filterEnumerator)
        {
            var cursors =
                source.Select(kv => kv.Value.GetEnumerableFromIndex(filterEnumerator.Current)
                        .Select(value => (kv.Key, value)).GetEnumerator())
                        .Where(c => c.MoveNext());
            
            var merged = CollectionUtlis.MergeSorted(cursors, (k1, k2) => k2.value == k1.value + 1);
            using var sourceSequenceEnumerator = merged.GetEnumerator();

            while (sourceSequenceEnumerator.MoveNext())
            {
                if (sourceSequenceEnumerator.Current.value != filterEnumerator.Current) continue;

                var (key, value) = sourceSequenceEnumerator.Current;
                target.Add(key, value);
                if (!filterEnumerator.MoveNext()) return;
            }
        }
    }
}