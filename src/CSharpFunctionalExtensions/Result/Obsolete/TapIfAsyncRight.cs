using System;
using System.Threading.Tasks;

namespace CSharpFunctionalExtensions
{
    public static partial class AsyncResultExtensionsRightOperand
    {
        [Obsolete("Use CheckIf() instead.")]
        public static Task<Result<T>> TapIf<T>(this Result<T> result, bool condition, Func<T, Task<Result>> func) =>
            CheckIf(result, condition, func);

        [Obsolete("Use CheckIf() instead.")]
        public static Task<Result<T>> TapIf<T, K>(this Result<T> result, bool condition, Func<T, Task<Result<K>>> func) =>
            CheckIf(result, condition, func);

        [Obsolete("Use CheckIf() instead.")]
        public static Task<Result<T, E>> TapIf<T, K, E>(this Result<T, E> result, bool condition, Func<T, Task<Result<K, E>>> func) =>
            CheckIf(result, condition, func);

        [Obsolete("Use CheckIf() instead.")]
        public static Task<Result<T>> TapIf<T>(this Result<T> result, Func<T, bool> predicate, Func<T, Task<Result>> func) =>
            CheckIf(result, predicate, func);

        [Obsolete("Use CheckIf() instead.")]
        public static Task<Result<T>> TapIf<T, K>(this Result<T> result, Func<T, bool> predicate, Func<T, Task<Result<K>>> func) =>
            CheckIf(result, predicate, func);

        [Obsolete("Use CheckIf() instead.")]
        public static Task<Result<T, E>> TapIf<T, K, E>(this Result<T, E> result, Func<T, bool> predicate, Func<T, Task<Result<K, E>>> func) =>
            CheckIf(result, predicate, func);
    }
}
