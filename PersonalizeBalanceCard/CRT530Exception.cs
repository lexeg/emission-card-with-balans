using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace CRT530Library
{
    public enum TypeError
    {
        errorCheckDispenser = 0x0,
        errorNoCard = 0x1,                  //Нет карт
        errorAnother = 0x3,                 //Продажа карт невозможна
        errorNo = 0x4,                        //Ошибок нет
        errorSale = 0x5
    }

    public class CRT530Exception : Exception
    {
        public readonly int Error;

        public CRT530Exception()
        {
        }

        public CRT530Exception(string message)
            : base(message)
        {
        }

        public CRT530Exception(int error)
        {
            this.Error = error;
        }

        public CRT530Exception(TypeError error)
        {
            this.Error = (int)error;
        }

        protected CRT530Exception(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public CRT530Exception(string message, Exception inner)
            : base(message, inner)
        {
        }

        public override string ToString()
        {
            return String.Format("Код ошибки: {0}\r\n{1}", this.Error, base.ToString());//base.ToString();
        }
    }
}
