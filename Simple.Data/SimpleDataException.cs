using System;
using System.Runtime.Serialization;

namespace Simple.Data
{
    [Serializable]
    public class SimpleDataException : Exception
    {
        public SimpleDataException()
        {
        }

        public SimpleDataException(string message) : base(message)
        {
        }

        public SimpleDataException(string message, Exception inner) : base(message, inner)
        {
        }

        protected SimpleDataException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}