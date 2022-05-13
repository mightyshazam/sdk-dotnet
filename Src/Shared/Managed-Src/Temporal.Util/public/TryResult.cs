namespace Temporal.Common
{
    public static class TryResult
    {
        public static TryResult<T> ForSuccess<T>(T result)
        {
            return new TryResult<T>(isSuccess: true, result);
        }

        public static TryResult<T> ForFailure<T>()
        {
            return new TryResult<T>(isSuccess: false, default(T));
        }
    }

    public struct TryResult<T>
    {
        private readonly bool _isSuccess;
        private readonly T _result;

        internal TryResult(T result)
            : this(true, result)
        {
        }

        internal TryResult(bool isSuccess, T result)
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

        public static implicit operator bool(TryResult<T> tryResult)
        {
            return tryResult.IsSuccess();
        }
    }
}
