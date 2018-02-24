using Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleSite.DataProvider
{
    public static class DataSample
    {
        [Cache]
        public static bool HasStock(int itemId)
        {
            //some verify item stock code here 
            Thread.Sleep(5000);

            return true;
        }

        [Cache(Duration = 3600)]
        public static decimal Calc(decimal a, decimal b)
        {
            //some your calc code here 
            Thread.Sleep(5000);

            return a + b;
        }

    }
}
