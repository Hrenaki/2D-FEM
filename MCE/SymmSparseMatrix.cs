using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCE
{
    class SymmSparseMatrix
    {
        public int size { get; private set; }
        public double[] di;
        public int[] ig;
        public int[] jg;
        public double[] gg;

        public SymmSparseMatrix(int size, int[] ig, int[] jg, double[] di, double[] gg)
        {
            this.size = size;
            this.ig = ig;
            this.jg = jg;
            this.di = di;
            this.gg = gg;
        }
        public static Vector operator*(SymmSparseMatrix matrix, Vector vector)
        {
            Vector res = new Vector(vector.size);
            int i, j;

            double[] vec = vector.values;
            double[] r = res.values;

            int[] ig = matrix.ig;
            int[] jg = matrix.jg;
            double[] di = matrix.di;
            double[] gg = matrix.gg;

            double sum;

            for(i = 0; i < matrix.size; i++)
            {
                sum = 0;
                for (j = ig[i]; j < ig[i + 1]; j++)
                {
                    sum += gg[j] * vec[jg[i]];
                    res[jg[j]] += gg[j] * vec[i];
                }
                r[i] += sum + di[i] * vec[i];
            }
            return res;
        }
        public override string ToString()
        {
            string str = "";
            int j, s, k;
            for(int i = 0; i < size; i++)
            {
                for (j = ig[i], s = 0; j < ig[i + 1] || s < i; j++, s++)
                    str += (jg[j] == s ? gg[j] : 0).ToString("E5") + " ";

                for(j = i; j < size; j++)
                {
                    for (k = ig[j]; k < ig[j + 1]; k++)
                        if (jg[k] == i)
                            break;
                    str += (k != ig[j + 1] ? gg[k] : 0).ToString("E5") + " ";
                }
            }
            return str;
        }
    }
}
