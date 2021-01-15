using System;
using System.Collections.Generic;
using System.IO;

namespace MCE
{
    class Problem
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

        private Solver solver;

        public Problem(int elemCount, int vertexesCount, int[][] elems, double[][] vertexes,
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
            for(i = 0; i < elemCount; i++) // element loop
            {
                numbers = elems[i]; // global numbers of element's vertexes
                Array.Sort(numbers);
                for(j = 1; j < numbers.Length; j++) // loop for global numbers
                {
                    for(k = 0; k < j; k++) // taking edge
                    {
                        if(adjacencyList[numbers[j]] == null) // if vertex's list is empty, create it
                        {
                            adjacencyList[numbers[j]] = new List<int>();
                            adjacencyList[numbers[j]].Add(numbers[k]);
                        }
                        else // if it's not, find position to insert
                        {    // due to numbers of related vertexes are sorted in ascending order
                            temp = adjacencyList[numbers[j]];
                            for (s = 0; s < temp.Count; s++)
                            {
                                if(temp[s] == numbers[k])
                                {
                                    s = -1;
                                    break;
                                }
                                if (numbers[k] < temp[s])
                                    break;
                            }

                            if(s != -1 || s == temp.Count)
                                temp.Insert(s, numbers[k]);
                        }
                    }
                }
            }

            for(i = 1; i < adjacencyList.Length; i++)
            {
                ig[i + 1] = ig[i];
                if(adjacencyList[i] != null)
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

                gamma = materials[k];
                lambda = diffCoeffs[k];
                localFunction = function[k];

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
                    for (j = 0; j < i; j++)
                    {
                        localMatrix[i, j] = gamma * detD / 24.0;
                        localMatrix[j, i] = localMatrix[i, j];
                    }
                }

                // adding G to local A
                for (i = 0; i < 3; i++)
                    for (j = 0; j < 3; j++)
                        localMatrix[i, j] += lambda * detD / 2.0 *
                            (D_1[i, 1] * D_1[j, 1] + D_1[i, 2] * D_1[j, 2]);
                // Gij = lambda * |detD| / 2.0 * (a(i,1) * a(j,1) + a(i,2) * a(j,2))
                // Aij = Gij + Mij
                //Console.WriteLine(k);

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

                // local B
                localB[0] = (2.0 * localFunction(vertexes[numbers[0]][0], vertexes[numbers[0]][1]) +
                    localFunction(vertexes[numbers[1]][0], vertexes[numbers[1]][1]) +
                    localFunction(vertexes[numbers[2]][0], vertexes[numbers[2]][1])) / 24.0 * detD;
                localB[1] = (2.0 * localFunction(vertexes[numbers[1]][0], vertexes[numbers[1]][1]) +
                    localFunction(vertexes[numbers[0]][0], vertexes[numbers[0]][1]) +
                    localFunction(vertexes[numbers[2]][0], vertexes[numbers[2]][1])) / 24.0 * detD;
                localB[2] = (2.0 * localFunction(vertexes[numbers[2]][0], vertexes[numbers[2]][1]) +
                    localFunction(vertexes[numbers[1]][0], vertexes[numbers[1]][1]) +
                    localFunction(vertexes[numbers[0]][0], vertexes[numbers[0]][1])) / 24.0 * detD;

                #region Second type boundary conditions 
                for (i = 0; i < condSize; i++)
                {
                    if (conditions[i].Type == 2)
                    {                            
                        if(conditions[i].CheckEdge(numbers[0], numbers[1]))
                        {
                            localFunction = conditions[i].Value;
                            temp = localFunction(v1[0], v1[1]);
                            gamma = localFunction(v2[0], v2[1]);
                            lambda = 2.0 * temp + gamma;
                            detD = temp + 2.0 * gamma;
                            temp = Math.Sqrt((v2[0] - v1[0]) * (v2[0] - v1[0]) + (v2[1] - v1[1]) * (v2[1] - v1[1])) / 6.0;
                            localB[0] += lambda * temp;
                            localB[1] += detD * temp;
                        }

                        if(conditions[i].CheckEdge(numbers[0], numbers[2]))
                        {
                            localFunction = conditions[i].Value;
                            temp = localFunction(v1[0], v1[1]);
                            gamma = localFunction(v3[0], v3[1]);
                            lambda = 2.0 * temp + gamma;
                            detD = temp + 2.0 * gamma;
                            temp = Math.Sqrt((v3[0] - v1[0]) * (v3[0] - v1[0]) + (v3[1] - v1[1]) * (v3[1] - v1[1])) / 6.0;
                            localB[0] += lambda * temp;
                            localB[2] += detD * temp;
                        }

                        if(conditions[i].CheckEdge(numbers[1], numbers[2]))
                        {
                            localFunction = conditions[i].Value;
                            temp = localFunction(v2[0], v2[1]);
                            gamma = localFunction(v3[0], v3[1]);
                            lambda = 2.0 * temp + gamma;
                            detD = temp + 2.0 * gamma;
                            temp = Math.Sqrt((v3[0] - v2[0]) * (v3[0] - v2[0]) + (v3[1] - v2[1]) * (v3[1] - v2[1])) / 6.0;
                            localB[1] += lambda * temp;
                            localB[2] += detD * temp;
                        }
                    }
                }
                #endregion

                // adding local B to global B
                for (i = 0; i < 3; i++)
                    globalB[numbers[i]] += localB[i];
            }

            // First type boundary conditions
            foreach(BoundaryCondition condition in conditions)
            {
                if(condition.Type == 1)
                {
                    curBound = condition.Vertexes;
                    localFunction = condition.Value;
                    for(k = 0; k < curBound.Length; k++)
                    {
                        i = curBound[k];
                        curVert = vertexes[i];

                        di[i] = 1;
                        globalB[i] = localFunction(curVert[0], curVert[1]);

                        for(s = ig[i]; s < ig[i + 1]; s++)
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
        }
        public void Solve()
        {
            createGlobalMatrixAndGlobalVector();
            solver = new Solver(new SymmSparseMatrix(vertexesCount, ig, jg, di, gg));
            solver.solveCGM_LLT(coeffs, globalB);
            using (Window win = new Window(vertexes, elems, coeffs.values, 800, 800, "Mesh"))
            {
                win.Run(120.0);
            }
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
