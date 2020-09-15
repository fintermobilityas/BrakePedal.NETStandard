﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;

namespace BrakePedal.NETStandard
{
    public class MemoryThrottleRepository : IThrottleRepository
    {
        private readonly IMemoryCache _store;

        // Setup as a function to allow for unit testing
        public Func<DateTime> CurrentDate = () => DateTime.UtcNow;

        public MemoryThrottleRepository(IMemoryCache cache)
        {
            _store = cache;
        }

        public MemoryThrottleRepository()
        {
            _store = new MemoryCache(new MemoryCacheOptions());
        }

        public object[] PolicyIdentityValues { get; set; }

        public long? GetThrottleCount(IThrottleKey key, Limiter limiter)
        {
            string id = CreateThrottleKey(key, limiter);

            var cacheItem = _store.Get(id) as ThrottleCacheItem;
            if (cacheItem != null)
            {
                return cacheItem.Count;
            }

            return null;
        }

        public Task<long?> GetThrottleCountAsync(IThrottleKey key, Limiter limiter)
            => Task.FromResult(GetThrottleCount(key, limiter));

        public void AddOrIncrementWithExpiration(IThrottleKey key, Limiter limiter)
        {
            string id = CreateThrottleKey(key, limiter);
            var cacheItem = _store.Get(id) as ThrottleCacheItem;

            if (cacheItem != null)
            {
                cacheItem.Count = cacheItem.Count + 1;
            }
            else
            {
                cacheItem = new ThrottleCacheItem()
                {
                    Count = 1,
                    Expiration = CurrentDate().Add(limiter.Period)
                };
            }

            _store.Set(id, cacheItem, cacheItem.Expiration);
        }

        public Task AddOrIncrementWithExpirationAsync(IThrottleKey key, Limiter limiter)
        {
            AddOrIncrementWithExpiration(key, limiter);
            return Task.CompletedTask;
        }

        public void SetLock(IThrottleKey key, Limiter limiter)
        {
            string throttleId = CreateThrottleKey(key, limiter);
            _store.Remove(throttleId);

            string lockId = CreateLockKey(key, limiter);
            DateTime expiration = CurrentDate().Add(limiter.LockDuration.Value);
            _store.Set(lockId, true, expiration);
        }

        public Task SetLockAsync(IThrottleKey key, Limiter limiter)
        {
            SetLock(key, limiter);
            return Task.CompletedTask;
        }

        public bool LockExists(IThrottleKey key, Limiter limiter)
        {
            string lockId = CreateLockKey(key, limiter);
            return _store.TryGetValue(lockId, out _);
        }

        public Task<bool> LockExistsAsync(IThrottleKey key, Limiter limiter)
            => Task.FromResult(LockExists(key, limiter));

        public void RemoveThrottle(IThrottleKey key, Limiter limiter)
        {
            string lockId = CreateThrottleKey(key, limiter);
            _store.Remove(lockId);
        }

        public Task RemoveThrottleAsync(IThrottleKey key, Limiter limiter)
        {
            RemoveThrottle(key, limiter);
            return Task.CompletedTask;
        }

        public string CreateLockKey(IThrottleKey key, Limiter limiter)
        {
            List<object> values = CreateBaseKeyValues(key, limiter);

            string lockKeySuffix = TimeSpanToFriendlyString(limiter.LockDuration.Value);
            values.Add("lock");
            values.Add(lockKeySuffix);

            string id = string.Join(":", values);
            return id;
        }

        public Task<string> CreateLockKeyAsync(IThrottleKey key, Limiter limiter)
            => Task.FromResult(CreateLockKey(key, limiter));

        public string CreateThrottleKey(IThrottleKey key, Limiter limiter)
        {
            List<object> values = CreateBaseKeyValues(key, limiter);

            string countKey = TimeSpanToFriendlyString(limiter.Period);
            values.Add(countKey);

            // Using the Unix timestamp to the key allows for better
            // precision when querying a key from Redis
            if (limiter.Period.TotalSeconds == 1)
                values.Add(GetUnixTimestamp());

            string id = string.Join(":", values);
            return id;
        }

        public Task<string> CreateThrottleKeyAsync(IThrottleKey key, Limiter limiter)
            => Task.FromResult(CreateThrottleKey(key, limiter));

        private List<object> CreateBaseKeyValues(IThrottleKey key, Limiter limiter)
        {
            List<object> values = key.Values.ToList();
            if (PolicyIdentityValues != null && PolicyIdentityValues.Length > 0)
                values.InsertRange(0, PolicyIdentityValues);

            return values;
        }

        private string TimeSpanToFriendlyString(TimeSpan span)
        {
            var items = new List<string>();
            Action<double, string> ifNotZeroAppend = (value, key) =>
            {
                if (value != 0)
                    items.Add(string.Concat(value, key));
            };

            ifNotZeroAppend(span.Days, "d");
            ifNotZeroAppend(span.Hours, "h");
            ifNotZeroAppend(span.Minutes, "m");
            ifNotZeroAppend(span.Seconds, "s");

            return string.Join("", items);
        }

        private long GetUnixTimestamp()
        {
            TimeSpan timeSpan = (CurrentDate() - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }

        [Serializable]
        public class ThrottleCacheItem
        {
            public long Count { get; set; }

            public DateTime Expiration { get; set; }
        }
    }
}