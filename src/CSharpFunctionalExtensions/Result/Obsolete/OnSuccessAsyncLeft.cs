using System;
using System.Threading.Tasks;

namespace CSharpFunctionalExtensions
{
    public static partial class AsyncResultExtensionsLeftOperand
    {
        [Obsolete("Use Map() instead.")]
        public static Task<Result<K, E>> OnSuccess<T, K, E>(this Task<Result<T, E>> resultTask, Func<T, K> func)
            => Map(resultTask, func);

        [Obsolete("Use Map() instead.")]
        public static Task<Result<K>> OnSuccess<T, K>(this Task<Result<T>> resultTask, Func<T, K> func)
            => Map(resultTask, func);

        [Obsolete("Use Map() instead.")]
        public static Task<Result<K>> OnSuccess<K>(this Task<Result> resultTask, Func<K> func)
            => Map(resultTask, func);

        [Obsolete("Use Bind() instead.")]
        public static Task<Result<K, E>> OnSuccess<T, K, E>(this Task<Result<T, E>> resultTask, Func<T, Result<K, E>> func)
            => Bind(resultTask, func);

        [Obsolete("Use Bind() instead.")]
        public static Task<Result<K>> OnSuccess<T, K>(this Task<Result<T>> resultTask, Func<T, Result<K>> func)
            => Bind(resultTask, func);

        [Obsolete("Use Bind() instead.")]
        public static Task<Result<K>> OnSuccess<K>(this Task<Result> resultTask, Func<Result<K>> func)
            => Bind(resultTask, func);

        [Obsolete("Use Bind() instead.")]
        public static Task<Result> OnSuccess<T>(this Task<Result<T>> resultTask, Func<T, Result> func)
            => Bind(resultTask, func);

        [Obsolete("Use Bind() instead.")]
        public static Task<Result> OnSuccess(this Task<Result> resultTask, Func<Result> func)
            => Bind(resultTask, func);

        [Obsolete("Use Tap() instead.")]
        public static Task<Result<T>> OnSuccess<T>(this Task<Result<T>> resultTask, Action<T> action)
            => Tap(resultTask, action);

        [Obsolete("Use Tap() instead.")]
        public static Task<Result> OnSuccess(this Task<Result> resultTask, Action action)
            => Tap(resultTask, action);
    }
}
