using System.Collections.Generic;
using System.Threading.Tasks;

namespace BrakePedal.NETStandard
{
    public interface IThrottlePolicy
    {
        string Name { get; set; }
        string[] Prefixes { get; set; }
        ICollection<Limiter> Limiters { get; set; }

        CheckResult Check(IThrottleKey key, bool increment = true);

        Task<CheckResult> CheckAsync(IThrottleKey key, bool increment = true);

        bool IsThrottled(IThrottleKey key, out CheckResult result, bool increment = true);

        Task<bool> IsThrottledAsync(IThrottleKey key, bool increment = true);

        bool IsLocked(IThrottleKey key, out CheckResult result, bool increment = true);

        Task<bool> IsLockedAsync(IThrottleKey key, bool increment = true);
    }
}