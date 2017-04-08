using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace WebApplication5.Controllers
{
    public class HomeController : Controller
    {
        private List<string> CurrencyCodes = new List<string>();

        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.Message = "";
            ICollection<string> items = new List<string>();
            string url = "http://www.ecb.europa.eu/stats/eurofxref/eurofxref-hist-90d.xml";
            XmlDocument doc1 = new XmlDocument();
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc1.NameTable);


            Task t = new Task(() =>
            {
                doc1.Load(url);
                nsMgr.AddNamespace("x", "http://www.ecb.int/vocabulary/2002-08-01/eurofxref");
                nsMgr.AddNamespace("gemses", "http://www.gesmes.org/xml/2002-08-01");
            });

            t.Start();
            t.Wait();

            XmlElement root = doc1.DocumentElement;

            XmlNodeList nodes = root.SelectNodes("x:Cube/x:Cube", nsMgr);
            foreach (XmlNode node in nodes)
            {
                string time = node.Attributes["time"].Value;
                items.Add(time);

                var n = node.SelectNodes("x:Cube", nsMgr);
                if (CurrencyCodes.Count == 0)
                {
                    // add missing part to currencies
                    CurrencyCodes.Add("EUR");
                    foreach (XmlNode nn in n)
                    {
                        string currency = nn.Attributes["currency"].Value;
                        CurrencyCodes.Add(currency);
                    }
                }
            }
            ViewBag.data = items;
            return View();
        }

        [HttpGet]
        public IEnumerable<string> GetCurrencyCodes()
        {
            return CurrencyCodes;
        }

        [HttpGet]
        public Dictionary<string, string> GetCurrencyNameFromCode(IEnumerable codes)
        {
            // currency names from
            // https://gist.github.com/bzerangue/5484282
            // or
            // https://www.currency-iso.org/dam/downloads/lists/list_one.xml

            var ht = new Dictionary<string, string>();
            string url = "https://www.currency-iso.org/dam/downloads/lists/list_one.xml";
            XmlDocument doc1 = new XmlDocument();
            doc1.Load(url);
            XmlElement root = doc1.DocumentElement;

            XmlNodeList nodes = root.SelectNodes("CcyTbl/CcyNtry");
            foreach (string code in codes)
            {
                foreach (XmlNode node in nodes)
                {
                    try
                    {
                        var Ccy = node.SelectSingleNode("Ccy").InnerText;
                        if (string.Compare(code, Ccy, true) == 0)
                        {
                            var CcyNm = node.SelectSingleNode("CcyNm").InnerText;
                            ht.Add(code, CcyNm);
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        //...
                    }
                }
            }
            return ht;
        }

    }
}