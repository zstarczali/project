using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml;
using WebApplication5.Models;

namespace WebApplication5.Controllers
{
    using Helper;
    public sealed class CurrencyConverter : Controller
    {

        private Dictionary<string, string> CurrencyNamesAndCodes = new Dictionary<string, string>();
        private List<string> CurrencyCodes = new List<string>();
        private SortedDictionary<double, Dictionary<string, double>> history = new SortedDictionary<double, Dictionary<string, double>>();
        private Dictionary<string, double> Currencies = new Dictionary<string, double>();
        private List<ChartItem> chart = new List<ChartItem>();

        private void LoadXML(string url, XmlDocument xmlDoc, XmlNamespaceManager nsMgr)
        {
            xmlDoc.Load(url);
            nsMgr.AddNamespace("x", "http://www.ecb.int/vocabulary/2002-08-01/eurofxref");
            nsMgr.AddNamespace("gemses", "http://www.gesmes.org/xml/2002-08-01");
        }

        public async Task LoadAndCollectData()
        {
            try
            {
                string url = "http://www.ecb.europa.eu/stats/eurofxref/eurofxref-hist-90d.xml";
                XmlDocument doc1 = new XmlDocument();
                XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc1.NameTable);

                await Task.Run(() =>
                {
                    LoadXML(url, doc1, nsMgr);
                });

                XmlElement root = doc1.DocumentElement;
                XmlNodeList nodes = root.SelectNodes("x:Cube/x:Cube", nsMgr);
                foreach (XmlNode node in nodes)
                {
                    string time = node.Attributes["time"].Value + " 09:00:00";
                    DateTime dt = DateTime.ParseExact(time, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                    long dateTime = System.Convert.ToInt64(dt.ToJSDate());
                    var n = node.SelectNodes("x:Cube", nsMgr);
                    if (CurrencyCodes.Count == 0)
                    {
                        // add missing part to currencies
                        CurrencyCodes.Add("EUR");
                        Currencies.Add("EUR", 1);

                        foreach (XmlNode nn in n)
                        {
                            string currency = nn.Attributes["currency"].Value;
                            double rate = double.Parse(nn.Attributes["rate"].Value, CultureInfo.InvariantCulture);

                            CurrencyCodes.Add(currency);
                            Currencies.Add(currency, rate);
                        }

                        history.Add(dateTime, Currencies);
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
                        history.Add(dateTime, currencies);

                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            this.CurrencyNamesAndCodes = GetCurrencyNameFromCode(CurrencyCodes);
        }

        public string GetCurrencyCode(string currencyCode)
        {
            try
            {
                return this.CurrencyNamesAndCodes[currencyCode];
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }


        public Dictionary<string, string> GetCurrencyNames()
        {
            return GetCurrencyNameFromCode(CurrencyCodes);
        }

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
                            // item not found, so we have to ignore it
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return ht;
        }

        public Chart CollectChartData(double value, string from, string to, string retValue)
        {
            string fromCurrency = this.GetCurrencyCode(from);
            string toCurrency = this.GetCurrencyCode(to);
            Chart chart = new Chart(retValue);
            double min = 0, max = 0;

            foreach (var item in this.history)
            {
                double time = item.Key;
                var currencies = item.Value;
                var cfrom = currencies[fromCurrency];
                var cto = currencies[toCurrency];
                var total = this.Convert(1, from, to, currencies);

                min = total < min ? total : min == 0 ? total : min;
                max = total > max ? total : max;
                chart.chartData.Add(new ChartItem { x = time, y = total });
            }
            chart.min = min;
            chart.max = max;

            return chart;
        }


        public double Convert(double value, string from, string to, Dictionary<string, double> currencies = null)
        {
            try
            {
                if (value == 0) return 0;

                string fromCurrency = this.GetCurrencyCode(from);
                string toCurrency = this.GetCurrencyCode(to);
                double oneEURValue = currencies != null ? System.Convert.ToDouble(currencies[fromCurrency]) : System.Convert.ToDouble(this.Currencies[fromCurrency]);
                double fromValInEUR = value / oneEURValue;
                double toCurrencyNum = currencies != null ? System.Convert.ToDouble(currencies[toCurrency]) : System.Convert.ToDouble(this.Currencies[toCurrency]);
                double toValInEUR = 1 / toCurrencyNum;
                return fromValInEUR / toValInEUR;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public ActionResult ConvertCurrency(string amount, string from, string to)
        {
            double currency;
            double total;

            try
            {
                double.TryParse(amount.Replace('.', ','), out currency);

                total = this.Convert(currency, from, to);
                Chart chart = this.CollectChartData(currency, from, to, total.ToString());
                var serializer = new JavaScriptSerializer();
                var serializedResult = serializer.Serialize(chart);

                return Content(serializedResult, "application/json");
            }
            catch (Exception ex)
            {
                return Content(ex.Message, "text/html");
            }
        }


    }
}