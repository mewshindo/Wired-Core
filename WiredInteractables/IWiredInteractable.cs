using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired.WiredInteractables
{
    public interface IWiredInteractable
    {
        Interactable interactable { get; }
        bool IsOn { get; }
        void SetPowered(bool state);
    }
}
