using Newtonsoft.Json;
using System.Collections.Generic;

namespace Dependinator.Test
{
    [JsonConverter(typeof(TestStateModelResetsConverter))]
    public class TestStateModelResets
    {
        public List<TestStateModel> Resets { get; set; }
    }
}
