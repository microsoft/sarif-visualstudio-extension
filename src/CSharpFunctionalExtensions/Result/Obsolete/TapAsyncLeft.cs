using System;
using System.Threading.Tasks;

namespace CSharpFunctionalExtensions
{
    public static partial class AsyncResultExtensionsLeftOperand
    {
        [Obsolete("Use Check() instead.")]
        public static Task<Result<T>> Tap<T>(this Task<Result<T>> resultTask, Func<T, Result> func) =>
            Check(resultTask, func);

        [Obsolete("Use Check() instead.")]
        public static Task<Result<T>> Tap<T, K>(this Task<Result<T>> resultTask, Func<T, Result<K>> func) =>
            Check(resultTask, func);

        [Obsolete("Use Check() instead.")]
        public static Task<Result<T, E>> Tap<T, K, E>(this Task<Result<T, E>> resultTask, Func<T, Result<K, E>> func) =>
            Check(resultTask, func);
    }
}
