using System.Collections.Generic;
using System.Linq;

namespace Dependinator.Test
{
    public class TestState : IDependencyState<TestNode>
    {
        public int ResetCounter { get; set; }
        public int ResetNumber { get; set; }
        public int Index { get; set; }
        public TestStateEvolutionModel Model { get; }
        public TestState(TestStateEvolutionModel model)
        {
            Model = model;
        }

        public ISet<TestNode> Targets => new HashSet<TestNode>(Model.Evolution[Index].Resets[ResetNumber].Targets.Select(TestNode.Create));
        public ISet<TestNode> NextDependencies => new HashSet<TestNode>(Model.Evolution[Index].Resets[ResetNumber].NextDependencies.Select(TestNode.Create));
        public bool UnboundDependency => Model.Evolution[Index].Resets[ResetNumber].UnboundDependencies;
        public DependState State => Model.Evolution[Index].Resets[ResetNumber].State;

        public void Reset(ResetReason reason)
        {
            if(Model.ResetBehaviour[ResetNumber] != null)
            {
                ResetCounter++;
                if(ResetCounter > Model.ResetBehaviour[ResetNumber])
                {
                    ResetCounter = 0;
                    ResetNumber++;
                }
            }

            Index = 0;
        }
    }
}
