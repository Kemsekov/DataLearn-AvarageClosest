using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class VectorMask
{
    /// <param name="allowed">Determine whether vector element with given value and index is allowed for computations</param>
    public VectorMask(Func<float,int,bool> allowed)
    {
        this.Allowed = allowed;    
    }
    
    public Func<float, int, bool> Allowed { get; }
    public bool IsAllowedUnderIndex(int index) => Allowed(-1,index);
    public bool IsAllowed(float value, int index = -1) => Allowed(value,index);
}