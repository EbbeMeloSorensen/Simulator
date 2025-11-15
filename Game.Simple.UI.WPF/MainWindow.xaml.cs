using System.ComponentModel;
using System.Windows;
using Game.Simple.ViewModel;

namespace Game.Simple.UI.WPF
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

        private void MainWindow_OnClosing(
            object sender,
            CancelEventArgs e)
        {
            ViewModel.Engine.HandleClosing();
        }
    }
}