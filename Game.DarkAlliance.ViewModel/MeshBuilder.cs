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

        /// <summary>
        /// Creates a UV sphere with correct outward-facing normals.
        /// </summary>
        public static MeshGeometry3D CreateSphere(
            Point3D center,
            double radius,
            int slices,
            int stacks)
        {
            var mesh = new MeshGeometry3D();

            // Vertices
            for (var stack = 0; stack <= stacks; stack++)
            {
                var phi = Math.PI * stack / stacks;
                var y = Math.Cos(phi);
                var r = Math.Sin(phi);

                for (var slice = 0; slice <= slices; slice++)
                {
                    var theta = 2 * Math.PI * slice / slices;
                    var x = r * Math.Cos(theta);
                    var z = r * Math.Sin(theta);

                    mesh.Positions.Add(new Point3D(
                        center.X + radius * x,
                        center.Y + radius * y,
                        center.Z + radius * z));

                    mesh.Normals.Add(new Vector3D(x, y, z));
                }
            }

            // Triangles
            for (var stack = 0; stack < stacks; stack++)
            {
                for (var slice = 0; slice < slices; slice++)
                {
                    var first = (stack * (slices + 1)) + slice;
                    var second = first + slices + 1;

                    mesh.TriangleIndices.Add(first);
                    mesh.TriangleIndices.Add(second);
                    mesh.TriangleIndices.Add(first + 1);

                    mesh.TriangleIndices.Add(first + 1);
                    mesh.TriangleIndices.Add(second);
                    mesh.TriangleIndices.Add(second + 1);
                }
            }

            return mesh;
        }

        /// <summary>
        /// Creates a closed cylinder centered on the Y axis.
        /// </summary>
        public static MeshGeometry3D CreateCylinder(
            Point3D center,
            double radius,
            double height,
            int slices)
        {
            if (slices < 3)
                throw new ArgumentException("Cylinder requires at least 3 slices.");

            var mesh = new MeshGeometry3D();

            var halfH = height / 2.0;

            var baseIndex = 0;

            // --------------------
            // Side surface
            // --------------------
            for (var i = 0; i <= slices; i++)
            {
                var angle = 2.0 * Math.PI * i / slices;
                var x = Math.Cos(angle);
                var z = Math.Sin(angle);

                var normal = new Vector3D(x, 0, z);
                normal.Normalize();

                // Bottom ring
                mesh.Positions.Add(new Point3D(
                    center.X + radius * x,
                    center.Y - halfH,
                    center.Z + radius * z));

                mesh.Normals.Add(normal);

                // Top ring
                mesh.Positions.Add(new Point3D(
                    center.X + radius * x,
                    center.Y + halfH,
                    center.Z + radius * z));

                mesh.Normals.Add(normal);
            }

            var stride = 2;

            for (var i = 0; i < slices; i++)
            {
                var i0 = baseIndex + i * stride;
                var i1 = i0 + 1;
                var i2 = i0 + stride;
                var i3 = i2 + 1;

                // Two triangles per slice
                mesh.TriangleIndices.Add(i0);
                mesh.TriangleIndices.Add(i2);
                mesh.TriangleIndices.Add(i1);

                mesh.TriangleIndices.Add(i1);
                mesh.TriangleIndices.Add(i2);
                mesh.TriangleIndices.Add(i3);
            }

            var sideVertexCount = mesh.Positions.Count;

            // --------------------
            // Top cap
            // --------------------
            var topCenterIndex = mesh.Positions.Count;
            mesh.Positions.Add(new Point3D(
                center.X, center.Y + halfH, center.Z));
            mesh.Normals.Add(new Vector3D(0, 1, 0));

            for (var i = 0; i <= slices; i++)
            {
                var angle = 2.0 * Math.PI * i / slices;
                var x = Math.Cos(angle);
                var z = Math.Sin(angle);

                mesh.Positions.Add(new Point3D(
                    center.X + radius * x,
                    center.Y + halfH,
                    center.Z + radius * z));

                mesh.Normals.Add(new Vector3D(0, 1, 0));
            }

            for (var i = 0; i < slices; i++)
            {
                mesh.TriangleIndices.Add(topCenterIndex);
                mesh.TriangleIndices.Add(topCenterIndex + i + 1);
                mesh.TriangleIndices.Add(topCenterIndex + i + 2);
            }

            // --------------------
            // Bottom cap
            // --------------------
            var bottomCenterIndex = mesh.Positions.Count;
            mesh.Positions.Add(new Point3D(
                center.X, center.Y - halfH, center.Z));
            mesh.Normals.Add(new Vector3D(0, -1, 0));

            for (var i = 0; i <= slices; i++)
            {
                var angle = 2.0 * Math.PI * i / slices;
                var x = Math.Cos(angle);
                var z = Math.Sin(angle);

                mesh.Positions.Add(new Point3D(
                    center.X + radius * x,
                    center.Y - halfH,
                    center.Z + radius * z));

                mesh.Normals.Add(new Vector3D(0, -1, 0));
            }

            for (var i = 0; i < slices; i++)
            {
                // Winding reversed because normal points down
                mesh.TriangleIndices.Add(bottomCenterIndex);
                mesh.TriangleIndices.Add(bottomCenterIndex + i + 2);
                mesh.TriangleIndices.Add(bottomCenterIndex + i + 1);
            }

            return mesh;
        }
    }
}
