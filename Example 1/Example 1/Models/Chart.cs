using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class Chart
    {
        public Chart(string convertedCurrency)
        {
            this.convertedCurrency = convertedCurrency;
        }
        public string convertedCurrency;
        public List<ChartItem> chartData = new List<ChartItem>();
    }
}