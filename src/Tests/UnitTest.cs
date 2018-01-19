using Cache.Fody;
using Fody;
using System;
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

        [Fact]
        public void ValidateHelloWorldIsInjected()
        {
            var type = testResult.Assembly.GetType("AssemblyToReference.NormalClass", true);
            var instance = (dynamic)Activator.CreateInstance(type);

            Assert.Equal(3.14M, instance.Calc2(1, 2));
        }
    }
}
