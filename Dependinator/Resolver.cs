using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dependinator
{
    public class Resolver<T> 
    {
        private IAdvancmentStrategy<T> Strategy { get; }
        private ISet<IDependencyState<T>> StartedResolving { get; }
        private IDictionary<T, ISet<IDependencyState<T>>>  DependanciesTaken { get; }
        private ISet<IDependencyState<T>> UnboundedDependancyTaken { get; }
        public Resolver(IAdvancmentStrategy<T> strategy)
        {
            Strategy = strategy;
            UnboundedDependancyTaken = new HashSet<IDependencyState<T>>();
            DependanciesTaken = new Dictionary<T, ISet<IDependencyState<T>>>();
            StartedResolving = new HashSet<IDependencyState<T>>();
        }

        public async Task Resolve()
        {
            var toAdvance = new List<IDependencyState<T>>();

            while (Strategy.States.Any(x => !IsCompleted(x)))
            {
                var resolving = Strategy.States.Any(x => x.State == DependState.Resolving && !IsFrozen(x));

                var targets = Strategy.States.SelectMany(s => s.Targets.Select(t => (t, s)))
                                             .ToLookup(x => x.t, x => x.s);

                foreach (var state in Strategy.States)
                {
                    // Don't advance completed states
                    if (IsCompleted(state)) continue;

                    // We can allow states to take a dependency on an unresolved state.
                    // However: In this case, the dependencies must freeze until the source is completed, not just resolved.
                    // This is so that it can work with a consistent view of the dependant node, which can then change when it's done.
                    if(state.State != DependState.Resolved)
                    {
                        if(IsFrozen(state)) continue;
                    }

                    // If we are resolving...
                    if (resolving)
                    {
                        // only advance resolving states
                        if (state.State != DependState.Resolving) continue;

                        // We cannot advance a state that wants to take a dependency on a resolving state
                        if (state.NextDependencies.Any(t => targets[t].Any(s => !s.Equals(state) && StartedResolving.Contains(s) && s.State == DependState.Resolving))) continue;

                        // We cannot allow unbound dependencies
                        if (state.UnboundDependency)
                        {
                            // You could allow them to filter one-by-one to resolution, when everything else is resolved. But slow.
                            throw new InvalidOperationException("Cannot make unbound dependencies during resolution");
                        }
                    }

                    // Manage dependencies taken
                    foreach (var dependancy in state.NextDependencies)
                    {
                        if (!DependanciesTaken.ContainsKey(dependancy))
                        {
                            DependanciesTaken[dependancy] = new HashSet<IDependencyState<T>>();
                        }
                        DependanciesTaken[dependancy].Add(state);
                    }
                    if (state.UnboundDependency)
                    {
                        UnboundedDependancyTaken.Add(state);
                    }

                    // This state is advancable
                    StartedResolving.Add(state);
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
                StartedResolving.Remove(takenState);
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

        bool IsFrozen(IDependencyState<T> state)
        {
            var dependingStates = state.Targets.SelectMany(x => DependanciesTaken.Where(d => d.Key.Equals(x)).SelectMany(d => d.Value).Concat(UnboundedDependancyTaken));
            return dependingStates.Any(x => !IsCompleted(x));
        }

        bool IsCompleted(IDependencyState<T> state)
        {
            return state.State == DependState.Completed ||
                   state.State == DependState.Failed;
        }

    }
}
