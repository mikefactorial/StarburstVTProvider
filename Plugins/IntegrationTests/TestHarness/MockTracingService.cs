using Microsoft.Xrm.Sdk;

namespace Starburst.Plugins.TestHarness
{
    public class MockTracingService : ITracingService
    {
        public void Trace(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }
}
