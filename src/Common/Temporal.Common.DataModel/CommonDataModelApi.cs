using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Temporal.Common.DataModel
{
    public class CommonDataModelApi
    {
    }

    public interface IDataValue
    {
        public sealed class Void : IDataValue
        {            
            public static readonly Void Instance = new Void();
            public static readonly Task<Void> CompletedTask = Task.FromResult(Instance);            
        }
    }

    public class PayloadsCollection : IReadOnlyList<Payload>
    {
        public static readonly PayloadsCollection Empty = new PayloadsCollection();

        public int Count { get; }
        public Payload this[int index] { get { return null; } }
        public IEnumerator<Payload> GetEnumerator() { return null; }
        IEnumerator IEnumerable.GetEnumerator() { return null; }
    }

    public class MutablePayloadsCollection : PayloadsCollection, ICollection<Payload>
    {
        public bool IsReadOnly { get { return false; } }
        public void Add(Payload item) { }
        public void Clear() { }
        public bool Contains(Payload item) { return false; }
        public void CopyTo(Payload[] array, int arrayIndex) { }
        public bool Remove(Payload item) { return false; }        
    }

    public class Payload
    {
        public IReadOnlyDictionary<string, Stream> Metadata { get; }
        public Stream Data { get; }
    }

    public enum WorkflowExecutionStatus
    {

    }

    public enum TimeoutType
    {
    }
}
