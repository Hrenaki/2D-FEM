using MCE;
using MCE.Problems.Magnetic;
using NumMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI
{
    public static class MagneticProblemRunner
    {
        public static void Run()
        {
            string directory = ".";

            BoundaryCondition[] conditions;
            RectangleMesh mesh = RectangleMeshReader.ReadFromSreda(directory + "sreda_opt_n", out conditions);
            RectangleMeshWriter.WriteSplitted(mesh, directory + "vertices.txt", directory + "elements.txt", directory + "materials.txt");

            mesh.Materials[2].RelativeMagneticPermeabilitySpline = SplineReader.ReadSplineFromTelma(directory + "mu.002");

            MagneticProblem problem = new MagneticProblem(mesh, conditions);
            Vector q0 = new Vector(mesh.VertexCount);

            SoLESolver.MaxSteps = 50000;

            problem.SolveLinear(q0, 4 * Math.PI * Math.Pow(10, -7));

            Point[] points = new Point[]
            {
                new Point(-7.1e-3, 3.1e-3),
                new Point(-3.0e-3, 1.6e-3),
                new Point(3.0e-3, 1.6e-3),
                new Point(5.2e-3, 3.5e-3),
                new Point(6.7e-3, 4.4e-3),
                new Point(8.4e-3, 3.1e-3)
            };


            foreach (Point point in points)
            {
                Console.WriteLine(string.Format("{0}\t{1}\t{2}", point.ToString("E2"), problem.GetAzValue(point).ToString("E7"), problem.GetBValue(point).ToString("E4")));
            }
            Console.WriteLine();

            problem.SolveNonLinear(q0, 4 * Math.PI * Math.Pow(10, -7), 4000, 1E-12, 1E-8, 0.5);

            foreach (Point point in points)
            {
                Console.WriteLine(string.Format("{0}\t{1}\t{2}", point.ToString("E2"), problem.GetAzValue(point).ToString("E7"), problem.GetBValue(point).ToString("E4")));
            }
        }
    }
}
