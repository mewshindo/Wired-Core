using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired.Models
{
    public interface IElectricNode
    {
        bool IsPowered { get; }
        float Consumption { get; }
        bool AllowPowerThrough { get; }
        void SetPowered(bool powered);
    }
}
