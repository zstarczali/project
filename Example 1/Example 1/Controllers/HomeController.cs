using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.Mvc;


namespace WebApplication5.Controllers
{
    using System.Threading.Tasks;

    public class HomeController : Controller
    {
        public CurrencyConverter converter = new CurrencyConverter();

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            ViewBag.Message = "";

            try
            {
                await converter.LoadAndCollectData();
                SaveState();
            }
            catch (Exception ex)
            {
                Content(ex.Message, "text/html");
            }

            return View();
        }

        private void SaveState()
        {
            HttpContext.Session.Add("CurrencyConverter", this.converter);
        }

        private void LoadState()
        {
            this.converter = HttpContext.Session["CurrencyConverter"] as CurrencyConverter;
        }

        [HttpPost]
        public ActionResult ConvertCurrency(string amount, string from, string to)
        {
            LoadState();
            return converter.ConvertCurrency(amount, from, to);
        }

        [HttpPost]
        public Dictionary<string, string> GetCurrencyNames()
        {
            return converter.GetCurrencyNames();
        }
    }
}