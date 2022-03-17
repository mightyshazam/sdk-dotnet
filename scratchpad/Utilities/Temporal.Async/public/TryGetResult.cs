using System;

namespace Temporal.Async
{
    // @ToDo: If we want to support returning these from workflow APIs, we must either implement IDataValue or find some other solution.
    public sealed class TryGetResult<T>
    {
        private readonly bool _isSuccess;
        private readonly T _result;

        public TryGetResult()
            : this(false, default(T))
        {
        }

        public TryGetResult(T result)
            : this(true, result)
        {
        }

        internal TryGetResult(bool isSuccess, T result)
        {
            _isSuccess = isSuccess;
            _result = result;
        }

        public T Result
        {
            get { return _result; }
        }

        public bool IsSuccess()
        {
            return IsSuccess(out _);
        }

        public bool IsSuccess(out T result)
        {
            result = _result;
            return _isSuccess;
        }
    }
}
