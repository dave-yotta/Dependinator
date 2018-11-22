using System.Collections.Generic;

namespace Dependinator.Test
{
    public class TestStateEvolutionModel
    {
        public int Id { get; set; }
        public List<int?> ResetBehaviour { get; set; }
        public List<TestStateModelResets> Evolution { get; set; }
    }
}
