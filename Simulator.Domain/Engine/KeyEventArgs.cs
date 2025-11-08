using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator.Domain.Engine
{
    public enum KeyboardKey
    {
        LeftArrow,
        RightArrow,
        UpArrow,
        DownArrow,
        Space
    }

    public enum KeyEventType
    {
        KeyPressed,
        KeyReleased
    }

    public class KeyEventArgs : EventArgs
    {
        public readonly KeyboardKey KeyboardKey;
        public readonly KeyEventType KeyEventType;

        public KeyEventArgs(
            KeyboardKey keyboardKey,
            KeyEventType keyEventType)
        {
            KeyboardKey = keyboardKey;
            KeyEventType = keyEventType;
        }
    }
}
