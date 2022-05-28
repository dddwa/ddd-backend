using System;
using System.Collections.Generic;
using System.Linq;

namespace DDD.Core.EloVoting
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> enumerable) {
            foreach (var cur in enumerable) {
                collection.Add(cur);
            }
        }
        
    }
    
    public class ShufflerConfig
    {
        public static readonly ShufflerConfig Default = new ShufflerConfig();
        
        // when the working set gets below this number of entries remaining, we will refill it from the base collection
        public long LowWatermark { get; set; } = 10;
        
        // the lock object to use for instances of the shuffler with the configured name
        public object Lock { get; set; } = new object();
    }
    
    public class EloVoteShuffler<T>
    {
        private readonly IList<T> _source;
        private readonly IList<T> _workingSet;
        private readonly ShufflerConfig _config;

        public EloVoteShuffler(ShufflerConfig config, IEnumerable<T> sourceSet)
        {
            _config = config;
            _source = sourceSet.ToList();

            _workingSet = new List<T>();
            _workingSet.AddRange(_source.OrderBy(x => Guid.NewGuid()));
        }
        
        public IEnumerable<T> Take(int count)
        {
            // Given Take modifies the underlying list, perform all of this inside the lock
            lock (_config.Lock)
            {
                if (_workingSet.Count < _config.LowWatermark)
                {
                    _workingSet.AddRange(_source.OrderBy(x => Guid.NewGuid()));
                }

                return _workingSet.Take(count);
            }
        }


        public bool Any()
        {
            return _workingSet.Any();
        }
    }
}