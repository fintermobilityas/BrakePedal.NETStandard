using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;

using Xunit;

namespace BrakePedal.NETStandard.Tests
{
    public class MemoryThrottleRepositoryTests
    {
        public class AddOrIncrementWithExpirationMethod
        {
            [Fact]
            public void NewObject_SetsCountToOneWithExpiration()
            {
                // Arrange
                var key = new SimpleThrottleKey("test", "key");
                var limiter = new Limiter()
                    .Limit(1)
                    .Over(100);
                var cache = new MemoryCache(new MemoryCacheOptions());
                var repository = new MemoryThrottleRepository(cache);
                repository.CurrentDate = () => new DateTime(2030, 1, 1);

                string id = repository.CreateThrottleKey(key, limiter);

                // Act
                repository.AddOrIncrementWithExpiration(key, limiter);

                // Assert
                var item = (MemoryThrottleRepository.ThrottleCacheItem)cache.Get(id);
                Assert.Equal(1L, item.Count);
                // We're testing a future date by 100 seconds which is 40 seconds + 1 minute
                Assert.Equal(new DateTime(2030, 1, 1, 0, 1, 40), item.Expiration);
            }

            [Fact]
            public async Task NewObject_SetsCountToOneWithExpirationAsync()
            {
                // Arrange
                var key = new SimpleThrottleKey("test", "key");
                var limiter = new Limiter()
                    .Limit(1)
                    .Over(100);
                var cache = new MemoryCache(new MemoryCacheOptions());
                var repository = new MemoryThrottleRepository(cache);
                repository.CurrentDate = () => new DateTime(2030, 1, 1);

                string id = await repository.CreateThrottleKeyAsync(key, limiter);

                // Act
                await repository.AddOrIncrementWithExpirationAsync(key, limiter);

                // Assert
                var item = (MemoryThrottleRepository.ThrottleCacheItem)cache.Get(id);
                Assert.Equal(1L, item.Count);
                // We're testing a future date by 100 seconds which is 40 seconds + 1 minute
                Assert.Equal(new DateTime(2030, 1, 1, 0, 1, 40), item.Expiration);
            }

            [Fact]
            public void ExistingObject_IncrementByOneAndSetExpirationDate()
            {
                // Arrange
                var key = new SimpleThrottleKey("test", "key");
                var limiter = new Limiter()
                    .Limit(1)
                    .Over(100);
                var cache = new MemoryCache(new MemoryCacheOptions());
                var repository = new MemoryThrottleRepository(cache);
                string id = repository.CreateThrottleKey(key, limiter);

                var cacheItem = new MemoryThrottleRepository.ThrottleCacheItem()
                {
                    Count = 1,
                    Expiration = new DateTime(2030, 1, 1)
                };

                cache
                    .Set(id, cacheItem, cacheItem.Expiration);

                // Act
                repository.AddOrIncrementWithExpiration(key, limiter);

                // Assert
                var item = (MemoryThrottleRepository.ThrottleCacheItem)cache.Get(id);
                Assert.Equal(2L, item.Count);
                Assert.Equal(new DateTime(2030, 1, 1), item.Expiration);
            }

            [Fact]
            public async Task ExistingObject_IncrementByOneAndSetExpirationDateAsync()
            {
                // Arrange
                var key = new SimpleThrottleKey("test", "key");
                var limiter = new Limiter()
                    .Limit(1)
                    .Over(100);
                var cache = new MemoryCache(new MemoryCacheOptions());
                var repository = new MemoryThrottleRepository(cache);
                string id = repository.CreateThrottleKey(key, limiter);

                var cacheItem = new MemoryThrottleRepository.ThrottleCacheItem()
                {
                    Count = 1,
                    Expiration = new DateTime(2030, 1, 1)
                };

                cache
                    .Set(id, cacheItem, cacheItem.Expiration);

                // Act
                await repository.AddOrIncrementWithExpirationAsync(key, limiter);

                // Assert
                var item = (MemoryThrottleRepository.ThrottleCacheItem)cache.Get(id);
                Assert.Equal(2L, item.Count);
                Assert.Equal(new DateTime(2030, 1, 1), item.Expiration);
            }

            [Fact]
            public void RetrieveValidThrottleCountFromRepostitory()
            {
                // Arrange
                var key = new SimpleThrottleKey("test", "key");
                var limiter = new Limiter()
                    .Limit(1)
                    .Over(100);
                var cache = new MemoryCache(new MemoryCacheOptions());
                var repository = new MemoryThrottleRepository(cache);
                string id = repository.CreateThrottleKey(key, limiter);

                var cacheItem = new MemoryThrottleRepository.ThrottleCacheItem()
                {
                    Count = 1,
                    Expiration = new DateTime(2030, 1, 1)
                };

                repository.AddOrIncrementWithExpiration(key, limiter);

                // Act
                var count = repository.GetThrottleCount(key, limiter);

                // Assert
                Assert.Equal(1, count);
            }

            [Fact]
            public async Task RetrieveValidThrottleCountFromRepostitoryAsync()
            {
                // Arrange
                var key = new SimpleThrottleKey("test", "key");
                var limiter = new Limiter()
                    .Limit(1)
                    .Over(100);
                var cache = new MemoryCache(new MemoryCacheOptions());
                var repository = new MemoryThrottleRepository(cache);
                string id = repository.CreateThrottleKey(key, limiter);

                var cacheItem = new MemoryThrottleRepository.ThrottleCacheItem()
                {
                    Count = 1,
                    Expiration = new DateTime(2030, 1, 1)
                };

                await repository.AddOrIncrementWithExpirationAsync(key, limiter);

                // Act
                var count = await repository.GetThrottleCountAsync(key, limiter);

                // Assert
                Assert.Equal(1, count);
            }

            [Fact]
            public void ThrottleCountReturnsNullWhenUsingInvalidKey()
            {
                // Arrange
                var key = new SimpleThrottleKey("test", "key");
                var limiter = new Limiter()
                    .Limit(1)
                    .Over(100);
                var cache = new MemoryCache(new MemoryCacheOptions());
                var repository = new MemoryThrottleRepository(cache);

                // Act
                var count = repository.GetThrottleCount(key, limiter);

                // Assert
                Assert.Null(count);
            }

            [Fact]
            public async Task ThrottleCountReturnsNullWhenUsingInvalidKeyAsync()
            {
                // Arrange
                var key = new SimpleThrottleKey("test", "key");
                var limiter = new Limiter()
                    .Limit(1)
                    .Over(100);
                var cache = new MemoryCache(new MemoryCacheOptions());
                var repository = new MemoryThrottleRepository(cache);

                // Act
                var count = await repository.GetThrottleCountAsync(key, limiter);

                // Assert
                Assert.Null(count);
            }
        }

        public class ThrottleCacheItemTests
        {
            [Fact]
            public void HasSerializableAttribute()
            {
                // Arrange
                var type = typeof(MemoryThrottleRepository.ThrottleCacheItem);

                // Act
                var result = type.IsDefined(typeof(SerializableAttribute), false);

                // Assert
                Assert.True(result);
            }
        }
    }
}