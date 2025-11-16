using Craft.Simulation.Engine;
using Game.DarkAlliance.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.UI.WPF
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
            SetupScene();
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

        private void MainWindow_OnLoaded(
            object sender,
            RoutedEventArgs e)
        {
            ViewModel.HandleLoaded();
        }

        private void MainWindow_OnClosing(
            object sender,
            CancelEventArgs e)
        {
            ViewModel.Engine.HandleClosing();
        }

        private void SetupScene()
        {
            // --- Lighting ---
            var light = new DirectionalLight(Colors.White, new Vector3D(-0.5, -1, -0.5));
            var lightModel = new ModelVisual3D { Content = light };
            viewport.Children.Add(lightModel);

            viewport.Children.Add(CreateRectangle(
                new Point3D(-2, 0, -2),
                new Point3D(2, 0, -2),
                new Point3D(2, 0, 2),
                new Point3D(-2, 0, 2),
                Colors.DarkSlateGray
            ));

            viewport.Children.Add(CreateRectangle(
                new Point3D(-4, 0, -3),
                new Point3D(0, 0, -3),
                new Point3D(0, 0, -2),
                new Point3D(-4, 0, -2),
                Colors.DarkSlateGray
            ));

            viewport.Children.Add(CreateRectangle(
                new Point3D(-4, 0, -7),
                new Point3D(-4, 0, -2),
                new Point3D(-5, 0, -2),
                new Point3D(-5, 0, -7),
                Colors.DarkSlateGray
            ));

            viewport.Children.Add(CreateRectangle(
                new Point3D(-2, 0, -14),
                new Point3D(-2, 0, -6),
                new Point3D(-4, 0, -6),
                new Point3D(-4, 0, -14),
                Colors.DarkSlateGray
            ));
        }

        private ModelVisual3D CreateRectangle(Point3D p1, Point3D p2, Point3D p3, Point3D p4, Color color)
        {
            var mesh = new MeshGeometry3D
            {
                Positions = new Point3DCollection { p1, p2, p3, p4 },
                TriangleIndices = new Int32Collection { 0, 2, 1, 0, 3, 2 },
            };

            var material = new DiffuseMaterial(new SolidColorBrush(color));

            return new ModelVisual3D
            {
                Content = new GeometryModel3D(mesh, material)
            };
        }
    }
}