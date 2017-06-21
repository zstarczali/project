using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace WebApplication5.Helper
{
    public static class Helper
    {
        public static double ToJSDate(this DateTime TheDate)
        {
            DateTime d1 = new DateTime(1970, 1, 1);
            DateTime d2 = TheDate.ToUniversalTime();
            TimeSpan ts = new TimeSpan(d2.Ticks - d1.Ticks);

            return ts.TotalSeconds;
        }
    }
}