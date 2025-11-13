using System.ComponentModel;
using System.Windows;
using Craft.Simulation.Engine;
using Game.Zelda.ViewModel;

namespace Game.Zelda.UI.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel { get { return DataContext as MainWindowViewModel; } }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnKeyDown(
            object sender,
            System.Windows.Input.KeyEventArgs e)
        {
            if (e.IsRepeat)
            {
                return;
            }

            switch (e.Key)
            {
                case System.Windows.Input.Key.Up:
                    ViewModel.Bonnet.HandleKeyEvent(KeyboardKey.UpArrow, KeyEventType.KeyPressed);
                    break;
                case System.Windows.Input.Key.Down:
                    ViewModel.Bonnet.HandleKeyEvent(KeyboardKey.DownArrow, KeyEventType.KeyPressed);
                    break;
                case System.Windows.Input.Key.Left:
                    ViewModel.Bonnet.HandleKeyEvent(KeyboardKey.LeftArrow, KeyEventType.KeyPressed);
                    break;
                case System.Windows.Input.Key.Right:
                    ViewModel.Bonnet.HandleKeyEvent(KeyboardKey.RightArrow, KeyEventType.KeyPressed);
                    break;
                case System.Windows.Input.Key.Space:
                    ViewModel.Bonnet.HandleKeyEvent(KeyboardKey.Space, KeyEventType.KeyPressed);
                    break;
            }
        }

        private void MainWindow_OnKeyUp(
            object sender,
            System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Up:
                    ViewModel.Bonnet.HandleKeyEvent(KeyboardKey.UpArrow, KeyEventType.KeyReleased);
                    break;
                case System.Windows.Input.Key.Down:
                    ViewModel.Bonnet.HandleKeyEvent(KeyboardKey.DownArrow, KeyEventType.KeyReleased);
                    break;
                case System.Windows.Input.Key.Left:
                    ViewModel.Bonnet.HandleKeyEvent(KeyboardKey.LeftArrow, KeyEventType.KeyReleased);
                    break;
                case System.Windows.Input.Key.Right:
                    ViewModel.Bonnet.HandleKeyEvent(KeyboardKey.RightArrow, KeyEventType.KeyReleased);
                    break;
                case System.Windows.Input.Key.Space:
                    ViewModel.Bonnet.HandleKeyEvent(KeyboardKey.Space, KeyEventType.KeyReleased);
                    break;
            }
        }

        private void MainWindow_OnClosing(
            object sender,
            CancelEventArgs e)
        {
            ViewModel.Bonnet.HandleClosing();
        }
    }
}
