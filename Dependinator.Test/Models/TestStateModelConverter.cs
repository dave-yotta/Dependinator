using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Dependinator.Test
{
    public class TestStateModelConverter : JsonConverter<TestStateModel>
    {
        public override void WriteJson(JsonWriter writer, TestStateModel value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override TestStateModel ReadJson(JsonReader reader, Type objectType, TestStateModel existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var json = JArray.Load(reader);
            return new TestStateModel
            {
                Targets = json[0].Values<int>().ToList(),
                NextDependencies = json[1].Values<int>().ToList(),
                UnboundDependencies = json[2].Value<bool>(),
                State = (DependState)Enum.Parse(typeof(DependState), json[3].Value<string>())
            };
        }
    }
}
