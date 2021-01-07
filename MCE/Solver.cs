using System;
using System.Collections.Generic;
using System.IO;

namespace MCE
{
    class Solver
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

        private double[] globalB; // global vector B
        public double[] coeffs { get; private set; } // vector q

        private double[] materials; // gamma for each element
        private double[] diffCoeffs; // lambda for each element
        private Func[] function; // vector f

        private BoundaryCondition[] conditions;

        public Solver(int elemCount, int vertexesCount, int[][] elems, double[][] vertexes,
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

            globalB = new double[vertexesCount];
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
        public void solveCGM_LLT()
        {
            double epsilon = 1E-14;
            int maxSteps = 10000;

            int i, j, k, s, m, n;
            int size = vertexesCount;

            void mult(double[] b, double[] x) // x = Ab
            {
                for (i = 0; i < size; i++)
                    x[i] = 0;

                for (i = 0; i < size; i++)
                {
                    for (j = ig[i]; j < ig[i + 1]; j++)
                    {
                        x[jg[j]] += b[i] * gg[j];
                        x[i] += b[jg[j]] * gg[j];
                    }
                    x[i] += di[i] * b[i];
                }
            }

            double[] diLLT = new double[size];
            double[] ggLLT = new double[gg.Length];

            #region LLT factorization    
            // LLT факторизация
            for (i = 0; i < size; i++)
            {
                diLLT[i] = 0;

                for (j = ig[i]; j < ig[i + 1]; j++) // Lij
                {
                    ggLLT[j] = gg[j];

                    s = jg[j]; // column index of Lij
                    m = ig[i];
                    for (k = ig[s]; k < ig[s + 1]; k++)
                    {
                        for (n = m; n < j; n++)
                            if (jg[n] == jg[k])
                            {
                                ggLLT[j] -= ggLLT[n] * ggLLT[k];
                                m = n + 1;
                                break;
                            }
                    }
                    // L43 = 1 / L33 * (A43 - L41 * L31 - L42 * L32)

                    ggLLT[j] /= diLLT[jg[j]];
                    diLLT[i] -= ggLLT[j] * ggLLT[j]; // -Lij ^ 2
                }

                diLLT[i] = Math.Sqrt(di[i] + diLLT[i]);
            }
            #endregion

            double[] temp = new double[size];
            double norm = 0; //  ||b||
            double numerator, denominator = 0, curNorm = 0;
            double[] q = new double[size];

            double[] v = globalB;
            coeffs = new double[size];
            double ak, bk;

            //rk = vec - mat * start
            double[] rk = new double[size];
            double[] zk = new double[size];
            mult(coeffs, rk);
            for (i = 0; i < size; i++)
            {
                rk[i] = v[i] - rk[i];
                zk[i] = rk[i];
                curNorm += rk[i] * rk[i];
                norm += globalB[i] * globalB[i];
                q[i] = rk[i];
            }
            solveLLT(q);
            for (i = 0; i < size; i++)
                denominator += q[i] * rk[i];

            // LLT * x = y 
            void solveLLT(double[] y)
            {
                // straight
                for (i = 0; i < size; i++)
                {
                    for (j = ig[i]; j < ig[i + 1]; j++)
                        y[i] -= ggLLT[j] * y[jg[j]];
                    y[i] /= diLLT[i];
                }
                // backward
                for (i = size - 1; i >= 0; i--)
                {
                    y[i] /= diLLT[i];
                    for (j = ig[i]; j < ig[i + 1]; j++)
                        y[jg[j]] -= ggLLT[j] * y[i];
                }
            }

            solveLLT(zk);

            int step;
            for (step = 1; step < maxSteps && curNorm / norm >= epsilon * epsilon; step++)
            {
                mult(zk, temp);

                #region ak = (q, r_k-1) / (A * z_k-1, z_k-1)
                numerator = denominator;
                denominator = 0;
                for (i = 0; i < size; i++)
                    denominator += temp[i] * zk[i];
                ak = numerator / denominator;
                #endregion

                curNorm = 0;
                for (i = 0; i < size; i++)
                {
                    coeffs[i] += ak * zk[i]; // x_k = x_k-1 + a_k * z_k-1
                    rk[i] -= ak * temp[i]; // r_k = r_k-1 - a_k * A * z_k-1
                    curNorm += rk[i] * rk[i];
                }

                // q = M^(-1) * r_k => LLT * q = r_k
                for (i = 0; i < size; i++)
                    q[i] = rk[i];
                solveLLT(q);

                denominator = 0;
                for (i = 0; i < size; i++)
                    denominator += q[i] * rk[i]; // (M^(-1) * r_k, r_k) => (q, r_k)
                                                 // b_k = (M^(-1) * r_k, r_k) / (M^(-1) * r_k-1, r_k-1)
                bk = denominator / numerator;

                for (i = 0; i < size; i++)
                    zk[i] = q[i] + bk * zk[i]; // z_k = q + b_k * z_k-1
            }
        }
        private void solveLOS()
        {
             int maxSteps = 10000;
             double epsilon = 1E-12;

             int i, j, k, s, m;
             int size = vertexesCount;
             double[] diLU = new double[size];
             for (i = 0; i < size; i++)
                 diLU[i] = di[i];
             double[] gguLU = new double[gg.Length];
             double[] gglLU = new double[gg.Length];

             #region LU факторизация    
             for (i = 0; i < size; i++)
             {
                 for (j = ig[i]; j < ig[i + 1]; j++) // Lij и Uji
                 {
                     gguLU[j] = gg[j];
                     gglLU[j] = gg[j];

                     s = jg[j]; // номер столбца, в котором находится Lik
                     m = ig[s]; // индекс в ggu, с которого начинается k-ый столбец
                     k = ig[i]; 

                     while(k < j)
                     {
                         if (jg[k] == jg[m])
                         {
                             gglLU[j] -= gglLU[k] * gguLU[m]; // -Lik * Ukj
                             gguLU[j] -= gglLU[m] * gguLU[k];
                             m++;
                             k++;
                         }
                         else
                         {
                             if (jg[m] < jg[k])
                                 m++;
                             else k++;
                         }
                     }

                     gguLU[j] /= diLU[jg[j]];
                 }

                 for (k = ig[i]; k < ig[i + 1]; k++)
                     diLU[i] -= gglLU[k] * gguLU[k]; // -Lik * Uki
             }
             #endregion

             void mult(double[] b, double[] x) // x = Ab
             {
                 for (i = 0; i < size; i++)
                     x[i] = 0;

                 for (i = 0; i < size; i++)
                 {
                     for (j = ig[i]; j < ig[i + 1]; j++)
                     {
                         x[jg[j]] += b[i] * gg[j];
                         x[i] += b[jg[j]] * gg[j];
                     }
                     x[i] += di[i] * b[i];
                 }
             }
             void straight(double[] y)
             {
                 for (i = 0; i < size; i++)
                 {
                     for (j = ig[i]; j < ig[i + 1]; j++)
                         y[i] -= gglLU[j] * y[jg[j]];
                     y[i] /= diLU[i];
                 }
             }
             void backward(double[] y)
             {
                 for (i = size - 1; i >= 0; i--)
                 {
                     for (j = ig[i]; j < ig[i + 1]; j++)
                         y[jg[j]] -= gguLU[j] * y[i];
                 }
             }


             double normR0 = 0;
             double numerator, denominator, curNorm;

             double ak, bk;
             coeffs = new double[size];
             double[] temp = new double[size];
             double[] q = new double[size];
             double[] rk = new double[size];
             double[] zk = new double[size];
             double[] pk = new double[size];

             //rk = S^(-1) * (globalB - mat * x0)
             mult(coeffs, rk); // r_k = mat * x0            
             for (i = 0; i < size; i++)
                 rk[i] = globalB[i] - rk[i]; // r_k = globalB - mat * x0
             straight(rk); // r_k = S^(-1) * (globalB - mat * x0)

             for (i = 0; i < size; i++)
             {
                 zk[i] = rk[i]; // z_k = r_k
                 normR0 += rk[i] * rk[i]; // (r_k, r_k)
             }
             curNorm = normR0;

             backward(zk); // z_k = Q^(-1) * r_k

             mult(zk, pk); // pk = A * z_k
             straight(pk); // pk = S^(-1) * A * z_k

             int step;
             for (step = 0; step < maxSteps && curNorm / normR0 >= epsilon * epsilon; step++)
             {
                 #region ak = (p_k-1, r_k-1) / (p_k-1, p_k-1)
                 numerator = 0;
                 denominator = 0;
                 for (i = 0; i < size; i++)
                 {
                     numerator += pk[i] * rk[i];
                     denominator += pk[i] * pk[i];
                 }
                 ak = numerator / denominator;
                 #endregion

                 curNorm = 0;
                 for (i = 0; i < size; i++)
                 {
                     coeffs[i] += ak * zk[i]; // x_k = x_k-1 + a_k * z_k-1
                     rk[i] -= ak * pk[i]; // r_k = r_k-1 - a_k * p_k-1
                     curNorm += rk[i] * rk[i];
                 }

                 for (i = 0; i < size; i++)
                     temp[i] = rk[i]; // temp = r_k
                 backward(temp); // temp = Q^(-1) * r_k

                 mult(temp, q); // q = A * temp = A * Q^(-1) * r_k
                 straight(q); // q = S^(-1) * A * Q^(-1) * r_k

                 numerator = 0;
                 for (i = 0; i < size; i++)
                     numerator += pk[i] * q[i];
                 bk = -1.0 * numerator / denominator;

                 for (i = 0; i < size; i++)
                 {
                     zk[i] = temp[i] + bk * zk[i];
                     pk[i] = q[i] + bk * pk[i];
                 }
             }
        }
        public void Solve()
        {
            createGlobalMatrixAndGlobalVector();
            solveCGM_LLT();
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
