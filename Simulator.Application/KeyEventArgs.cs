using System;

namespace Simulator.Application
{
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
