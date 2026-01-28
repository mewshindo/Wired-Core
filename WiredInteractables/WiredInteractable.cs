using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired.WiredInteractables
{
    public interface WiredInteractable
    {
        Interactable interactable { get; }
        bool isOn { get; }
        void Toggle(bool state);
    }
}
