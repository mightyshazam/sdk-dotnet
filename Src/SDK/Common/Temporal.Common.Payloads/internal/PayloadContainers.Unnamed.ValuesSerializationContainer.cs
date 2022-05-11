using System.Collections;
using System.Linq;

namespace Temporal.Common.Payloads
{
    public static partial class PayloadContainers
    {
        public static partial class Unnamed
        {
            internal struct ValuesSerializationContainer
            {
                private readonly PayloadContainers.IUnnamed _container;

                public ValuesSerializationContainer(PayloadContainers.IUnnamed container)
                {
                    _container = container;
                }

                public IEnumerable UnnamedPayloads
                {
                    get { return _container?.Values.Select((ue) => ue.ValueObject); }
                }
            }
        }
    }
}
