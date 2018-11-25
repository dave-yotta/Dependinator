using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dependinator
{
    public class Resolver<T> 
    {
        private DependencyManager<T> Dependencies { get; }
        private IAdvancmentStrategy<T> Strategy { get; }
        public Resolver(IAdvancmentStrategy<T> strategy)
        {
            Strategy = strategy;
            Dependencies = new DependencyManager<T>();
        }

        public async Task Resolve()
        {
            var toAdvance = new List<IDependencyState<T>>();

            while (Strategy.States.Any(x => !Dependencies.IsCompleted(x)))
            {
                var resolving = Strategy.States.Any(x => x.State == DependState.Resolving && !Dependencies.IsFrozen(x));

                foreach (var state in Strategy.States)
                {
                    // Don't advance completed states
                    if (Dependencies.IsCompleted(state)) continue;

                    // Frozen states what to take a target that has already been taken as a dependency (and not completed)
                    if(state.State != DependState.Resolved)
                    {
                        if(Dependencies.IsFrozen(state)) continue;
                    }

                    // If we are resolving...
                    if (resolving)
                    {
                        // only advance resolving states
                        if (state.State != DependState.Resolving) continue;

                        // Dont allow states to advance that depend on unresolved targets, unless
                        // this is going to cause a deadlock (then this can be the lone ranger to advance,
                        // freezing out the rest)
                        var unresolved = Dependencies.UnresolvedDependencies(state);
                        if (unresolved.Any() && unresolved.Select(Dependencies.UnresolvedDependencies).Any(x => x.Count == 0)) continue;

                        // We cannot allow unbound dependencies
                        if (state.NextUnboundDependency)
                        {
                            // You could allow them to filter one-by-one to resolution, when everything else is resolved. But slow.
                            throw new InvalidOperationException("Cannot make unbound dependencies during resolution");
                        }
                    }

                    // track targets and dependencies taken
                    Dependencies.Update(state);

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
                        Dependencies.ResetDependenciesOf(advanced);
                    }
                }

                toAdvance.Clear();
            }
        }
    }
}
