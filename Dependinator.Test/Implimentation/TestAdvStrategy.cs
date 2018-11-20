using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dependinator.Test
{
    public class TestAdvStrategy : IAdvancmentStrategy<TestNode>
    {
        private Action<IEnumerable<TestState>> AdvanceCallback { get; set; }

        public void OnAdvance(Action<IEnumerable<TestState>> advanceCallback)
        {
            AdvanceCallback = advanceCallback;
        }

        public TestAdvStrategy(params TestState[] states)
        {
            StatesIn = states;
            States = new HashSet<IDependencyState<TestNode>>(states);
        }

        public TestState[] StatesIn { get; }
        public ISet<IDependencyState<TestNode>> States { get; }

        public Task Advance(IEnumerable<IDependencyState<TestNode>> states)
        {
            foreach(var testState in states.Cast<TestState>())
            {
                testState.Index++;
            }
            AdvanceCallback?.Invoke(states.Cast<TestState>());
            return Task.CompletedTask;
        }
    }
}
