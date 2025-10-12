using System.Windows;
using Craft.Math;
using Craft.Utils;
using Simulator.Domain;

namespace Simulator.ViewModel
{
    public static class Helpers
    {
        public static PointD AsPointD(
            this Vector2D vector)
        {
            return new PointD(vector.X, vector.Y);
        }

        public static Point InitialWorldWindowFocus(
            this Scene scene)
        {
            return new Point(
                (scene.InitialWorldWindowUpperLeft.X + scene.InitialWorldWindowLowerRight.X) / 2,
                (scene.InitialWorldWindowUpperLeft.Y + scene.InitialWorldWindowLowerRight.Y) / 2);
        }

        public static Size InitialWorldWindowSize(
            this Scene scene)
        {
            return new Size(
                scene.InitialWorldWindowLowerRight.X - scene.InitialWorldWindowUpperLeft.X,
                scene.InitialWorldWindowLowerRight.Y - scene.InitialWorldWindowUpperLeft.Y);
        }
    }
}
