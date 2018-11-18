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
        bool UnboundDependency { get; }
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
    //  - Taking a dependency on unresolved state by targets appearing only resets the dependants, not the new source that just appeared
    //  - What the hell happens if two states target the same node T?
    //  - Check it supports 1000 20ms tasks at low overhead, i guess.

    public class Resolver<T> 
    {
        private IAdvancmentStrategy<T> Strategy { get; }
        private IDictionary<T, ISet<IDependencyState<T>>>  DependanciesTaken { get; }
        private ISet<IDependencyState<T>> UnboundedDependancyTaken { get; }
        public Resolver(IAdvancmentStrategy<T> strategy)
        {
            Strategy = strategy;
            UnboundedDependancyTaken = new HashSet<IDependencyState<T>>();
            DependanciesTaken = new Dictionary<T, ISet<IDependencyState<T>>>();
        }

        public async Task Resolve()
        {
            var toAdvance = new List<IDependencyState<T>>();

            while (Strategy.States.Any(x => !IsCompleted(x)))
            {
                var resolving = Strategy.States.Any(x => x.State == DependState.Resolving);

                var targets = Strategy.States.SelectMany(s => s.Targets.Select(t => (t, s)))
                                             .ToLookup(x => x.t, x => x.s);

                foreach (var state in Strategy.States)
                {
                    // Any state which has managed to take a dependency on a resolving state must be restarted
                    if(state.State == DependState.Resolving)
                    {
                        CheckReset(state, ResetReason.HasBecomeInconsistent);
                    }

                    // Don't advance completed states
                    if (IsCompleted(state)) continue;

                    // If we are resolving...
                    if (resolving)
                    {
                        // only advance resolving states
                        if (state.State != DependState.Resolving) continue;

                        // We cannot advance a state that wants to take a dependency on a resolving state
                        if (state.NextDependencies.Any(t => targets[t].Any(s => !s.Equals(state) && s.State == DependState.Resolving))) continue;

                        // We cannot allow unbound dependencies
                        if (state.UnboundDependency)
                        {
                            // You could allow them to filter one-by-one to resolution, when everything else is resolved.
                            throw new InvalidOperationException("Cannout make unbound dependencies during resolution");
                        }
                    }

                    // It may or may not be good performance wise, buy you could try only advancing those that want to take
                    // unbound dependencies when all other states are totally completed, since any failure will reset
                    // a state that takes an unbound dependency.

                    // This state is advancable
                    foreach (var dependancy in state.NextDependencies)
                    {
                        if (!DependanciesTaken.ContainsKey(dependancy))
                        {
                            DependanciesTaken[dependancy] = new HashSet<IDependencyState<T>>();
                        }
                        DependanciesTaken[dependancy].Add(state);
                    }
                    if(state.UnboundDependency)
                    {
                        UnboundedDependancyTaken.Add(state);
                    }
                    toAdvance.Add(state);
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

        void ResetSet(IDependencyState<T> source, ISet<IDependencyState<T>> taken, ResetReason reason)
        {
            var toRemove = taken.Where(x => !x.Equals(source)).ToList();

            foreach (var takenState in toRemove)
            {
                taken.Remove(takenState);
                takenState.Reset(reason);
            }
            foreach (var takenState in toRemove)
            {
                CheckReset(takenState, reason);
            }
        }

        // Reset anything that has taken a dependency on this state
        void CheckReset(IDependencyState<T> state, ResetReason reason)
        {
            // Reset bounded dependencies taken
            foreach (var dependancy in state.Targets.Where(DependanciesTaken.ContainsKey))
            {
                ResetSet(state, DependanciesTaken[dependancy], reason);
            }

            // Reset unbounded dependencies taken
            ResetSet(state, UnboundedDependancyTaken, reason);
        }

        bool IsCompleted(IDependencyState<T> state)
        {
            return state.State == DependState.Completed ||
                   state.State == DependState.Failed ||
                   state.State == DependState.Terminated;
        }

    }
}
