using System;

namespace Game.Rocket.ViewModel
{
    public class LevelSelectedEventArgs : EventArgs
    {
        public readonly Level Level;

        public LevelSelectedEventArgs(
            Level level)
        {
            Level = level;
        }
    }
}
