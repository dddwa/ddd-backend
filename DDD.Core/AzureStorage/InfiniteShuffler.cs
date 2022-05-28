using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DDD.Core.AzureStorage;

namespace DDD.Core.AzureStorage
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
        public static readonly ShufflerConfig Default = new ShufflerConfig()
        {
            Name = "Default"
        };
        
        public string Name { get; set; }
        // when the working set gets below this number of entries remaining, we will refill it from the base collection
        public long LowWatermark { get; set; } = 10;
        
        // the lock object to use for instances of the shuffler with the configured name
        public object Lock { get; set; } = new object();
    }
    
    public class InfiniteShuffler<T> : List<T>
    {
        public readonly double LowWatermark = 0.05;
        
        private readonly IList<T> _source;
        private readonly IList<T> _workingSet;
        public ShufflerConfig Config { get; set; }

        public InfiniteShuffler(ShufflerConfig config, IEnumerable<T> sourceSet)
        {
            Config = config;
            _source = sourceSet.ToList();

            _workingSet = new List<T>();
            _workingSet.AddRange(_source.OrderBy(x => Guid.NewGuid()));
        }
        
        public IEnumerable<T> Take(int count)
        {
            lock (Config.Lock)
            {
                if (_workingSet.Count < Config.LowWatermark)
                {
                    _workingSet.AddRange(_source.OrderBy(x => Guid.NewGuid()));
                }

                return _workingSet.Take(count);
            }
        }
    }
}