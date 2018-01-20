using Cache.Fody;
using Fody;
using System;
using System.Collections.Generic;
using Xunit;

namespace Tests
{
    public class UnitTest
    {
        static TestResult testResult;

        static UnitTest()
        {
            var weavingTask = new ModuleWeaver();
            testResult = weavingTask.ExecuteTestRun("AssemblyToReference.dll", false);
        }

        //[Fact]
        //public void InstanceClassTest()
        //{
        //    var type = testResult.Assembly.GetType("AssemblyToReference.NormalClass", true);
        //    var instance = (dynamic)Activator.CreateInstance(type);

        //    Assert.Equal(3.14M, instance.Calc2(1, 2));
        //}

        [Fact]
        public void InstanceClassTestWithGeneric()
        {
            var type = testResult.Assembly.GetType("AssemblyToReference.NormalClass", true);
            var instance = (dynamic)Activator.CreateInstance(type);

            var p2 = new Dictionary<int, string>();
            p2.Add(1, "111");
            p2.Add(2, "222");
            Assert.Equal(3.14M, instance.Calc3(1,  p2));
            p2.Add(3, "333");
            p2.Add(4, "444");
            Assert.Equal(3.14M, instance.Calc3(1, p2));

        }

    }
}
