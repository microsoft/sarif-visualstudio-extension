using System;
using System.Threading.Tasks;

namespace CSharpFunctionalExtensions
{
    public static partial class ResultExtensions
    {
        public static async Task<Result> OnSuccessTry(this Task<Result> task, Action action,
            Func<Exception, string> errorHandler = null)
        {
            var result = await task.DefaultAwait();
            return result.OnSuccessTry(action, errorHandler);
        }

        public static async Task<Result<T>> OnSuccessTry<T>(this Task<Result> task, Func<T> func,
            Func<Exception, string> errorHandler = null)
        {
            var result = await task.DefaultAwait();
            return result.OnSuccessTry(func, errorHandler);
        }

        public static async Task<Result<T, E>> OnSuccessTry<T, E>(this Task<Result<T, E>> task, Func<T> func,
            Func<Exception, E> errorHandler = null)
        {
            var result = await task.DefaultAwait();
            return result.OnSuccessTry(func, errorHandler);
        }

        public static async Task<Result> OnSuccessTry<T>(this Task<Result<T>> task, Action<T> action,
            Func<Exception, string> errorHandler = null)
        {
            var result = await task.DefaultAwait();
            return result.OnSuccessTry(action, errorHandler);
        }

        public static async Task<Result<K>> OnSuccessTry<T, K>(this Task<Result<T>> task, Func<T, K> action,
            Func<Exception, string> errorHandler = null)
        {
            var result = await task.DefaultAwait();
            return result.OnSuccessTry(action, errorHandler);
        }

        public static async Task<Result<K, E>> OnSuccessTry<T, K, E>(this Task<Result<T, E>> task, Func<T, K> action,
            Func<Exception, E> errorHandler = null)
        {
            var result = await task.DefaultAwait();
            return result.OnSuccessTry(action, errorHandler);
        }
    }
}
