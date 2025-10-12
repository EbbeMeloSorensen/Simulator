using System;

namespace Game.TowerDefense.ViewModel
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
