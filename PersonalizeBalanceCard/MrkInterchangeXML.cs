using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace PersonalizeBalanceCard
{
    public class lastError
    {
        [XmlElement("code")]
        public int Code = 0;
        [XmlElement("description")]
        public string Description;
    }

    public class MrkStatuses
    {
        [XmlElement("cardPresent")]
        public bool CardPresent;
        [XmlElement("cardPrice")]
        public decimal CardPrice;
        [XmlElement("dispenser")]
        public int DispenserStatus;
        [XmlElement("maxBalance")]
        public decimal MaxBalance;
        [XmlElement("status")]
        public bool MrkReady;
        [XmlElement("reader")]
        public int ReaderReady;
    }

    public class info
    {
        [XmlElement("ansStatus")]
        public MrkStatuses MrkStatus;
    }

    public class card
    {
        [XmlElement("ansSaleCard")]
        public ansInfo AnsSaleCard;
    }

    public class print
    {
        [XmlElement("line")]
        public List<string> line = new List<string>();
    }

    public class cardInfo
    {
        [XmlElement("application")]
        public long ApplicationCode;
        [XmlElement("refundable")]
        public bool AppType;
        [XmlElement("amount")]
        public decimal Balance;
        [XmlElement("enabledTo")]
        public string Date;
        [XmlElement("description")]
        public string Description = string.Empty;
        [XmlElement("PAN")]
        public decimal Pan;
        [XmlElement("refNumber")]
        public string RefNumber = "01";
        [XmlElement("track2")]
        public string Track2;
    }

    [XmlRoot("TCLib")]
    public class AnsStatus
    {
        [XmlElement("info")]
        public info info;
    }

    [XmlRoot("TCLib")]
    public class AnsSale
    {
        public card card;
    }

    [XmlRoot("TCLib")]
    public class AnsCancel
    {
        [XmlElement("card")]
        public CancelCard card;
    }

    [XmlRoot("TCLib")]
    public class AnsWait
    {
        [XmlElement("card")]
        public WaitCard card;
    }

    [XmlRoot("TCLib")]
    public class AnsError
    {
        [XmlElement("error")]
        public TCerror error;
    }

    public class CancelCard
    {
        [XmlElement("ansCancelWaitCard")]
        public ansCancelCard AnsCancelCard;
    }

    public class ansCancelCard
    {
    }

    public class WaitCard
    {
        [XmlElement("ansWaitCard")]
        public ansInfo AnsWaitCard;
    }

    public class ansInfo
    {
        [XmlElement("additionalInfo")]
        public string AddInfo = string.Empty;
        [XmlElement("cardInfo")]
        public cardInfo CardInfo;
        [XmlElement("print")]
        public print Print = new print();
    }

    [XmlRoot("TCLib")]
    public class AnsWrite
    {
        [XmlElement("card")]
        public ansWriteCard card;
    }

    public class ansWriteCard
    {
        [XmlElement("ansWriteCard")]
        public ansInfo CardInfo;
    }

    public class TCerror
    {
        [XmlElement("lastError")]
        public lastError Error;
    }

    public class XmlHelper
    {
        public static XmlDocument CreateDocument(object obj)
        {
            XmlDocument document = new XmlDocument();
            XmlSerializer serializer = new XmlSerializer(obj.GetType());
            MemoryStream w = new MemoryStream();
            try
            {
                XmlTextWriter xmlWriter = new XmlTextWriter(w, Encoding.Unicode);
                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                serializer.Serialize(xmlWriter, obj, namespaces);
                w.Position = 0L;
                document.Load(w);
            }
            catch (Exception exception)
            {
                throw new SerializeException("CreateDocument Error: " + exception.Message);
            }
            finally
            {
                if (w != null)
                {
                    w.Dispose();
                }
            }
            return document;
        }

        public static object ExtractMessage(string xml, Type t)
        {
            object obj2;
            try
            {
                obj2 = new XmlSerializer(t).Deserialize(new StringReader(xml));
            }
            catch (Exception exception)
            {
                throw new SerializeException("ExtractMessage Error: " + exception.Message);
            }
            return obj2;
        }
    }
}
