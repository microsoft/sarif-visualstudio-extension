﻿using System;
using System.Threading.Tasks;

namespace CSharpFunctionalExtensions
{
    public partial struct Result
    {
        /// <summary>
        ///     Creates a result whose success/failure reflects the supplied condition. Opposite of FailureIf().
        /// </summary>
        public static Result SuccessIf(bool isSuccess, string error)
        {
            return isSuccess
                ? Success()
                : Failure(error);
        }

        /// <summary>
        ///     Creates a result whose success/failure depends on the supplied predicate. Opposite of FailureIf().
        /// </summary>
        public static Result SuccessIf(Func<bool> predicate, string error)
        {
            return SuccessIf(predicate(), error);
        }

        /// <summary>
        ///     Creates a result whose success/failure depends on the supplied predicate. Opposite of FailureIf().
        /// </summary>
        public static async Task<Result> SuccessIf(Func<Task<bool>> predicate, string error)
        {
            bool isSuccess = await predicate().DefaultAwait();
            return SuccessIf(isSuccess, error);
        }

        /// <summary>
        ///     Creates a result whose success/failure reflects the supplied condition. Opposite of FailureIf().
        /// </summary>
        public static Result<T> SuccessIf<T>(bool isSuccess, T value, string error)
        {
            return isSuccess
                ? Success(value)
                : Failure<T>(error);
        }

        /// <summary>
        ///     Creates a result whose success/failure depends on the supplied predicate. Opposite of FailureIf().
        /// </summary>
        public static Result<T> SuccessIf<T>(Func<bool> predicate, T value, string error)
        {
            return SuccessIf(predicate(), value, error);
        }

        /// <summary>
        ///     Creates a result whose success/failure depends on the supplied predicate. Opposite of FailureIf().
        /// </summary>
        public static async Task<Result<T>> SuccessIf<T>(Func<Task<bool>> predicate, T value, string error)
        {
            bool isSuccess = await predicate().DefaultAwait();
            return SuccessIf(isSuccess, value, error);
        }

        /// <summary>
        ///     Creates a result whose success/failure reflects the supplied condition. Opposite of FailureIf().
        /// </summary>
        public static Result<T, E> SuccessIf<T, E>(bool isSuccess, T value, E error)
        {
            return isSuccess
                ? Success<T, E>(value)
                : Failure<T, E>(error);
        }

        /// <summary>
        ///     Creates a result whose success/failure depends on the supplied predicate. Opposite of FailureIf().
        /// </summary>
        public static Result<T, E> SuccessIf<T, E>(Func<bool> predicate, T value, E error)
        {
            return SuccessIf(predicate(), value, error);
        }

        /// <summary>
        ///     Creates a result whose success/failure depends on the supplied predicate. Opposite of FailureIf().
        /// </summary>
        public static async Task<Result<T, E>> SuccessIf<T, E>(Func<Task<bool>> predicate, T value, E error)
        {
            bool isSuccess = await predicate().DefaultAwait();
            return SuccessIf(isSuccess, value, error);
        }
    }

    /// <summary>
    /// Alternative entrypoint for <see cref="UnitResult{E}" /> to avoid ambiguous calls
    /// </summary>
    public static partial class UnitResult
    {
        /// <summary>
        ///     Creates a result whose success/failure reflects the supplied condition. Opposite of FailureIf().
        /// </summary>
        public static UnitResult<E> SuccessIf<E>(bool isSuccess, E error)
        {
            return isSuccess
                ? Success<E>()
                : Failure(error);
        }

        /// <summary>
        ///     Creates a result whose success/failure depends on the supplied predicate. Opposite of FailureIf().
        /// </summary>
        public static UnitResult<E> SuccessIf<E>(Func<bool> predicate, E error)
        {
            return SuccessIf(predicate(), error);
        }

        /// <summary>
        ///     Creates a result whose success/failure depends on the supplied predicate. Opposite of FailureIf().
        /// </summary>
        public static async Task<UnitResult<E>> SuccessIf<E>(Func<Task<bool>> predicate, E error)
        {
            bool isSuccess = await predicate().DefaultAwait();
            return SuccessIf(isSuccess, error);
        }
    }
}
