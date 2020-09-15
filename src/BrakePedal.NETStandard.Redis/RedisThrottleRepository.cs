﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using StackExchange.Redis;

namespace BrakePedal.NETStandard.Redis
{
    public class RedisThrottleRepository : IThrottleRepository
    {
        private readonly IDatabase _db;

        public RedisThrottleRepository(IDatabase database)
        {
            _db = database;
        }

        public object[] PolicyIdentityValues { get; set; }

        public long? GetThrottleCount(IThrottleKey key, Limiter limiter)
        {
            string id = CreateThrottleKey(key, limiter);
            RedisValue value = _db.StringGet(id);
            long convert;
            if (long.TryParse(value, out convert))
                return convert;

            return null;
        }

        public async Task<long?> GetThrottleCountAsync(IThrottleKey key, Limiter limiter)
        {
            string id = CreateThrottleKey(key, limiter);
            RedisValue value = await _db.StringGetAsync(id);
            long convert;
            if (long.TryParse(value, out convert))
                return convert;

            return null;
        }

        public void AddOrIncrementWithExpiration(IThrottleKey key, Limiter limiter)
        {
            string id = CreateThrottleKey(key, limiter);

            long result = _db.StringIncrement(id);

            // If we get back 1, that means the key was incremented as it
            // was expiring or it's a new key. Ensure we set the expiration.
            if (result == 1)
                _db.KeyExpire(id, limiter.Period);
        }

        public async Task AddOrIncrementWithExpirationAsync(IThrottleKey key, Limiter limiter)
        {
            string id = CreateThrottleKey(key, limiter);

            long result = await _db.StringIncrementAsync(id);

            // If we get back 1, that means the key was incremented as it
            // was expiring or it's a new key. Ensure we set the expiration.
            if (result == 1)
            {
                await _db.KeyExpireAsync(id, limiter.Period);
            }
        }

        public bool LockExists(IThrottleKey key, Limiter limiter)
        {
            string id = CreateLockKey(key, limiter);
            return _db.KeyExists(id);
        }

        public Task<bool> LockExistsAsync(IThrottleKey key, Limiter limiter)
        {
            string id = CreateLockKey(key, limiter);
            return _db.KeyExistsAsync(id);
        }

        public void SetLock(IThrottleKey key, Limiter limiter)
        {
            string id = CreateLockKey(key, limiter);
            ITransaction trans = _db.CreateTransaction();
            trans.StringIncrementAsync(id);
            trans.KeyExpireAsync(id, limiter.LockDuration);
            trans.Execute();
        }

        public async Task SetLockAsync(IThrottleKey key, Limiter limiter)
        {
            string id = CreateLockKey(key, limiter);
            ITransaction trans = _db.CreateTransaction();
            await trans.StringIncrementAsync(id);
            await trans.KeyExpireAsync(id, limiter.LockDuration);
            await trans.ExecuteAsync();
        }

        public void RemoveThrottle(IThrottleKey key, Limiter limiter)
        {
            string id = CreateThrottleKey(key, limiter);
            _db.KeyDelete(id);
        }

        public Task RemoveThrottleAsync(IThrottleKey key, Limiter limiter)
        {
            string id = CreateThrottleKey(key, limiter);
            return _db.KeyDeleteAsync(id);
        }

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
        
        public string CreateLockKey(IThrottleKey key, Limiter limiter)
        {
            List<object> values = CreateBaseKeyValues(key, limiter);

            string lockKeySuffix = TimeSpanToFriendlyString(limiter.LockDuration.Value);
            values.Add("lock");
            values.Add(lockKeySuffix);

            string id = string.Join(":", values);
            return id;
        }     

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
            TimeSpan timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }
    }
}