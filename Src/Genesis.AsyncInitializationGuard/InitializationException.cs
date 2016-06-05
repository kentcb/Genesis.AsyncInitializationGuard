namespace Genesis.AsyncInitializationGuard
{
    using System;

    /// <summary>
    /// Indicates an error during initialization of an <see cref="InitializationGuard"/>.
    /// </summary>
    public sealed class InitializationException : Exception
    {
        /// <summary>
        /// Creates a new instance of the <c>InitializationException</c> class.
        /// </summary>
        public InitializationException()
        {
        }

        /// <summary>
        /// Creates a new instance of the <c>InitializationException</c> class.
        /// </summary>
        /// <param name="message">
        /// The message for the exception.
        /// </param>
        public InitializationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance of the <c>InitializationException</c> class.
        /// </summary>
        /// <param name="message">
        /// The message for the exception.
        /// </param>
        /// <param name="innerException">
        /// The inner exception.
        /// </param>
        public InitializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}