using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace PersonalizeBalanceCard
{
    [Serializable]
    public class SerializeException : Exception
    {
        public SerializeException()
        {
        }

        public SerializeException(string message)
            : base(message)
        {
        }

        protected SerializeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public SerializeException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
