using System.Collections.Generic;

namespace Dependinator
{
    public interface IDependencyState<T>
    {
        ISet<T> Targets { get; }
        ISet<T> NextDependencies { get; }
        bool UnboundDependency { get; }
        DependState State { get; }
        void Reset(ResetReason reason);
    }
}
