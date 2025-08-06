using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using TaxCalculator.Services.Interfaces;

namespace TaxCalculator.Services.Services
{
    public class CacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, CacheItem> _cache = new ConcurrentDictionary<string, CacheItem>();

        public Task<T> GetAsync<T>(string key) where T : class
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (item.ExpiryTime > DateTime.UtcNow)
                {
                    return Task.FromResult(item.Value as T);
                }
                else
                {
                    _cache.TryRemove(key, out _);
                }
            }
            return Task.FromResult<T>(null);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
        {
            var item = new CacheItem
            {
                Value = value,
                ExpiryTime = DateTime.UtcNow.Add(expiration)
            };
            _cache.AddOrUpdate(key, item, (k, v) => item);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _cache.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        private class CacheItem
        {
            public object Value { get; set; }
            public DateTime ExpiryTime { get; set; }
        }
    }
}
