using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NumMath;
using EParser;
using Visualizer;

namespace MCE
{
    class Program
    {
        static void Main(string[] args)
        {

            //Console.Write("Enter directory with problem: ");
            ParabolicProblem problem = ParabolicProblem.ReadProblemFrom("parabolic/spec");
            if (problem != null)
            {
                problem.Solve();

                //Console.Write("Enter directory to output: ");
                //problem.OutputResult(Console.ReadLine());
                Console.WriteLine("Done!");
            }

            Func u = t => t[0] * t[0] + t[1];
            double[] time_points = new double[] { 7 };
            double[][] points = new double[][] { 
                new double[] { 0.75, 0.4 }, new double[] { 0.75, 1.6 },
                new double[] { 1.25, 0.4 }, new double[] { 1.25, 1.6 }
            };
            double[] point;
            double actual;
            double expected;
            Vector q_actual = new Vector(time_points.Length * points.Length);
            Vector q_expected = new Vector(q_actual.size);

            double x, y, h = 1E-4;
            int i, j, k;
            for (i = 0; i < points.Length; i++)
            {
                point = points[i];
                for (k = 0; k < time_points.Length; k++)
                {
                    actual = problem.GetValue(point[0], point[1], time_points[k]);
                    q_actual[i * time_points.Length + k] = actual;
                    expected = u(point[0], point[1], time_points[k]);
                    q_expected[i * time_points.Length + k] = expected;
                    Console.WriteLine(point[0] + " " + point[1] + " " + time_points[k] + " " + expected.ToString("E5") + " " + actual.ToString("E5") + " " + Math.Abs(expected - actual).ToString("E5"));
                }
            }

            Console.WriteLine(((q_actual - q_expected).magnitude / q_expected.magnitude).ToString("E5"));
            Console.ReadLine();
        }
    }
}
