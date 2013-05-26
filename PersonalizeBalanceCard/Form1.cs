using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;

public enum DisppenserStatus
{
    DispenserError,
    NoCard,
    FewCard,
    ManyCard
}

public enum CardOperationType
{
    reqStatus,
    reqWaitCard,
    reqCancelWaitCard,
    reqSaleCard,
    Balance,
    reqChangePin,
    reqWriteCard,
    reqPayment,
    reqControl_reader_1,
    Info
}

public enum EventEntryType
{
    CriticalError = 4,
    Error = 3,
    Event = 1,
    Warning = 2
}

namespace PersonalizeBalanceCard
{
    public partial class Form1 : Form
    {
        private bool _cancel;
        private static object _lockObject=new object();
        private static Dictionary<int, string> _errorsDescription=new Dictionary<int,string>();
        private int _operationID;
        private String reqStatus="<?xml version='1.0' encoding='windows-1251'?>\r\n<TCLib version='3.04'>\r\n\t<info>\r\n\t\t<reqStatus/>\r\n\t</info>\r\n</TCLib>";

        private string OperationID
        {
            get
            {
                int num = this._operationID++;
                return num.ToString();
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private static void Init()
        {
            _errorsDescription.Add(2, "Сбой карты или карта не может быть обслужена в этом терминале.");
            _errorsDescription.Add(3, "Терминал в данный момент не работает, приносим свои извинения.");
            _errorsDescription.Add(4, "Повторите  операцию  через 1-2  минуты, приносим  свои  извинения.");
            _errorsDescription.Add(5, "Превышено время операции, просим повторить операцию.");
            _errorsDescription.Add(6, "Нарушение последовательности операций, Повторите операцию. Порядок операции описан  в меню \x00abПомощь\x00bb.");
            _errorsDescription.Add(7, "Нет возможности выполнить данную операцию.");
            _errorsDescription.Add(8, "Нет возможности выполнить данную операцию. Повторите операцию.");
            _errorsDescription.Add(9, "Приносим свои извинения,  повторите операцию или  обратитесь в другой терминал.");
            _errorsDescription.Add(10, "Заберите свою карту и повторите операцию или обратитесь в другой терминал.");
            _errorsDescription.Add(0x65, "Заберите свою карту и повторите операцию или обратитесь в другой терминал.");
        }

        private string CreateDate(string date)
        {
            try
            {
                return DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("dd.MM.yyyy");
            }
            catch (Exception)
            {
                return "xx.xx.xxxx";
            }
        }


        private object ExtractMessage(string message, Type type)
        {
            AnsError error = (AnsError)XmlHelper.ExtractMessage(message, typeof(AnsError));
            if (error.error != null)
            {
                throw new MrkErrorException(error.error.Error);
            }
            return XmlHelper.ExtractMessage(message, type);
        }

        public void Logger(String text, EventEntryType ev)
        {
            textLoger.Text += String.Format("{0}; EventEntryType {1}\r\n", text, ev);
        }

        public void SendAction(CardOperationType cardOperationType)
        {
            PipeClient client = new PipeClient();
            client.Logger = Logger;
            string message = client.SendPipeMessage(CardOperationType.reqStatus, reqStatus, PipeClient.CONNECT_TIMEOUT, true);
            AnsStatus status = (AnsStatus)this.ExtractMessage(message, typeof(AnsStatus));
            MrkStatuses mrkStatus = status.info.MrkStatus;
            if (!mrkStatus.MrkReady)
            {
                //this.SetErrorScreen("status.MrkReady == false", "МРК не готов к работе.", true);
                textLoger.Text += "МРК не готов к работе.";
            }
            else if (cardOperationType != CardOperationType.reqSaleCard)
            {
                if (mrkStatus.ReaderReady == 0)
                {
                    //base.CallBack("SetInfoWithMainMenu", this._readerErrorMessage);
                    Logger("Ошибка", EventEntryType.Error);
                }
                else
                {
                    client.SendPipeMessage(CardOperationType.reqControl_reader_1, "<?xml version='1.0' encoding='Windows-1251'?>\r\n<TCLib version='3.04'>\r\n\t<service>\r\n\t\t<reqControl>\r\n\t\t\t<reader>1</reader>\r\n\t\t</reqControl>\r\n\t</service>\r\n</TCLib>", PipeClient.CONNECT_TIMEOUT, true);
                    //base.CallBack("SetInfoWithMainMenu", this._waitCardMessage);
                    //this.StartPooling(cardOperationType);
                    Logger("Ждем", EventEntryType.Event);
                }
            }
            else
            {
                DisppenserStatus dispenserStatus = (DisppenserStatus)mrkStatus.DispenserStatus;
                string str2 = "Продажа карт запрещена: ";
                string format = "Стоимость карты при получении: {0} РУБ.{1}Наличие карт в терминале: {2}";
                decimal num = mrkStatus.CardPrice / 100M;
                switch (dispenserStatus)
                {
                    case DisppenserStatus.DispenserError:
                        Logger(str2 + "диспенсер не работает", EventEntryType.Error);
                        break;

                    case DisppenserStatus.NoCard:
                        Logger(string.Format(format, num, Environment.NewLine, "НЕТ"), EventEntryType.Warning);
                        break;

                    case DisppenserStatus.FewCard:
                    case DisppenserStatus.ManyCard:
                        if (!(mrkStatus.CardPrice == 0M))
                        {
                            Logger(string.Format(format, num, Environment.NewLine, "ЕСТЬ"),EventEntryType.Event);
                            break;
                        }
                        Logger(str2 + "цена карты не определена.", EventEntryType.Warning);
                        break;
                }
            }
        }

        private void PoolFunction(object state)
        {
            object obj2;
            string _terminalId = "";
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            this._cancel = true;
            Monitor.Enter(obj2 = _lockObject);
            try
            {
                string str = (string)state;
                Logger("Current Command: " + str, EventEntryType.Event);
                decimal result = 0M;
                string[] strArray = str.ToLower().Split(new char[] { ';' });
                string oldPin = string.Empty;
                string newPin = string.Empty;
                if (((strArray[0] == "sale") || (strArray[0] == "sale_card")) || (strArray[0] == "add"))
                {
                    decimal.TryParse(strArray[1], NumberStyles.Number, CultureInfo.InvariantCulture, out result);
                    _terminalId = strArray[2];
                }
                else if (strArray[0] == "pin")
                {
                    oldPin = strArray[1];
                    newPin = strArray[2];
                }
                else if ((strArray[0] == "start_pin") || (strArray[0] == "start_payment"))
                {
                    _terminalId = strArray[1];
                }
                switch (strArray[0])
                {
                    case "start_sale":
                        this.SendAction(CardOperationType.reqSaleCard);
                        return;

                    case "start_add":
                        this.SendAction(CardOperationType.reqWriteCard);
                        return;

                    case "balance":
                        this.SendAction(CardOperationType.Balance);
                        return;

                    case "info":
                        this.SendAction(CardOperationType.Info);
                        return;

                    case "payment":
                        //this.Payment();
                        return;

                    case "sale":
                        this.Sale(result, _terminalId, false);
                        return;

                    case "sale_card":
                        this.Sale(result, _terminalId, true);
                        return;

                    case "add":
                        //this.Add(result, this._terminalId);
                        return;

                    case "cancel":
                        //this.SendCancelCommand(true);
                        return;

                    case "start_pin":
                        //this.Status(CardOperationType.reqChangePin);
                        return;

                    case "pin":
                        //this.ChangePin(oldPin, newPin);
                        return;

                    case "start_payment":
                        this.SendAction(CardOperationType.reqPayment);
                        return;

                    case "print":
                        //this.Print();
                        return;

                    case "show_commission":
                        //this.ShowCommission();
                        return;
                }
                Logger("Данная команда не поддерживается терминалом: " + str, EventEntryType.CriticalError);
            }
            catch (TimeoutException)
            {
                Logger("TimeoutException. Нет связи с МРК.", EventEntryType.Error);
            }
            catch (SerializeException)
            {
                Logger("SerializeException. Ошибка разбора ответа от МРК", EventEntryType.Error);
            }
            catch (WtfException)
            {
                //this.SendCancelCommand(true);
            }
            catch (MrkErrorException exception)
            {
                string screenMessage = _errorsDescription.ContainsKey(exception.Error.Code) ? _errorsDescription[exception.Error.Code] : "Ошибка карт ридера";
                Logger(string.Concat(new object[] { "Ошибка МРК. Код ошибки: ", exception.Error.Code, " Описание ошибки: ", exception.Error.Description,screenMessage }), EventEntryType.Error);
            }
            catch (Exception exception2)
            {
                Logger("Unexpected error: " + exception2.ToString() + " Внутренняя ошибка терминала.", EventEntryType.Error);
            }
            finally
            {
                Monitor.Exit(obj2);
            }
        }

        private void Sale(decimal money, string terminalId, bool oneStep = false)
        {
            AnsSale sale;
            string str = decimal.ToInt32(money * 100M).ToString("d");
            PipeClient client = new PipeClient();
            client.Logger = Logger;
            string message = string.Empty;
            try
            {
                if (oneStep)
                {
                    message = client.SendPipeMessage("<?xml version='1.0' encoding='windows-1251'?>\r\n<TCLib version='3.04'>\r\n\t<info>\r\n\t\t<reqStatus/>\r\n\t</info>\r\n</TCLib>", 0x1388, true);
                    AnsStatus status = (AnsStatus)this.ExtractMessage(message, typeof(AnsStatus));
                }
                //base.CallBack("SetInfo", this._saleWaitingMessage);
                Logger("Ждем", EventEntryType.Event);
                message = client.SendPipeMessage(CardOperationType.reqSaleCard, string.Format("<?xml version='1.0' encoding='Windows-1251'?>\r\n<TCLib version='3.04'>\r\n\t<card>\r\n\t\t<reqSaleCard>\r\n\t\t\t<amount>{0}</amount>\r\n\t\t\t<terminalID>{1}</terminalID>\r\n\t\t\t<operation>{2}</operation>\r\n\t\t</reqSaleCard>\r\n\t</card>\r\n</TCLib>", str, terminalId, this.OperationID), 0x1e848, true);
                sale = (AnsSale)this.ExtractMessage(message, typeof(AnsSale));
            }
            catch (MrkErrorException exception)
            {
                string str3 = string.Format("Ошибка продажи карты: {0}. Деньги возвращены на сдачу.", exception.Error.Description);
                //base.AllMoneyToBalance();
                //this.WriteEntry(str3, EventEntryType.Error);
                Logger(str3, EventEntryType.Error);
                //base.CallBack("SetError", str3);
                //base.CallBack("SetPrintStep", str3);
                return;
            }
            catch (Exception exception2)
            {
                //base.AllMoneyToBalance();
                Logger("Неожиданная ошибка продажи: " + exception2.ToString(), EventEntryType.Error);
                //base.CallBack("SetError", exception2.Message);
                //base.CallBack("SetPrintStep", "Ошибка продажи карты. Деньги возвращены на сдачу");
                return;
            }
            cardInfo cardInfo = sale.card.AnsSaleCard.CardInfo;
            //this.SetAccountInfo(cardInfo);
            object[] args = new object[] { Environment.NewLine, cardInfo.Description, (cardInfo.Balance / 100M).ToString("F"), cardInfo.Pan, this.CreateDate(cardInfo.Date) };
            string str4 = string.Format("Не забудьте забрать карту!{0}{0}Информация о карте: {1}{0} Текущий баланс: {2} руб.{0}Номер карты: {3}{0}Срок действия: {4}{0}", args);
            Logger(str4, EventEntryType.Event);
            Thread.Sleep(200);
            //base.CallBack("SetPrintStep", "Не забудьте забрать карту!");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Init();
            //PoolFunction((object)"start_sale");
            //return;
            for (int i = 0; i < 5; i++)
            {
                PoolFunction((object)"start_sale");
                PoolFunction((object)"sale;75;204");
            }
        }

    }
}