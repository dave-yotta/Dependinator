using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Dependinator.Test
{
    public class TestStateModelResetsConverter : JsonConverter<TestStateModelResets>
    {
        public override void WriteJson(JsonWriter writer, TestStateModelResets value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        public override TestStateModelResets ReadJson(JsonReader reader, Type objectType, TestStateModelResets existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var json = JArray.Load(reader);
            return new TestStateModelResets
            {
                Resets = json.ToObject<List<TestStateModel>>()
            };
        }
    }
}
