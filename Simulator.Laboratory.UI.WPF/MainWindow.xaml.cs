using System.ComponentModel;
using System.Windows;
using Craft.Simulation.Engine;
using Simulator.Laboratory.ViewModel;

namespace Simulator.Laboratory.UI.WPF
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
                    ViewModel.Engine.HandleKeyEvent(KeyboardKey.UpArrow, KeyEventType.KeyPressed);
                    break;
                case System.Windows.Input.Key.Down:
                    ViewModel.Engine.HandleKeyEvent(KeyboardKey.DownArrow, KeyEventType.KeyPressed);
                    break;
                case System.Windows.Input.Key.Left:
                    ViewModel.Engine.HandleKeyEvent(KeyboardKey.LeftArrow, KeyEventType.KeyPressed);
                    break;
                case System.Windows.Input.Key.Right:
                    ViewModel.Engine.HandleKeyEvent(KeyboardKey.RightArrow, KeyEventType.KeyPressed);
                    break;
                case System.Windows.Input.Key.Space:
                    ViewModel.Engine.HandleKeyEvent(KeyboardKey.Space, KeyEventType.KeyPressed);
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
                    ViewModel.Engine.HandleKeyEvent(KeyboardKey.UpArrow, KeyEventType.KeyReleased);
                    break;
                case System.Windows.Input.Key.Down:
                    ViewModel.Engine.HandleKeyEvent(KeyboardKey.DownArrow, KeyEventType.KeyReleased);
                    break;
                case System.Windows.Input.Key.Left:
                    ViewModel.Engine.HandleKeyEvent(KeyboardKey.LeftArrow, KeyEventType.KeyReleased);
                    break;
                case System.Windows.Input.Key.Right:
                    ViewModel.Engine.HandleKeyEvent(KeyboardKey.RightArrow, KeyEventType.KeyReleased);
                    break;
                case System.Windows.Input.Key.Space:
                    ViewModel.Engine.HandleKeyEvent(KeyboardKey.Space, KeyEventType.KeyReleased);
                    break;
            }
        }

        private void MainWindow_OnClosing(
            object sender,
            CancelEventArgs e)
        {
            ViewModel.Engine.HandleClosing();
        }
    }
}
