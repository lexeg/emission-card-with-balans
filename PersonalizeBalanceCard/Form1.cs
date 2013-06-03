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
        private string _terminalId = "";
        private decimal _maxValue = 0.0M;
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

        public void Logger2(String text)
        {
            textLoger.Text += String.Format("{0};\r\n", text);
        }

        public DisppenserStatus SendAction(CardOperationType cardOperationType)
        {
            DisppenserStatus result = DisppenserStatus.DispenserError;
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
                    this.StartPooling(cardOperationType);
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
                        result = DisppenserStatus.DispenserError;
                        break;

                    case DisppenserStatus.NoCard:
                        Logger(string.Format(format, num, Environment.NewLine, "НЕТ"), EventEntryType.Warning);
                        result = DisppenserStatus.NoCard;
                        break;

                    case DisppenserStatus.FewCard:
                    case DisppenserStatus.ManyCard:
                        if (!(mrkStatus.CardPrice == 0M))
                        {
                            Logger(string.Format(format, num, Environment.NewLine, "ЕСТЬ"),EventEntryType.Event);
                            result = (DisppenserStatus)mrkStatus.DispenserStatus;
                            break;
                        }
                        Logger(str2 + "цена карты не определена.", EventEntryType.Warning);
                        break;
                }
            }
            return result;
        }

        private void PoolFunction(object state, ref DisppenserStatus statusOfDispenser)
        {
            object obj2;
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
                        statusOfDispenser = this.SendAction(CardOperationType.reqSaleCard);
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
                        this.Add(result, this._terminalId);
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

        private void WorkFunction(object poolingType)
        {
            lock (_lockObject)
            {
                AnsStatus status;
                decimal maxBalance;
                string str;
                object[] objArray;
                decimal num3;
                int num = 0;
                this._cancel = false;
                do
                {
                    try
                    {
                        str = new PipeClient().SendPipeMessage(CardOperationType.reqStatus, "<?xml version='1.0' encoding='windows-1251'?>\r\n<TCLib version='3.04'>\r\n\t<info>\r\n\t\t<reqStatus/>\r\n\t</info>\r\n</TCLib>", 0x1388, false);
                        status = (AnsStatus)this.ExtractMessage(str, typeof(AnsStatus));
                        maxBalance = status.info.MrkStatus.MaxBalance;
                        //base.CallBack("LiveSignal", "");
                    }
                    catch (Exception exception1)
                    {
                        Exception exception = exception1;
                        Logger("ERROR! Pooling exception: " + exception.Message, EventEntryType.Error);
                        goto Label_05A4;
                    }
                    Thread.Sleep(0x3e8);
                    if (++num > 20)
                    {
                        Logger("20 секунд прошло, а карту так и не приложили для чтения. Высылаем МРК команду \"Cancel\"", EventEntryType.Event);
                        this.SendCancelCommand(true);
                        goto Label_05A4;
                    }
                }
                while (!status.info.MrkStatus.CardPresent);
                CardOperationType type = (CardOperationType)poolingType;
                string arg = (type == CardOperationType.Info) ? "Пожалуйста, подождите. Получаем выписку по карте..." : "this._readCardMessage";
                //base.CallBack("SetStartPooling", arg);
                //base.CallBack("LiveSignal", "");
                cardInfo cardInfo = null;
                string str3 = string.Empty;
                string str4 = string.Empty;
                try
                {
                    AnsWait wait;
                    PipeClient client = new PipeClient();
                    client.Logger = Logger;
                    if (type == CardOperationType.Info)
                    {
                        str = client.SendPipeMessage(CardOperationType.Info, string.Format("<?xml version='1.0' encoding='Windows-1251'?>\r\n<TCLib version='3.04'>\r\n\t<card>\r\n\t\t<reqWaitCard>\r\n\t\t\t<timeout>{0}</timeout>\r\n\t\t\t<application>1</application>\r\n\t\t\t<getInfo>2</getInfo>\r\n\t\t</reqWaitCard>\r\n\t</card>\r\n</TCLib>", "30"), 0x30d40, true);
                        wait = (AnsWait)this.ExtractMessage(str, typeof(AnsWait));
                        str4 = "this.CreateInfo(wait)";
                        //this.SetAccountInfo(wait.card.AnsWaitCard.CardInfo);
                    }
                    else
                    {
                        str = client.SendPipeMessage(CardOperationType.reqWaitCard, string.Format("<?xml version='1.0' encoding='Windows-1251'?>\r\n<TCLib version='3.04'>\r\n\t<card>\r\n\t\t<reqWaitCard>\r\n\t\t\t<timeout>{0}</timeout>\r\n\t\t\t<application>1</application>\r\n\t\t\t<getInfo>1</getInfo>\r\n\t\t</reqWaitCard>\r\n\t</card>\r\n</TCLib>", "30"), 0x9c40, true);
                        wait = (AnsWait)this.ExtractMessage(str, typeof(AnsWait));
                        cardInfo = wait.card.AnsWaitCard.CardInfo;
                        string[] strArray = cardInfo.Description.Split(new char[] { ',' });
                        string str5 = (strArray.Length > 1) ? (", " + strArray[1]) : string.Empty;
                        this._maxValue = (maxBalance - cardInfo.Balance) / 100M;
                        objArray = new object[4];
                        objArray[0] = Environment.NewLine;
                        num3 = cardInfo.Balance / 100M;
                        objArray[1] = num3.ToString("F").Replace(",", ".") + " руб." + str5;
                        objArray[2] = cardInfo.Pan;
                        objArray[3] = this.CreateDate(cardInfo.Date);
                        str3 = string.Format("Текущий баланс: {1}{0}Номер карты: {2}{0}Срок действия: {3}{0}", objArray) + ((this._maxValue > 0M) ? string.Format("Максимальная сумма пополнения: {0} руб.", this._maxValue) : "Карта пополнена на максимальную сумму");
                    }
                }
                catch (MrkErrorException exception2)
                {
                    string message = string.Format("Ошибка ожидания карты: {0}", exception2.Error.Description);
                    Logger(message, EventEntryType.Error);
                    if (exception2.Error.Code == 0x68)
                    {
                        this.SendCancelCommand(true);
                    }
                    else
                    {
                        //base.CallBack("SetCrashScreen", message);
                        Logger(string.Format("{0}: {1}", "SetCrashScreen", message), EventEntryType.Error);
                    }
                    goto Label_05A4;
                }
                catch (Exception exception4)
                {
                    //this.SetErrorScreen("Unexpected. Ошибка ожидания карты: " + exception4.ToString(), "Ошибка ожидания карты.", true);
                    Logger("Unexpected. Ошибка ожидания карты: " + exception4.ToString() + " Ошибка ожидания карты.", EventEntryType.Error);
                    goto Label_05A4;
                }
                switch (type)
                {
                    case CardOperationType.reqWriteCard:
                        Logger("Пополнение. Дождались карту: " + Environment.NewLine + str3, EventEntryType.Event);
                        //this.SetAccountInfo(cardInfo);
                        //base.CallBack("SetWaitCardResult", str3);
                        Logger(String.Format("{0}; {1}", "SetWaitCardResult", str3), EventEntryType.Event);
                        if (this._maxValue > 0M)
                        {
                            //base.CallBack("SetButtonNext", "");
                        }
                        break;

                    case CardOperationType.Balance:
                        Logger("Просмотр баланса. Дождались карту: " + str3, EventEntryType.Event);
                        //base.CallBack("SetBalanceResult", str3);
                        break;

                    case CardOperationType.reqChangePin:
                        Logger("Смена пинкода. Дождались карту: " + str3, EventEntryType.Event);
                        //base.CallBack("StartChangePin", string.Empty);
                        break;

                    case CardOperationType.reqPayment:
                        objArray = new object[4];
                        objArray[0] = Environment.NewLine;
                        num3 = cardInfo.Balance / 100M;
                        objArray[1] = num3.ToString("F").Replace(",", ".");
                        objArray[2] = cardInfo.Pan;
                        objArray[3] = this.CreateDate(cardInfo.Date);
                        str3 = string.Format("Доступная для оплаты сумма: {1} руб.{0}Номер карты: {2}{0}Срок действия: {3}{0}", objArray);
                        Logger("Оплата по карте. Дождались карту: " + str3, EventEntryType.Event);
                        //base.CallBack("SetPan", cardInfo.Pan);
                        //base.CallBack("SetWaitCardResult", str3);
                        //base.CallBack("SetButtonNext", "");
                        break;

                    case CardOperationType.Info:
                        //base.CallBack("SetDataGrid", str4);
                        break;
                }
            Label_05A4: ;
            }
        }

        private void Add(decimal money, string terminalId)
        {
            decimal num2 = money;
            AnsWrite write;
            decimal balance = 0M;
            string str = decimal.ToInt32(num2 * 100M).ToString("d");
            PipeClient client = new PipeClient();
            client.Logger = Logger;
            try
            {
                //base.CallBack("SetInfo", this._addWaitingMessage);
                string str2 = client.SendPipeMessage(CardOperationType.reqWriteCard, string.Format("<?xml version='1.0' encoding='Windows-1251'?>\r\n<TCLib version='3.04'>\r\n\t<card>\r\n\t\t<reqWriteCard>\r\n\t\t\t<amount>{0}</amount>\r\n\t\t\t<terminalID>{1}</terminalID>\r\n\t\t\t<operation>{2}</operation>\r\n\t\t</reqWriteCard>\r\n\t</card>\r\n</TCLib>", str, terminalId, this.OperationID), 0x2ee0, true);
                write = (AnsWrite)this.ExtractMessage(str2, typeof(AnsWrite));
                //base.SetBalance(balance);
                //base.AddTotal(-balance);
            }
            catch (MrkErrorException exception)
            {
                Logger("Ошибка пополнения карты: " + exception.Error.Description, EventEntryType.Error);
                //this.BreakScenario("Ошибка пополнения карты: " + exception.Error.Description);
                return;
            }
            catch (Exception exception2)
            {
                Logger("Неожиданная ошибка пополнения: " + exception2.ToString(), EventEntryType.Error);
                //this.BreakScenario("Неожиданная ошибка пополнения: " + exception2.ToString());
                return;
            }
            cardInfo cardInfo = write.card.CardInfo.CardInfo;
            object[] args = new object[] { Environment.NewLine, (cardInfo.Balance / 100M).ToString("F"), cardInfo.Pan, this.CreateDate(cardInfo.Date) };
            string message = string.Format("Карта успешно пополнена.{0} Текущий баланс: {1} руб.{0}Номер карты: {2}{0}Срок действия: {3}{0}", args);
            //this.SetAccountInfo(cardInfo);
            Logger(message, EventEntryType.Event);
            //this.SendCancelCommand(false);
            //base.CallBack("SetPrintStep", message);
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

        private void SendCancelCommand(bool exit = true)
        {
            PipeClient client = new PipeClient();
            client.Logger = Logger;
            try
            {
                string message = client.SendPipeMessage(CardOperationType.reqCancelWaitCard, "<?xml version='1.0' encoding='Windows-1251'?>\r\n<TCLib version='3.04'>\r\n\t<card>\r\n\t\t<reqCancelWaitCard>\r\n\t\t\t<reader>0</reader>\r\n\t\t</reqCancelWaitCard>\r\n\t</card>\r\n</TCLib>", 0x1388, true);
                AnsCancel cancel = (AnsCancel)this.ExtractMessage(message, typeof(AnsCancel));
                if (cancel.card.AnsCancelCard == null)
                {
                    Logger("Ошибка отмены карты", EventEntryType.Error);
                }
                else
                {
                    Logger("Команда Cancel успешно выполнена.", EventEntryType.Event);
                }
            }
            catch (WtfException exception)
            {
                Logger("Отмена. WTF: " + exception.Message, EventEntryType.Error);
            }
            catch (Exception exception2)
            {
                Logger("Ошибка отмены: " + exception2.ToString(), EventEntryType.Error);
            }
            finally
            {
                if (exit)
                {
                    //base.CallBack("Exit", string.Empty);
                }
            }
        }

        private void StartPooling(CardOperationType cardOperaionType)
        {
            //Thread _thread = new Thread(new ParameterizedThreadStart(this.WorkFunction));
            //_thread.Start(cardOperaionType);
            this.WorkFunction(cardOperaionType);
        }



        private void button1_Click(object sender, EventArgs e)
        {
            Init();
            //PoolFunction((object)"start_sale");
            //return;
            DisppenserStatus statusOfDispenser = DisppenserStatus.NoCard;
            do
            {
                PoolFunction((object)"start_sale", ref statusOfDispenser);
                textLoger.Text += String.Format("\r\nПромежуточный статус: {0}\r\n", statusOfDispenser);
                if (statusOfDispenser == DisppenserStatus.NoCard && statusOfDispenser == DisppenserStatus.DispenserError)
                {
                    break;
                }
                PoolFunction((object)"sale;75;204", ref statusOfDispenser);
            } while (statusOfDispenser != DisppenserStatus.NoCard && statusOfDispenser != DisppenserStatus.DispenserError);
            textLoger.Text += String.Format("\r\nПоследний полученный статус: {0}", statusOfDispenser);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _errorsDescription.Clear();
            textLoger.Clear();
            Init();
            
            CRT530Library.ClassCRT530 dispenser = new CRT530Library.ClassCRT530();
            dispenser.logging = Logger2;
            dispenser.ComPort = "COM1";
            dispenser.BaudRate = 5;

            if (dispenser.OpenPort("COM1", 5))
            {
                for (int i = 0; i < 80; i++)
                {
                    dispenser.PreDispenseCard(CRT530Library.TypeDispense.LeaveSensor2);
                    Thread.Sleep(2000);
                    DisppenserStatus statusOfDispenser = DisppenserStatus.NoCard;
                    PoolFunction((object)"start_add", ref statusOfDispenser);
                    //Thread.Sleep(5000);
                    PoolFunction((object)"add;25;204", ref statusOfDispenser);
                    Thread.Sleep(1000);
                    dispenser.PreDispenseCard(CRT530Library.TypeDispense.OutDoor);
                    Thread.Sleep(2000);
                }
            }
            dispenser.ClosePort();
            MessageBox.Show("Готово");
        }

    }
}