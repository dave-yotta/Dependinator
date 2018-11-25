using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dependinator
{
    public class Resolver<T> 
    {
        private IAdvancmentStrategy<T> Strategy { get; }
        private IDictionary<T, ISet<IDependencyState<T>>> TargetsTaken { get; }
        private IDictionary<T, ISet<IDependencyState<T>>>  DependanciesTaken { get; }
        private ISet<IDependencyState<T>> UnboundedDependancyTaken { get; }
        public Resolver(IAdvancmentStrategy<T> strategy)
        {
            Strategy = strategy;
            UnboundedDependancyTaken = new HashSet<IDependencyState<T>>();
            TargetsTaken = new Dictionary<T, ISet<IDependencyState<T>>>();
            DependanciesTaken = new Dictionary<T, ISet<IDependencyState<T>>>();
        }

        public async Task Resolve()
        {
            var toAdvance = new List<IDependencyState<T>>();

            while (Strategy.States.Any(x => !IsCompleted(x)))
            {
                var resolving = Strategy.States.Any(x => x.State == DependState.Resolving && !IsFrozen(x));

                foreach (var state in Strategy.States)
                {
                    // Don't advance completed states
                    if (IsCompleted(state)) continue;

                    // Frozen states what to take a target that has already been taken as a dependency (and not completed)
                    if(state.State != DependState.Resolved)
                    {
                        if(IsFrozen(state)) continue;
                    }

                    // If we are resolving...
                    if (resolving)
                    {
                        // only advance resolving states
                        if (state.State != DependState.Resolving) continue;

                        // Dont allow states to advance that depend on unresolved targets, unless
                        // this is going to cause a deadlock (then this can be the lone ranger to advance,
                        // freezing out the rest)
                        var unresolved = UnresolvedDependencies(state);
                        if (unresolved.Any() && unresolved.Select(UnresolvedDependencies).Any(x => x.Count == 0)) continue;

                        // We cannot allow unbound dependencies
                        if (state.NextUnboundDependency)
                        {
                            // You could allow them to filter one-by-one to resolution, when everything else is resolved. But slow.
                            throw new InvalidOperationException("Cannot make unbound dependencies during resolution");
                        }
                    }

                    // Manage dependencies and targets taken
                    foreach (var dependancy in state.NextDependencies)
                    {
                        if (!DependanciesTaken.ContainsKey(dependancy))
                        {
                            DependanciesTaken[dependancy] = new HashSet<IDependencyState<T>>();
                        }
                        DependanciesTaken[dependancy].Add(state);
                    }
                    if (state.NextUnboundDependency)
                    {
                        UnboundedDependancyTaken.Add(state);
                    }
                    foreach (var target in state.NextTargets)
                    {
                        if (!TargetsTaken.ContainsKey(target))
                        {
                            TargetsTaken[target] = new HashSet<IDependencyState<T>>();
                        }
                        TargetsTaken[target].Add(state);
                    }

                    // This state is advancable
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
                        CheckReset(advanced);
                    }
                }

                toAdvance.Clear();
            }
        }

        void ResetSet(IDependencyState<T> source, ISet<IDependencyState<T>> taken)
        {
            var toRemove = taken.Where(x => !x.Equals(source)).ToList();

            foreach (var takenState in toRemove)
            {
                taken.Remove(takenState);
                takenState.Reset();
            }
            foreach (var takenState in toRemove)
            {
                CheckReset(takenState);
            }
        }

        // Reset anything that has taken a dependency on this state
        void CheckReset(IDependencyState<T> state)
        {
            var targets = TargetsTaken.Where(x => x.Value.Contains(state))
                                      .Select(x => x.Key)
                                      .ToList();

            // Reset bounded dependencies taken
            foreach (var dependancy in targets.Where(DependanciesTaken.ContainsKey))
            {
                ResetSet(state, DependanciesTaken[dependancy]);
            }

            // Reset unbounded dependencies taken
            ResetSet(state, UnboundedDependancyTaken);

            // clear targets
            foreach(var target in targets)
            {
                TargetsTaken[target].Remove(state);
            }
        }

        List<IDependencyState<T>> UnresolvedDependencies(IDependencyState<T> state)
        {
            return state.NextDependencies
                        .Where(TargetsTaken.ContainsKey)
                        .SelectMany(t => TargetsTaken[t].Where(s => !s.Equals(state)))
                        .Where(s => s.State == DependState.Resolving)
                        .ToList();
        }

        bool IsFrozen(IDependencyState<T> state)
        {
            var targets = TargetsTaken.Where(x => x.Value.Contains(state))
                                      .Select(x => x.Key)
                                      .Concat(state.NextTargets)
                                      .ToList();

            var dependingStates = targets.SelectMany(x => DependanciesTaken.Where(d => d.Key.Equals(x)).SelectMany(d => d.Value).Concat(UnboundedDependancyTaken));
            return dependingStates.Any(x => !x.Equals(state) && !IsCompleted(x));
        }

        bool IsCompleted(IDependencyState<T> state)
        {
            return state.State == DependState.Completed ||
                   state.State == DependState.Failed;
        }

    }
}
