using System;
using System.Threading.Tasks;

namespace CSharpFunctionalExtensions
{
    public static partial class AsyncResultExtensionsRightOperand
    {
        /// <summary>
        ///     Passes the result to the given function (regardless of success/failure state) to yield a final output value.
        /// </summary>
        public static Task<T> Finally<T>(this Result result, Func<Result, Task<T>> func)
          => func(result);

        /// <summary>
        ///     Passes the result to the given function (regardless of success/failure state) to yield a final output value.
        /// </summary>
        public static Task<K> Finally<T, K>(this Result<T> result, Func<Result<T>, Task<K>> func)
          => func(result);

        /// <summary>
        ///     Passes the result to the given function (regardless of success/failure state) to yield a final output value.
        /// </summary>
        public static Task<K> Finally<K, E>(this UnitResult<E> result, Func<UnitResult<E>, Task<K>> func)
          => func(result);

        /// <summary>
        ///     Passes the result to the given function (regardless of success/failure state) to yield a final output value.
        /// </summary>
        public static Task<K> Finally<T, K, E>(this Result<T, E> result, Func<Result<T, E>, Task<K>> func)
          => func(result);
    }
}
