using NumMath;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils.EParser;

namespace MCE.Problems
{
    public class EllipticalProblem
    {
        private int vertexesCount;
        private int elemCount;
        private int[][] elems; // elements list
        private double[][] vertexes; // vertexes list

        private double[,] localMatrix; // local matrix A

        // global matrix in sparce format
        private double[] di;
        private int[] ig;
        private int[] jg;
        private double[] gg;

        private Vector globalB; // global vector B
        public Vector coeffs { get; private set; } // vector q

        private double[] materials; // gamma for each element
        private double[] diffCoeffs; // lambda for each element
        private Func[] function; // vector f

        private BoundaryCondition[] conditions;

        public string Name { get; set; }

        public EllipticalProblem(int elemCount, int vertexesCount, int[][] elems, double[][] vertexes,
            double[] materials, double[] diffCoeffs,
            Func[] function, params BoundaryCondition[] conditions)
        {
            this.elemCount = elemCount;
            this.vertexesCount = vertexesCount;
            this.elems = elems;
            this.vertexes = vertexes;
            this.materials = materials;
            this.diffCoeffs = diffCoeffs;
            this.function = function;
            this.conditions = conditions;

            coeffs = new Vector(vertexesCount);
            globalB = new Vector(vertexesCount);
        }
        public static EllipticalProblem ReadProblemFrom(string directory)
        {
            ExpressionParser parser = new ExpressionParser();

            string elemsPath = "";
            string vertexesPath = "";
            List<string> boundaryConditionsPaths = new List<string>();
            string materialPath = "";
            string diffCoeffsPath = "";
            string functionPath = "";
            string[] files = Directory.GetFiles(directory);

            if (!Directory.Exists(directory) && files.Length != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Directory isn't exist or empty. Try again");
                Console.ResetColor();
                return null;
            }

            int i;
            string temp;
            for (i = 0; i < files.Length; i++)
            {
                temp = Path.GetFileName(files[i]);
                switch (temp)
                {
                    case "vertexes.txt": vertexesPath = files[i]; break;
                    case "elements.txt": elemsPath = files[i]; break;
                    case "gamma.txt": materialPath = files[i]; break;
                    case "lambda.txt": diffCoeffsPath = files[i]; break;
                    case "function.txt": functionPath = files[i]; break;
                    default:
                        if (temp.StartsWith("S") && temp.EndsWith(".txt"))
                            boundaryConditionsPaths.Add(files[i]);
                        break;
                }
            }

            // Reading vertexes
            string[] content = File.ReadAllLines(vertexesPath);
            int vertexesCount = content.Length;
            double[][] vertexes = new double[vertexesCount][];
            for (i = 0; i < vertexesCount; i++)
                vertexes[i] = content[i].Split(' ').Select(v => double.Parse(v)).ToArray();

            // Reading and compiling all function parts in right part of equation
            content = File.ReadAllLines(functionPath);
            Func[] function = new Func[content.Length];
            for (i = 0; i < function.Length; i++)
                function[i] = parser.GetFunction(content[i]);

            // Reading elements
            content = File.ReadAllLines(elemsPath);
            int elemCount = content.Length;
            int[][] elems = new int[elemCount][];
            for (i = 0; i < elemCount; i++)
                elems[i] = content[i].Split(' ').Select(e => int.Parse(e)).ToArray();

            // Reading materials and diff coeffs
            double[] materials = File.ReadAllText(materialPath).Split(' ').Select(m => double.Parse(m)).ToArray();
            double[] diffCoeffs = File.ReadAllText(diffCoeffsPath).Split(' ').Select(m => double.Parse(m)).ToArray();

            // Reading boundary conditions
            string[][] boundCond_str = new string[boundaryConditionsPaths.Count][];
            for (i = 0; i < boundaryConditionsPaths.Count; i++)
                boundCond_str[i] = File.ReadAllLines(boundaryConditionsPaths[i]);
            BoundaryCondition[] conditions = new BoundaryCondition[boundCond_str.Length];
            Func[] values = new Func[boundCond_str.Length];
            for (i = 0; i < conditions.Length; i++)
                conditions[i] = BoundaryCondition.Parse(parser, boundCond_str[i][0],
                    boundCond_str[i][1], boundCond_str[i][2]);

            return new EllipticalProblem(elemCount, vertexesCount, elems, vertexes,
                materials, diffCoeffs, function, conditions);
        }
        private void generatePortrait()
        {
            di = new double[vertexesCount];
            ig = new int[vertexesCount + 1];
            jg = new int[vertexesCount + elemCount - 1];
            gg = new double[jg.Length];

            int[] numbers;
            int i, j, k, s;
            List<int> temp;

            List<int>[] adjacencyList = new List<int>[vertexesCount];
            for (i = 0; i < elemCount; i++) // element loop
            {
                Array.Sort(elems[i], 0, 3);
                numbers = elems[i].Take(3).ToArray(); // global numbers of element's vertexes
                for (j = 1; j < numbers.Length; j++) // loop for global numbers
                {
                    for (k = 0; k < j; k++) // taking edge
                    {
                        if (adjacencyList[numbers[j]] == null) // if vertex's list is empty, create it
                        {
                            adjacencyList[numbers[j]] = new List<int>();
                            adjacencyList[numbers[j]].Add(numbers[k]);
                        }
                        else // if it's not, find position to insert
                        {    // due to numbers of related vertexes are sorted in ascending order
                            temp = adjacencyList[numbers[j]];
                            for (s = 0; s < temp.Count; s++)
                            {
                                if (temp[s] == numbers[k])
                                {
                                    s = -1;
                                    break;
                                }
                                if (numbers[k] < temp[s])
                                    break;
                            }

                            if (s != -1 || s == temp.Count)
                                temp.Insert(s, numbers[k]);
                        }
                    }
                }
            }

            for (i = 1; i < adjacencyList.Length; i++)
            {
                ig[i + 1] = ig[i];
                if (adjacencyList[i] != null)
                    for (j = 0; j < adjacencyList[i].Count; j++)
                    {
                        jg[ig[i] + j] = adjacencyList[i][j];
                        ig[i + 1]++;
                    }
            }
        }
        private void createGlobalMatrixAndGlobalVector()
        {
            generatePortrait();

            localMatrix = new double[3, 3]; // local matrix
            double[] localB = new double[3]; // local B
            double[,] D_1 = new double[3, 3];

            Func localFunction;
            double gamma;
            double lambda;

            double detD; //  detD: (x2 - x1)(y3 - y1) - (x3 - x1)(y2 - y1)
            int[] numbers; // global numbers of element's vertexes
            double[] v1; // vertex 1
            double[] v2; // vertex 2
            double[] v3; // vertex 3

            int i, j, k, s, m;
            double temp;
            double[] curVert;

            int condSize = conditions.Length;
            int[] curBound;

            // element loop
            for (k = 0; k < elemCount; k++)
            {
                numbers = elems[k];

                gamma = materials[numbers[3]];
                lambda = diffCoeffs[numbers[3]];
                localFunction = function[numbers[3]];

                v1 = vertexes[numbers[0]]; // (x1, y1)
                v2 = vertexes[numbers[1]]; // (x2, y2)
                v3 = vertexes[numbers[2]]; // (x3, y3)

                detD = (v2[0] - v1[0]) * (v3[1] - v1[1]) - (v3[0] - v1[0]) * (v2[1] - v1[1]);

                #region Вычисление матрицы D^(-1)
                D_1[0, 0] = (v2[0] * v3[1] - v3[0] * v2[1]) / detD; // x2 * y3 - x3 * y2
                D_1[1, 0] = (v1[1] * v3[0] - v1[0] * v3[1]) / detD; // -(x1 * y3 - y1 * x3)
                D_1[2, 0] = (v1[0] * v2[1] - v1[1] * v2[0]) / detD; // x1 * y2 - y1 * x2

                D_1[0, 1] = (v2[1] - v3[1]) / detD; // -(y3 - y2)
                D_1[1, 1] = (v3[1] - v1[1]) / detD; // y3 - y1
                D_1[2, 1] = (v1[1] - v2[1]) / detD; // -(y2 - y1)

                D_1[0, 2] = (v3[0] - v2[0]) / detD; // x3 - x2
                D_1[1, 2] = (v1[0] - v3[0]) / detD; // -(x3 - x1)                 
                D_1[2, 2] = (v2[0] - v1[0]) / detD; // x2 - x1 
                #endregion

                detD = Math.Abs(detD);
                for (i = 0; i < 3; i++)
                {
                    localMatrix[i, i] = gamma * detD / 12.0;
                    for (j = i + 1; j < 3; j++)
                    {
                        localMatrix[i, j] = gamma * detD / 24.0;
                        localMatrix[j, i] = localMatrix[i, j];
                    }
                }

                // adding G to local A
                for (i = 0; i < 3; i++)
                {
                    for (j = 0; j <= i; j++)
                    {
                        localMatrix[i, j] += lambda * detD / 2.0 *
                            (D_1[i, 1] * D_1[j, 1] + D_1[i, 2] * D_1[j, 2]);
                        localMatrix[j, i] = localMatrix[i, j];
                    }
                }

                // Gij = lambda * |detD| / 2.0 * (a(i,1) * a(j,1) + a(i,2) * a(j,2))
                // Aij = Gij + Mij
                //Console.WriteLine(k);

                // local B
                localB[0] = (2.0 * localFunction(v1) + localFunction(v2) + localFunction(v3)) / 24.0 * detD;
                localB[1] = (localFunction(v1) + 2.0 * localFunction(v2) + localFunction(v3)) / 24.0 * detD;
                localB[2] = (localFunction(v1) + localFunction(v2) + 2.0 * localFunction(v3)) / 24.0 * detD;

                #region Second and third type boundary conditions 
                for (m = 0; m < condSize; m++)
                {
                    switch (conditions[m].Type)
                    {
                        case BoundaryConditionType.Second:
                            for (i = 0; i < 2; i++)
                            {
                                for (j = i + 1; j < 3; j++)
                                    if (conditions[m].CheckEdge(numbers[i], numbers[j]))
                                    {
                                        v1 = vertexes[numbers[i]];
                                        v2 = vertexes[numbers[j]];
                                        localFunction = conditions[m].Value;

                                        temp = localFunction(v1);
                                        gamma = localFunction(v2);
                                        lambda = 2.0 * temp + gamma;
                                        detD = temp + 2.0 * gamma;
                                        temp = Math.Sqrt((v2[0] - v1[0]) * (v2[0] - v1[0]) + (v2[1] - v1[1]) * (v2[1] - v1[1])) / 6.0;

                                        localB[i] += temp * lambda;
                                        localB[j] += temp * detD;
                                    }
                            }
                            break;
                        case BoundaryConditionType.Third:
                            for (i = 0; i < 2; i++)
                            {
                                for (j = i + 1; j < 3; j++)
                                    if (conditions[m].CheckEdge(numbers[i], numbers[j]))
                                    {
                                        v1 = vertexes[numbers[i]];
                                        v2 = vertexes[numbers[j]];
                                        localFunction = conditions[m].Value;

                                        temp = localFunction(v1[0], v1[1]);
                                        gamma = localFunction(v2[0], v2[1]);
                                        lambda = 2.0 * temp + gamma;
                                        detD = temp + 2.0 * gamma;
                                        temp = Math.Sqrt((v2[0] - v1[0]) * (v2[0] - v1[0]) + (v2[1] - v1[1]) * (v2[1] - v1[1])) * conditions[m].Beta / 6.0;

                                        localMatrix[i, i] += temp * 2.0;
                                        localMatrix[i, j] += temp;
                                        localMatrix[j, i] += temp;
                                        localMatrix[j, j] += temp * 2.0;

                                        localB[i] += temp * lambda;
                                        localB[j] += temp * detD;
                                    }
                            }
                            break;
                    }
                }
                #endregion

                // adding diagonal to global matrix
                for (i = 0; i < 3; i++)
                    di[numbers[i]] += localMatrix[i, i];

                for (i = 1; i < 3; i++)
                {
                    m = ig[numbers[i]];
                    for (j = 0; j < i; j++)
                    {
                        for (s = m; s < ig[numbers[i] + 1]; s++)
                            if (jg[s] == numbers[j])
                            {
                                gg[s] += localMatrix[i, j];
                                m++;
                                break;
                            }
                    }
                }

                // adding local B to global B
                for (i = 0; i < 3; i++)
                    globalB[numbers[i]] += localB[i];
            }

            // First type boundary conditions
            foreach (BoundaryCondition condition in conditions.Where(condition => condition.Type == BoundaryConditionType.First))
            {
                curBound = condition.Vertexes;
                localFunction = condition.Value;
                for (k = 0; k < curBound.Length; k++)
                {
                    i = curBound[k];
                    curVert = vertexes[i];

                    di[i] = 1;
                    globalB[i] = localFunction(curVert);

                    for (s = ig[i]; s < ig[i + 1]; s++)
                    {
                        globalB[jg[s]] -= gg[s] * globalB[i];
                        gg[s] = 0;
                    }

                    for (s = i + 1; s < vertexesCount; s++)
                    {
                        for (j = ig[s]; j < ig[s + 1] && jg[j] <= i; j++)
                        {
                            if (jg[j] == i)
                            {
                                globalB[s] -= gg[j] * globalB[i];
                                gg[j] = 0;
                                break;
                            }
                        }
                    }
                }
            }
        }
        public void Solve()
        {
            createGlobalMatrixAndGlobalVector();
            SoLESolver.SolveCGM_LLT(new SymmSparseMatrix(vertexesCount, ig, jg, di, gg), globalB, coeffs);
        }
        public double GetValue(double x, double y)
        {
            int[] numbers = null;
            double[] s = new double[3];
            double[] v1, v2, v3;
            double detD = 0;
            int i;
            for (i = 0; i < elemCount; i++)
            {
                numbers = elems[i];
                v1 = vertexes[numbers[0]];
                v2 = vertexes[numbers[1]];
                v3 = vertexes[numbers[2]];

                // S12
                s[0] = Math.Abs((v2[0] - v1[0]) * (y - v1[1]) -
                    (x - v1[0]) * (v2[1] - v1[1]));

                // S31
                s[1] = Math.Abs((v1[0] - v3[0]) * (y - v3[1]) -
                    (x - v3[0]) * (v1[1] - v3[1]));

                // S23
                s[2] = Math.Abs((v2[0] - v3[0]) * (y - v3[1]) -
                    (x - v3[0]) * (v2[1] - v3[1]));

                detD = Math.Abs((v2[0] - v1[0]) * (v3[1] - v1[1]) -
                    (v3[0] - v1[0]) * (v2[1] - v1[1]));

                if (Math.Abs(detD - s[0] - s[1] - s[2]) < 1E-10)
                    break;
            }

            if (i == elemCount)
                return double.NaN;
            return (coeffs[numbers[0]] * s[2] + coeffs[numbers[1]] * s[1] +
                coeffs[numbers[2]] * s[0]) / detD;
        }
    }
}
