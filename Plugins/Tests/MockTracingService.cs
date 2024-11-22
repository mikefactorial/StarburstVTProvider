using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starburst.Plugins.Tests
{
    public class MockTracingService : ITracingService
    {
        public void Trace(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }
}
