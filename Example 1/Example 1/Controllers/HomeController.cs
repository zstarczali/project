using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml;
using WebApplication5.Models;

namespace WebApplication5.Controllers
{

    public class HomeController : Controller
    {

        private Dictionary<string, string> CurrencyNamesAndCodes = new Dictionary<string, string>();
        private List<string> CurrencyCodes = new List<string>();
        private Dictionary<int, Dictionary<string, double>> history = new Dictionary<int, Dictionary<string, double>>();
        private Dictionary<string, double> Currencies = new Dictionary<string, double>();
        private List<ChartItem> chart = new List<ChartItem>();

        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.Message = "";
            try
            {
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
                int day = 1;
                XmlNodeList nodes = root.SelectNodes("x:Cube/x:Cube", nsMgr);
                foreach (XmlNode node in nodes)
                {
                    string time = node.Attributes["time"].Value;

                    var n = node.SelectNodes("x:Cube", nsMgr);
                    if (CurrencyCodes.Count == 0)
                    {
                        // add missing part to currencies
                        CurrencyCodes.Add("EUR");
                        Currencies.Add("EUR", 1);

                        foreach (XmlNode nn in n)
                        {
                            string currency = nn.Attributes["currency"].Value;
                            double rate = double.Parse(nn.Attributes["rate"].Value, System.Globalization.CultureInfo.InvariantCulture);

                            CurrencyCodes.Add(currency);
                            Currencies.Add(currency, rate);
                        }
                        history.Add(day++, Currencies);
                    }
                    else
                    {

                        Dictionary<string, double> currencies = new Dictionary<string, double>();
                        currencies.Add("EUR", 1);

                        foreach (XmlNode nn in n)
                        {
                            string currency = nn.Attributes["currency"].Value;
                            double rate = double.Parse(nn.Attributes["rate"].Value, System.Globalization.CultureInfo.InvariantCulture);
                            currencies.Add(currency, rate);
                        }
                        history.Add(day++, currencies);

                    }
                }
            }
            catch (Exception)
            {
            }

            this.CurrencyNamesAndCodes = GetCurrencyNameFromCode(CurrencyCodes);


            saveState();

            ViewBag.history = history;
            return View();
        }

        public void saveState()
        {
            HttpContext.Session.Add("history", this.history);
            HttpContext.Session.Add("Currencies", this.Currencies);
            HttpContext.Session.Add("CurrencyNamesAndCodes", CurrencyNamesAndCodes);
        }

        public void loadState()
        {
            this.CurrencyNamesAndCodes = HttpContext.Session["CurrencyNamesAndCodes"] as Dictionary<string, string>;
            this.Currencies = HttpContext.Session["Currencies"] as Dictionary<string, double>;
            this.history = HttpContext.Session["history"] as Dictionary<int, Dictionary<string, double>>;
        }

        public string getCurrencyCode(string currencyCode)
        {
            try
            {
                this.loadState();
                return this.CurrencyNamesAndCodes[currencyCode];
            }
            catch (Exception)
            {
                return string.Empty;
            }
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
            try
            {
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
                                ht.Add(CcyNm, code);
                                break;
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }

            catch (Exception)
            {
            }
            return ht;
        }

        [HttpPost]
        public ActionResult ConvertCurrency(string amount, string from, string to)
        {
            double currency;
            double total;

            try
            {
                double.TryParse(amount.Replace('.', ','), out currency);

                total = this.convert(currency, from, to);
                Chart chart = this.collectChartData(currency, from, to, total.ToString());
                var serializer = new JavaScriptSerializer();
                var serializedResult = serializer.Serialize(chart);

                return Content(serializedResult, "application/json");
            }
            catch (Exception)
            {
                return Content("", "text/html");
            }
        }

        public Chart collectChartData(double value, string from, string to, string retValue)
        {
            string fromCurrency = this.getCurrencyCode(from);
            string toCurrency = this.getCurrencyCode(to);
            Chart chart = new Chart(retValue);

            foreach (var item in this.history)
            {
                int time = item.Key;
                var currencies = item.Value;
                var cfrom = currencies[fromCurrency];
                var cto = currencies[toCurrency];
                var total = this.convert(value, from, to, currencies);

                chart.chartData.Add(new ChartItem { x = time, y = total });
            }


            return chart;
        }

        public double convert(double value, string from, string to, Dictionary<string, double> currencies = null)
        {
            try
            {
                if (value == 0) return 0;

                string fromCurrency = this.getCurrencyCode(from);
                string toCurrency = this.getCurrencyCode(to);
                double oneEURValue = currencies != null ? Convert.ToDouble(currencies[fromCurrency]) : Convert.ToDouble(this.Currencies[fromCurrency]);
                double fromValInEUR = value / oneEURValue;
                double toCurrencyNum = currencies != null ? Convert.ToDouble(currencies[toCurrency]) : Convert.ToDouble(this.Currencies[toCurrency]);
                double toValInEUR = 1 / toCurrencyNum;
                return fromValInEUR / toValInEUR;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}