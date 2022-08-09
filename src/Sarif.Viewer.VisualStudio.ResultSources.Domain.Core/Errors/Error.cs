// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Errors
{
    /// <summary>
    /// Represents a service error.
    /// </summary>
    public abstract class Error
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public Error(string message)
        {
            this.Message = message;
        }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <value>The error message.</value>
        public string Message { get; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <param name="error">The <see cref="Error"/>.</param>
        public static implicit operator string(Error error)
        {
            return error.Message;
        }
    }
}
