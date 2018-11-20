using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dependinator
{
    public interface IAdvancmentStrategy<T>
    {
        ISet<IDependencyState<T>> States { get; }
        Task Advance(IEnumerable<IDependencyState<T>> states);
    }
}
