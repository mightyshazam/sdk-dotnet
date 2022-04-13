using System.Threading.Tasks;

namespace Temporal.Common
{
    public interface IPayload
    {
        public sealed class Void : IPayload
        {
            public static readonly Void Instance = new Void();
            public static readonly Task<Void> CompletedTask = Task.FromResult(Instance);
        }
    }
}
