using System.Collections.Generic;
using System.Linq;

namespace Dependinator
{
    public class DependencyManager<T>
    {
        public DependencyManager()
        {
            UnboundedDependancyTaken = new HashSet<IDependencyState<T>>();
            TargetsTaken = new Dictionary<T, ISet<IDependencyState<T>>>();
            DependanciesTaken = new Dictionary<T, ISet<IDependencyState<T>>>();
        }

        private IDictionary<T, ISet<IDependencyState<T>>> TargetsTaken { get; }
        private IDictionary<T, ISet<IDependencyState<T>>>  DependanciesTaken { get; }
        private ISet<IDependencyState<T>> UnboundedDependancyTaken { get; }

        public void Update(IDependencyState<T> state)
        {
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
        }

        public List<IDependencyState<T>> UnresolvedDependencies(IDependencyState<T> state)
        {
            return state.NextDependencies
                        .Where(TargetsTaken.ContainsKey)
                        .SelectMany(t => TargetsTaken[t].Where(s => !s.Equals(state)))
                        .Where(s => s.State == DependState.Resolving)
                        .ToList();
        }

        public bool IsFrozen(IDependencyState<T> state)
        {
            var targets = TargetsTaken.Where(x => x.Value.Contains(state))
                                      .Select(x => x.Key)
                                      .Concat(state.NextTargets)
                                      .ToList();

            var dependingStates = targets.SelectMany(x => DependanciesTaken.Where(d => d.Key.Equals(x)).SelectMany(d => d.Value).Concat(UnboundedDependancyTaken));
            return dependingStates.Any(x => !x.Equals(state) && !IsCompleted(x));
        }

        public bool IsCompleted(IDependencyState<T> state)
        {
            return state.State == DependState.Completed ||
                   state.State == DependState.Failed;
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
                ResetDependenciesOf(takenState);
            }
        }

        // Reset anything that has taken a dependency on this state
        public void ResetDependenciesOf(IDependencyState<T> state)
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
            foreach (var target in targets)
            {
                TargetsTaken[target].Remove(state);
            }
        }
    }
}
