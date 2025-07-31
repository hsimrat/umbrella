using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.Interfaces
{
    public interface IUpdateSpecification<T>
    {
        Expression<Func<T, bool>> Criteria { get; }
        Action<T> UpdateAction { get; }
    }
}
