using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using NumMath;
using NumMath.Splines;

namespace MCE.Problems.Magnetic
{
    public static class SplineReader
    {
        public static ISpline1D ReadSplineFromTelma(string filename)
        {
            Vector2[] points;
            using (StreamReader sw = new StreamReader(filename))
            {
                int pointCount = int.Parse(sw.ReadLine());
                points = new Vector2[pointCount];

                string[] separator = new string[] { " " };

                for(int i = 0; i < pointCount; i++)
                {
                    string[] values = sw.ReadLine().Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    points[i] = new Vector2(double.Parse(values[1]), double.Parse(values[0]));
                }
            }

            return new LinearInterpolationSpline(points);
        }
    }
}
