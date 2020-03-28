using System.Collections.Generic;
using Gunter.Data;
using Gunter.Data.Configuration;
using Reusable;

namespace Gunter.Workflow.Data
{
    internal class SessionContext
    {
        public string TheoryDirectoryName { get; set; }

        public TestFilter TestFilter { get; set; } = new TestFilter();

        public HashSet<string> TheoryNames { get; set; } = new HashSet<string>(SoftString.Comparer);

        public List<Theory> Theories { get; set; } = new List<Theory>();
    }
}