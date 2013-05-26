using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace PersonalizeBalanceCard
{
    [Serializable]
    public class WtfException : Exception
    {
        public WtfException()
        {
        }

        public WtfException(string message)
            : base(message)
        {
        }

        protected WtfException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public WtfException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
