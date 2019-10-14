using System;

namespace IO.Ably
{
    // https://github.com/vkhorikov/FuntionalPrinciplesCsharp/blob/master/New/CustomerManagement.Logic/Common/Result.cs
    // Slightly modified version of the above

    /// <summary>
    /// Result class representing the result of an operation.
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Is the operation successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Error if the operation failed.
        /// </summary>
        public ErrorInfo Error { get; }

        /// <summary>
        /// Has the operation failed.
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Initializes a new instance of the <see cref="Result"/> class.
        /// </summary>
        /// <param name="isSuccess">true if success.</param>
        /// <param name="error">error if failure.</param>
        /// <exception cref="InvalidOperationException">for invalid parameters.</exception>
        protected Result(bool isSuccess, ErrorInfo error)
        {
            if (isSuccess && error != null)
            {
                throw new InvalidOperationException();
            }

            if (!isSuccess && error == null)
            {
                throw new InvalidOperationException();
            }

            IsSuccess = isSuccess;
            Error = error;
        }

        /// <summary>
        /// Factory method to create a failed Result with message.
        /// </summary>
        /// <param name="message">errro message.</param>
        /// <returns>Result.</returns>
        public static Result Fail(string message)
        {
            return new Result(false, new ErrorInfo() { Message = message });
        }

        /// <summary>
        /// Factory method to create a failed Result with Error.
        /// </summary>
        /// <param name="error">Error.</param>
        /// <returns>true / false.</returns>
        public static Result Fail(ErrorInfo error)
        {
            return new Result(false, error);
        }

        /// <summary>
        /// Factory method to create a failed Result of T.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="error">Error of the failure.</param>
        /// <returns>failed Result of T.</returns>
        public static Result<T> Fail<T>(ErrorInfo error)
        {
            return new Result<T>(default(T), false, error);
        }

        /// <summary>
        /// Factory method to create a successful Result.
        /// </summary>
        /// <returns>Result.</returns>
        public static Result Ok()
        {
            return new Result(true, null);
        }

       /// <summary>
       /// Factory method to create a successful Result of T with value.
       /// </summary>
       /// <typeparam name="T">Type of value.</typeparam>
       /// <param name="value">successful value held in the result.</param>
       /// <returns>Result.</returns>
        public static Result<T> Ok<T>(T value)
        {
            return new Result<T>(value, true, null);
        }

        /// <summary>
        /// Combines a number of results. If any of them has failed it returns failure otherwise
        /// returns a successful result.
        /// </summary>
        /// <param name="results">Results to check.</param>
        /// <returns>Result.</returns>
        public static Result Combine(params Result[] results)
        {
            foreach (Result result in results)
            {
                if (result.IsFailure)
                {
                    return result;
                }
            }

            return Ok();
        }
    }

    /// <summary>
    /// Class representing a <see cref="Result"/> containing a value.
    /// </summary>
    /// <typeparam name="T">Type of value.</typeparam>
    public class Result<T> : Result
    {
        private readonly T _value;

        /// <summary>
        /// When successful exposes the value
        /// held in the result object.
        /// </summary>
        public T Value
        {
            get
            {
                if (!IsSuccess)
                {
                    throw new InvalidOperationException();
                }

                return _value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Result{T}"/> class.
        /// </summary>
        /// <param name="value">value.</param>
        /// <param name="isSuccess">if successful.</param>
        /// <param name="error">Error object.</param>
        protected internal Result(T value, bool isSuccess, ErrorInfo error)
            : base(isSuccess, error)
        {
            _value = value;
        }
    }
}
