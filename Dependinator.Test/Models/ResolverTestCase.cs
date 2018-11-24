using System.Collections.Generic;

namespace Dependinator.Test
{
    public class ResolverTestCase
    {
        public string Name { get; set; }
        public List<TestStateEvolutionModel> Arrange { get; set; }
        public List<TestStateAdvancementExpectedModel> Assert { get; set; }
    }
}
