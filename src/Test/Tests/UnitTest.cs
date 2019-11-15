using AssemblyToReference;
using Cache;
using Cache.Fody;
using Fody;
using System;
using System.Collections.Generic;
using System.Reflection;
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
            new CacheProvider(new RuntimeCache());
        }

        [Fact]
        public void InstanceClassTest()
        {
            var type = testResult.Assembly.GetType("AssemblyToReference.NormalClass", true);
            var instance = (dynamic)Activator.CreateInstance(type);

            Assert.Equal(3.14M, instance.Calc2(1, 2));
        }

        [Fact]
        public void StaticMethodTest()
        {
            var type = testResult.Assembly.GetType("AssemblyToReference.NormalClass", true);
            var mf = type.GetMethod("Calc4", BindingFlags.Public | BindingFlags.Static);

            Assert.Equal(3.14M, mf.Invoke(null, new object[] { 1, 2}));

        }
    }
}
