using System.Collections.Generic;

namespace Dependinator.Test
{
    public class TestStateEvolutionModel
    {
        public int Id { get; set; }
        public bool ResetBehaviour { get; set; }
        public List<TestStateModel> Evolution { get; set; }
    }
}
