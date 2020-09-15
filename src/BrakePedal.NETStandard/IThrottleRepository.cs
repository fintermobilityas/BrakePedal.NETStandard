using System.Threading.Tasks;

namespace BrakePedal.NETStandard
{
    public interface IThrottleRepository
    {
        object[] PolicyIdentityValues { get; set; }

        long? GetThrottleCount(IThrottleKey key, Limiter limiter);

        Task<long?> GetThrottleCountAsync(IThrottleKey key, Limiter limiter);

        void AddOrIncrementWithExpiration(IThrottleKey key, Limiter limiter);

        Task AddOrIncrementWithExpirationAsync(IThrottleKey key, Limiter limiter);

        void SetLock(IThrottleKey key, Limiter limiter);

        Task SetLockAsync(IThrottleKey key, Limiter limiter);

        bool LockExists(IThrottleKey key, Limiter limiter);

        Task<bool> LockExistsAsync(IThrottleKey key, Limiter limiter);

        void RemoveThrottle(IThrottleKey key, Limiter limiter);

        Task RemoveThrottleAsync(IThrottleKey key, Limiter limiter);

        string CreateThrottleKey(IThrottleKey key, Limiter limiter);

        string CreateLockKey(IThrottleKey key, Limiter limiter);
    }
}