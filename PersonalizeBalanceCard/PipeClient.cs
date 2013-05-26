using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Pipes;

namespace PersonalizeBalanceCard
{
    public class PipeClient : IDisposable
    {
        private readonly Encoding _encoder = Encoding.GetEncoding("windows-1251");
        private string _incommingMessage;
        private Thread _thread;
        private const int BUFFER_SIZE = 0x1000;
        public const int CONNECT_TIMEOUT = 0x1388;
        private const string PIPE_NAME = "mrkpipe";


        public delegate void LoggerDelegate(string message, EventEntryType ev);
        public LoggerDelegate Logger;

        public void Dispose()
        {
            if (this._thread != null)
            {
                this._thread.Abort();
                this._thread = null;
            }
        }

        private void ReadFunction(object stream)
        {
            int count = 0;
            byte[] buffer = new byte[0x1000];
            try
            {
                count = ((NamedPipeClientStream)stream).Read(buffer, 0, 0x1000);
                this._incommingMessage = this._encoder.GetString(buffer, 0, count);
            }
            catch (Exception exception)
            {
                this._incommingMessage = string.Empty;
                Logger("PipeClient. Ошибка чтения ответа от МРК: " + exception.Message, EventEntryType.Error);
            }
        }

        public string SendPipeMessage(string message, int timeout, bool canWrite = true)
        {
            using (NamedPipeClientStream stream = new NamedPipeClientStream("mrkpipe"))
            {
                stream.Connect(0x1388);
                stream.ReadMode = PipeTransmissionMode.Message;
                byte[] bytes = this._encoder.GetBytes(message);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
                this.WriteEntry(string.Format("{0}++++++++++ PipeClient. Запрос ++++++++++{0}{1} ", Environment.NewLine, message), canWrite);
                Thread.Sleep(100);
                this._thread = new Thread(new ParameterizedThreadStart(this.ReadFunction));
                this._thread.Start(stream);
                if (!this._thread.Join(timeout))
                {
                    Logger("PipeClient. Превышен внутренний таймаут ответа от МРК", EventEntryType.CriticalError);
                    this._incommingMessage = string.Empty;
                    stream.Close();
                    this.Dispose();
                }
                if (this._incommingMessage == string.Empty)
                {
                    throw new WtfException(string.Format("PipeClient. null. Таймаут операции {0} мс.", timeout));
                }
                this.WriteEntry(string.Format("{0}++++++++++ PipeClient. Ответ от МРК ++++++++++{0}{1}", Environment.NewLine, this._incommingMessage), canWrite);
                return this._incommingMessage;
            }
        }

        public string SendPipeMessage(CardOperationType cardType, string message, int timeout, bool canWrite = true)
        {
            using (NamedPipeClientStream stream = new NamedPipeClientStream("mrkpipe"))
            {
                stream.Connect(0x1388);
                stream.ReadMode = PipeTransmissionMode.Message;
                byte[] bytes = this._encoder.GetBytes(message);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
                this.WriteEntry(string.Format("{0} ++++++++++ PipeClient. Запрос: [{2}] ++++++++++ {0}{1}", Environment.NewLine, message, cardType), canWrite);
                Thread.Sleep(100);
                this._thread = new Thread(new ParameterizedThreadStart(this.ReadFunction));
                this._thread.Start(stream);
                string str = string.Format("{0} ++++++++++ PipeClient. Ответ от МРК на запрос [{1}] ++++++++++ {0}", Environment.NewLine, cardType);
                bool flag = false;
                if (!this._thread.Join(timeout))
                {
                    flag = true;
                    this._incommingMessage = string.Empty;
                    stream.Close();
                    this.Dispose();
                }
                if (this._incommingMessage == string.Empty)
                {
                    str = str + (flag ? string.Format("MRK_TIME_OUT. PipeClient. МРК не ответил за отведенное время, прерываем поток чтения. Таймаут для этой операции {0} мс. ", timeout) : "MRK_NULL. PipeClient. Прочитана нулевая строка.");
                    this.WriteEntry(str, canWrite);
                    throw new WtfException("WTF");
                }
                this.WriteEntry(string.Format("{0}{1}", str, this._incommingMessage), canWrite);
                return this._incommingMessage;
            }
        }

        private string SubstringString(string message)
        {
            if (message.Contains("<track2>"))
            {
                int index = message.IndexOf("<track2>");
                int startIndex = message.IndexOf("</track2>");
                return (message.Substring(0, index + 8) + "***" + message.Substring(startIndex, message.Length - startIndex));
            }
            return message;
        }

        private void WriteEntry(string message, bool canWrite)
        {
            if (canWrite)
            {
                Logger(message, EventEntryType.Event);
            }
        }
    }
}
