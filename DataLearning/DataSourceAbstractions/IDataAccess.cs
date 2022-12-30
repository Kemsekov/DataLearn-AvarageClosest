using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public interface IDataAccess<T> : IEnumerable<T>
{
    int Length{get;}
    T this[int index]{get;set;}
    Span<T> AsSpan(Range range);
}