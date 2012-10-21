using System;
using System.Runtime.Serialization;

namespace Simple.Data.Ado
{
    [Serializable]
    public class SchemaResolutionException : SimpleDataException
    {
        public SchemaResolutionException()
        {
        }

        public SchemaResolutionException(string message)
            : base(message)
        {
        }

        public SchemaResolutionException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected SchemaResolutionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}