using Newtonsoft.Json;
using System.Collections.Generic;

namespace Dependinator.Test
{
    [JsonConverter(typeof(TestStateModelConverter))]
    public class TestStateModel
    {
        public List<int> Targets { get; set; }
        public List<int> NextDependencies { get; set; }
        public bool UnboundDependencies { get; set; }
        public DependState State { get; set; }
    }
}
