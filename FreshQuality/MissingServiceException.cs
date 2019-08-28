// <summary>
// FreshQuality is a tool that will:
// a) analyze the attached libraries/projects of a test project,
// b) create instances of pertinent classes(via dependency injection), initially only controllers, but this was extended.
// c) expose them for use in unit tests.
//
// The ability to how the dependency injection works allow for testing the classes
// in a disconnect manner.
// </summary>
// <copyright file="MissingServiceException.cs" company="Fresh Consulting LLC">2019</copyright>

namespace FreshQuality
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Provides a wrapper around IOC service exceptions
    /// </summary>
    [Serializable]
    public class MissingServiceException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MissingServiceException"/> class.
        /// </summary>
        public MissingServiceException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingServiceException"/> class.
        /// </summary>
        /// <param name="message">Exception message</param>
        public MissingServiceException(string message)
            : base($"This type is missing an IOC initialization: {message}")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingServiceException"/> class.
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">addtional exception details</param>
        public MissingServiceException(string message, Exception innerException)
            : base($"This type is missing an IOC initialization: {message}", innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingServiceException"/> class.
        /// </summary>
        /// <param name="info">serialization info</param>
        /// <param name="context">context info</param>
        protected MissingServiceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}