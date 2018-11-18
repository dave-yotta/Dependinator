using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dependinator
{
    public enum ResetReason
    {
        HasBecomeInconsistent,
        ConnectedFailure
    }

    public enum DependState
    {
        Resolving,
        Resolved,
        Failed,
        Completed,
        Terminated
    }

    public interface IAdvancmentStrategy<T>
    {
        ISet<IDependencyState<T>> States { get; }
        Task Advance(IEnumerable<IDependencyState<T>> states);
    }

    public interface IDependencyState<T>
    {
        ISet<T> Targets { get; }
        ISet<T> NextDependencies { get; }
        bool NextDependencyIsUnknown { get; }
        DependState State { get; }
        bool Reset(ResetReason reason);
    }

    // Test cases:
    //  - Depending on self
    //  - Begin or become co-dependant
    //  - Begin or become AB BA dependency
    //  - Take dependency on nothing, that would advance to Done quicker that one that eventually declares it as a target
    //  - Resetting one after resolve, that leads to a different state path, taking targets on Resolved/Done states (that now must be reset!)
    //  - Take a dependency which another state then declares it as it's target
    //  - Reset should allow the strategy to mutate it's states list e.g. "no, i'll not reset, i'll just have this as a failure"
    //  - Take an unbounded dependency after resolving - and have something fail
    //  - Take any unbounded dependency before resolving - we throw an exception becuase it's not supported (It can only be done synchronously)

    public class Resolver<T> 
    {
        private IAdvancmentStrategy<T> Strategy { get; }
        public Resolver(IAdvancmentStrategy<T> strategy)
        {
            Strategy = strategy;
        }

        public async Task Resolve()
        {
            var toAdvance = new List<IDependencyState<T>>();
            var dependenciesTaken = new Dictionary<IDependencyState<T>, ISet<T>>();
            var dependenciesTakenLookup = new Dictionary<T, ISet<IDependencyState<T>>>();
            var unboundedTaken = new List<IDependencyState<T>>();

            // Reset anything that has taken a dependency on this state
            void CheckReset(IDependencyState<T> state, ResetReason reason)
            {
                foreach (var dependancy in state.Targets.Where(dependenciesTakenLookup.ContainsKey))
                {
                    var taken = dependenciesTakenLookup[dependancy];
                    var toRemove = taken.Where(x => !x.Equals(state)).ToList();

                    foreach (var takenState in toRemove)
                    {
                        dependenciesTaken[takenState].Clear();
                        taken.Remove(takenState);
                        takenState.Reset(reason);
                    }
                    foreach (var takenState in toRemove)
                    {
                        CheckReset(takenState, reason);
                    }
                }
            }

            IEnumerable<IDependencyState<T>> Dependenc

            bool IsCompleted(IDependencyState<T> state)
            {
                return state.State == DependState.Completed ||
                       state.State == DependState.Failed ||
                       state.State == DependState.Terminated;
            }

            while (Strategy.States.Any(x => !IsCompleted(x)))
            {
                var resolving = Strategy.States.Any(x => x.State == DependState.Resolving);

                foreach (var state in Strategy.States)
                {
                    // Any state which has managed to take a dependency on a resolving state must be restarted
                    if(state.State == DependState.Resolving)
                    {
                        CheckReset(state, ResetReason.HasBecomeInconsistent);
                    }

                    // Don't advance completed states
                    if (IsCompleted(state)) continue;

                    // If we are resolving, only advance resolving states
                    if(resolving && state.State != DependState.Resolving) continue;

                    // We cannot advance a state that wants to take a dependency on a resolving state
                    if (state.NextDependencies.Any(dependenciesTakenLookup.ContainsKey)) continue;

                    // We cannot advance a state that wants to take unbounded dependency

                }

                if (toAdvance.Count == 0)
                {
                    throw new InvalidOperationException("States are in deadlock");
                }

                await Strategy.Advance(toAdvance);

                foreach(var advanced in toAdvance)
                {
                    // Any state that took a dependency on a failed state must be restarted
                    if(advanced.State == DependState.Failed)
                    {
                        CheckReset(advanced, ResetReason.ConnectedFailure);
                    }
                }

                toAdvance.Clear();
            }
        }
    }
}
