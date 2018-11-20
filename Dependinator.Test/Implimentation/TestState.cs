using System.Collections.Generic;
using System.Linq;

namespace Dependinator.Test
{
    public class TestState : IDependencyState<TestNode>
    {
        public int Index { get; set; }
        private bool ResetFail { get; set; }
        public TestStateEvolutionModel Model { get; }
        public TestState(TestStateEvolutionModel model)
        {
            Model = model;
        }

        public ISet<TestNode> Targets => new HashSet<TestNode>(Model.Evolution[Index].Targets.Select(TestNode.Create));
        public ISet<TestNode> NextDependencies => new HashSet<TestNode>(Model.Evolution[Index].NextDependencies.Select(TestNode.Create));
        public bool UnboundDependency => Model.Evolution[Index].UnboundDependencies;
        public DependState State => ResetFail ? DependState.Terminated : Model.Evolution[Index].State;

        public void Reset(ResetReason reason)
        {
            if(Model.ResetBehaviour)
            {
                Index = 0;
            }
            else
            {
                ResetFail = true;   
            }
        }
    }
}
