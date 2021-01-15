using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCE
{
    class Solver
    {
        private readonly int maxSteps = 1000;
        private readonly double epsilon = 1E-12;
        public SymmSparseMatrix matrix;

        public Solver(SymmSparseMatrix mat)
        {
            matrix = mat;
        }

        public void solveCGM_LLT(Vector start, Vector vec)
        {
            int i, j, k, s, m, n;
            int size = matrix.size;
            int[] ig = matrix.ig;
            int[] jg = matrix.jg;
            double[] di = matrix.di;
            double[] gg = matrix.gg;

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

            double[] xk = start.values;
            double[] v = vec.values;
            double ak, bk;

            //rk = vec - mat * start
            double[] rk = new double[size];
            double[] zk = new double[size];
            mult(xk, rk);
            for (i = 0; i < size; i++)
            {
                rk[i] = v[i] - rk[i];
                zk[i] = rk[i];
                curNorm += rk[i] * rk[i];
                norm += v[i] * v[i];
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
                    xk[i] += ak * zk[i]; // x_k = x_k-1 + a_k * z_k-1
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
        public void solveLOS(Vector start, Vector vec)
        {
            int i, j, k, s, m;
            int size = matrix.size;
            int[] ig = matrix.ig;
            int[] jg = matrix.jg;
            double[] di = matrix.di;
            double[] gg = matrix.gg;

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

                    while (k < j)
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
            double[] xk = start.values;
            double[] v = vec.values;
            double[] temp = new double[size];
            double[] q = new double[size];
            double[] rk = new double[size];
            double[] zk = new double[size];
            double[] pk = new double[size];

            //rk = S^(-1) * (v - mat * x0)
            mult(xk, rk); // r_k = mat * x0            
            for (i = 0; i < size; i++)
                rk[i] = v[i] - rk[i]; // r_k = v - mat * x0
            straight(rk); // r_k = S^(-1) * (v - mat * x0)

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
                    xk[i] += ak * zk[i]; // x_k = x_k-1 + a_k * z_k-1
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
    }
}
