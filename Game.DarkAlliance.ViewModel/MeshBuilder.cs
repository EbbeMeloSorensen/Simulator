using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel
{
    public static class MeshBuilder
    {
        public static MeshGeometry3D CreateQuad(
            Point3D p0,
            Point3D p1,
            Point3D p2,
            Point3D p3)
        {
            var mesh = new MeshGeometry3D();

            mesh.Positions.Add(p0);
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);
            mesh.Positions.Add(p3);

            // Compute single normal
            var normal = Vector3D.CrossProduct(p1 - p0, p2 - p0);
            normal.Normalize();

            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);

            return mesh;
        }

        public static MeshGeometry3D CreateSphere(
            Point3D center,
            double radius,
            int thetaDiv,
            int phiDiv)
        {
            var mesh = new MeshGeometry3D();

            for (var pi = 0; pi <= phiDiv; pi++)
            {
                var phi = Math.PI * pi / phiDiv;
                var y = Math.Cos(phi);
                var r = Math.Sin(phi);

                for (var ti = 0; ti <= thetaDiv; ti++)
                {
                    var theta = 2.0 * Math.PI * ti / thetaDiv;

                    var x = r * Math.Cos(theta);
                    var z = r * Math.Sin(theta);

                    mesh.Positions.Add(new Point3D(
                        center.X + radius * x,
                        center.Y + radius * y,
                        center.Z + radius * z));

                    mesh.Normals.Add(new Vector3D(x, y, z));
                }
            }

            // Build triangles
            for (var pi = 0; pi < phiDiv; pi++)
            {
                for (var ti = 0; ti < thetaDiv; ti++)
                {
                    var a = (thetaDiv + 1) * pi + ti;
                    var b = a + thetaDiv + 1;

                    mesh.TriangleIndices.Add(a);
                    mesh.TriangleIndices.Add(b);
                    mesh.TriangleIndices.Add(a + 1);

                    mesh.TriangleIndices.Add(a + 1);
                    mesh.TriangleIndices.Add(b);
                    mesh.TriangleIndices.Add(b + 1);
                }
            }

            return mesh;
        }
    }
}
