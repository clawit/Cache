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
            testResult = weavingTask.ExecuteTestRun("AssemblyToReference.dll");
        }

        [Fact]
        public void ValidateHelloWorldIsInjected()
        {
            var type = testResult.Assembly.GetType("AssemblyToReference.NormalClass");
            var instance = (dynamic)Activator.CreateInstance(type);

            Assert.Equal("12", instance.AttributeA);
        }
    }
}
