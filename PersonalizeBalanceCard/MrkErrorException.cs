using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace PersonalizeBalanceCard
{
    [Serializable]
    public class MrkErrorException : Exception
    {
        public readonly lastError Error;

        public MrkErrorException()
        {
        }

        public MrkErrorException(string message)
            : base(message)
        {
        }

        public MrkErrorException(lastError error)
        {
            this.Error = error;
        }

        protected MrkErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public MrkErrorException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
