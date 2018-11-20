using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Dependinator.Test
{
    public class TestStateAdvancementExpectedModelConverter : JsonConverter<TestStateAdvancementExpectedModel>
    {
        public override void WriteJson(JsonWriter writer, TestStateAdvancementExpectedModel value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override TestStateAdvancementExpectedModel ReadJson(JsonReader reader, Type objectType, TestStateAdvancementExpectedModel existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var json = JArray.Load(reader);
            return new TestStateAdvancementExpectedModel
            {
                ToAdvance = json.Values<int>().ToList()
            };
        }
    }
}
