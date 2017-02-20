using System.Drawing;
using System.IO;
using System.Linq;

namespace _3D_reconstruction
{
    public class OpenGloperation
    {
        public void SaveFase(string path, Point3d[] points, PointF[] textureMatrix, string texture)
        {
            var w = new StreamWriter(path);
            w.WriteLine(points.Count());
            foreach (var point in points)
            {
                w.WriteLine(point.ToString());
            }
            w.WriteLine(texture);
            w.WriteLine(textureMatrix.Count());
            foreach (var tx in textureMatrix)
            {
                w.WriteLine("{0} {1}", tx.X, tx.Y);
            }
            w.Close();
        }

        public void OpenFase(string path, ref Point3d[] points, ref PointF[] textureMatrix, ref string texture)
        {
            var r = new StreamReader(path);
            var count = int.Parse(r.ReadLine());
            points = new Point3d[count];
            for (var i = 0; i < count; i++)
            {
                var row = r.ReadLine();
                var pt = row.Split(' ');
                points[i] = new Point3d(
                float.Parse(pt[0]), float.Parse(pt[1]), float.Parse(pt[2]));
            }
            texture = r.ReadLine();
            count = int.Parse(r.ReadLine());
            textureMatrix = new PointF[count];
            for (var i = 0; i < count; i++)
            {
                var pt = r.ReadLine().Split(' ');
                textureMatrix[i] = new PointF(float.Parse(pt[0]), float.Parse(pt[1]));
            }
            r.Close();
        }
    }
}
