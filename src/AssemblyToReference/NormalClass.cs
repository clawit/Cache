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
        public string AttributeA {
            get {
                string a = "1";
                string b = "2";

                return a + b;
            }
        }

        public int AttributeB
        {
            get
            {
                char a = '1';
                char b = '2';

                return a + b;
            }
        }

        public static decimal Calc(int a, int b)
        {
            Thread.Sleep(10);
            return 3.14M;
        }

    }
}
