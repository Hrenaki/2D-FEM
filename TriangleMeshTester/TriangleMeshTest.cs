using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MCE;
using MCE.Problems;
using Utils;
using Utils.EParser;

namespace TriangleMeshTester
{
    public static class TriangleMeshTest
    {
        public static Result LoadMeshAndRun(string filename, double diffCoef, double material)
        {
            TriangleMesh mesh;
            var result = TriangleMeshReader.TryReadMeshFrom(filename, out mesh);
            if (!result.IsSuccessful)
                return result;

            var xProblem = GenerateAndSolveLinearProblem(mesh, "u=x", 1, 0, 0, diffCoef, material);
            var yProblem = GenerateAndSolveLinearProblem(mesh, "u=y", 0, 1, 0, diffCoef, material);

            AnalyseProblemSolution(xProblem, t => t[0], mesh);
            AnalyseProblemSolution(yProblem, t => t[1], mesh);
            
            return Result.OK;
        }

        private static void AnalyseProblemSolution(EllipticalProblem problem, Func originSolution, TriangleMesh mesh)
        {
            Console.WriteLine();
            Console.WriteLine($"Analysing problem: {problem.Name}");
            Console.WriteLine("Origin\tActual\tRel.Error");

            var format = "{0}\t{1}\t{2}";

            foreach(var vertex in mesh.Vertices)
            {
                var originValue = originSolution(vertex);
                var actualValue = problem.GetValue(vertex[0], vertex[1]);
                var relError = Math.Abs(actualValue - originValue) / Math.Abs(originValue);

                Console.WriteLine(string.Format(format, originValue.ToString("E5"), actualValue.ToString("E5"), relError.ToString("E5")));
            }
        }

        private static EllipticalProblem GenerateAndSolveLinearProblem(TriangleMesh mesh, string problemName, double coeffX, double coeffY, double coeffShift, double diffCoef, double material)
        {
            var conditions = GenerateBoundaryConditions(mesh, coeffX, coeffY, coeffShift, diffCoef);
            Func func = t => material * (coeffX * t[0] + coeffY * t[1] + coeffShift);

            var problem = new EllipticalProblem(mesh.ElementCount, mesh.VertexCount,
                                                 mesh.ElementsArray, mesh.VerticesArray,
                                                 new double[] { material }, new double[] { diffCoef },
                                                 new Func[] { func },
                                                 conditions);
            problem.Name = problemName;

            problem.Solve();
            return problem;
        }

        private static BoundaryCondition[] GenerateBoundaryConditions(TriangleMesh mesh, double coeffX, double coeffY, double coeffShift, double diffCoef)
        {
            var border = mesh.Border;
            var vertices = mesh.Vertices;

            if(border.Count < 5)
            {
                return new BoundaryCondition[1]
                {
                    new BoundaryCondition(BoundaryConditionType.First, t => coeffX * t[0] + coeffY * t[1] + coeffShift, border.ToArray()),
                };
            }

            var secondBoundaryVertexIndices = border.Skip(border.Count - 3).ToArray();

            var normal1 = GetEdgeNormal(vertices.ElementAt(secondBoundaryVertexIndices[0]), vertices.ElementAt(secondBoundaryVertexIndices[1]));
            var normal2 = GetEdgeNormal(vertices.ElementAt(secondBoundaryVertexIndices[0]), vertices.ElementAt(secondBoundaryVertexIndices[1]));

            var secondBoundary1_value = diffCoef * (coeffX * normal1[0] + coeffY * normal1[1]);
            var secondBoundary2_value = diffCoef * (coeffX * normal2[0] + coeffY * normal2[1]);

            var boundaryConditions = new BoundaryCondition[3]
            {
                new BoundaryCondition(BoundaryConditionType.First, t => coeffX * t[0] + coeffY * t[1] + coeffShift, border.Take(border.Count - 3).ToArray()),
                new BoundaryCondition(BoundaryConditionType.Second, 
                                      t => secondBoundary1_value,
                                      new int[]{ secondBoundaryVertexIndices[0], secondBoundaryVertexIndices[1]}),
                new BoundaryCondition(BoundaryConditionType.Second, 
                                      t => secondBoundary2_value,
                                      new int[]{ secondBoundaryVertexIndices[1], secondBoundaryVertexIndices[2] })
            };

            return boundaryConditions;
        }

        private static double[] GetEdgeNormal(double[] start, double[] end)
        {
            double[] vec = new double[2] { -(end[1] - start[1]), end[0] - start[0]};

            double len = Math.Sqrt(vec[0] * vec[0] + vec[1] * vec[1]);
            vec[0] /= len;
            vec[1] /= len;

            return vec;
        }
    }
}
