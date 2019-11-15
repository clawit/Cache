using Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AssemblyToReference
{
    public class NormalClass
    {
        //public static ICacheProvider Cache2 { get; set; }

        static NormalClass()
        {
            //Cache2 = CacheProvider.Provider;
        }

        //public string AttributeA {
        //    get {
        //        string a = "1";
        //        string b = "2";

        //        return a + b;
        //    }
        //}

        //public int AttributeB
        //{
        //    get
        //    {
        //        char a = '1';
        //        char b = '2';

        //        return a + b;
        //    }
        //}

        //[Cache]
        //public string AttributeC
        //{
        //    get
        //    {
        //        string a = "1";
        //        string b = "2";

        //        return a + b;
        //    }
        //}

        //[Cache]
        //public int AttributeD
        //{
        //    get
        //    {
        //        char a = '1';
        //        char b = '2';

        //        return a + b;
        //    }
        //}
        //public static decimal Calc(int a, int b)
        //{
        //    Thread.Sleep(10);
        //    return 3.14M;
        //}

        [Cache]
        public decimal Calc2(int a, int b)
        {
            Thread.Sleep(10000);
            return 3.14M;
        }

        //[Cache]
        //public decimal Calc3(int a, Dictionary<int, string> list)
        //{
        //    Thread.Sleep(10000);
        //    return 3.14M;
        //}

        //[Cache(Duration = 3600)]
        [Cache]
        public static decimal Calc4(int a, int b)
        {
            Thread.Sleep(5000);
            return 3.14M;
        }
    }
}
