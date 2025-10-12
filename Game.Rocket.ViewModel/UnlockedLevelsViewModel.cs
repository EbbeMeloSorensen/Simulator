using System;
using System.Collections.ObjectModel;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Game.Rocket.ViewModel
{
    public class UnlockedLevelsViewModel : ViewModelBase
    {
        private Level _selectedLevel;

        private RelayCommand _startLevelCommand;

        public Level SelectedLevel
        {
            get { return _selectedLevel; }
            set
            {
                _selectedLevel = value;
                RaisePropertyChanged();
                StartLevelCommand.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<Level> UnlockedLevels { get; }

        public RelayCommand StartLevelCommand =>
            _startLevelCommand ?? (_startLevelCommand =
                new RelayCommand(StartLevel, CanStartLevel));

        public event EventHandler<LevelSelectedEventArgs> LevelSelected;

        public UnlockedLevelsViewModel()
        {
            UnlockedLevels = new ObservableCollection<Level>();
        }

        public void AddLevel(Level level)
        {
            UnlockedLevels.Add(level);
            SelectedLevel = level;
        }

        private void StartLevel()
        {
            OnLevelSelected(SelectedLevel);
        }

        private bool CanStartLevel()
        {
            return SelectedLevel != null;
        }

        private void OnLevelSelected(
            Level level)
        {
            var handler = LevelSelected;

            if (handler != null)
            {
                handler(this, new LevelSelectedEventArgs(level));
            }
        }

    }
}
