using System.Threading.Tasks;

namespace Temporal.Common
{
    public interface IDataValue
    {
        public sealed class Void : IDataValue
        {
            public static readonly Void Instance = new Void();
            public static readonly Task<Void> CompletedTask = Task.FromResult(Instance);
        }
    }
}
