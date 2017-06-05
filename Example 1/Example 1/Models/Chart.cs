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
            this.min = this.max = 0;
        }
        public string convertedCurrency;
        public double min { set; get; }
        public double max { set; get; }
        public List<ChartItem> chartData = new List<ChartItem>();
    }
}