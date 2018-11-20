using Newtonsoft.Json;
using System.Collections.Generic;

namespace Dependinator.Test
{
    [JsonConverter(typeof(TestStateAdvancementExpectedModelConverter))]
    public class TestStateAdvancementExpectedModel
    {
        public List<int> ToAdvance { get; set; }
    }
}
