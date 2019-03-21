/*
FreshQuality is a tool that will:
a) analyze the attached libraries/projects of a test project, 
b) create instances of pertinent classes (via dependency injection), [INITIALLY ONLY CONTROLLERS]
c) expose them for use in unit tests.

The ability to how the dependency injection works allow for testing the classes
in a disconnect manner.
*/

using System;
using System.Runtime.Serialization;

namespace FreshQuality
{
    [Serializable]
    internal class MissingServiceException : Exception
    {
        public MissingServiceException()
        {
        }

        public MissingServiceException(string message) : base($"This type is missing an IOC initialization: {message}")
        {
        }

        public MissingServiceException(string message, Exception innerException) : base($"This type is missing an IOC initialization: {message}", innerException)
        {
        }

        protected MissingServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}