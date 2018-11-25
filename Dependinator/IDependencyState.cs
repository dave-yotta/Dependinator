using System.Collections.Generic;

namespace Dependinator
{
    public interface IDependencyState<T>
    {
        ISet<T> NextTargets { get; }
        ISet<T> NextDependencies { get; }
        bool NextUnboundDependency { get; }
        DependState State { get; }
        void Reset();
    }
}
