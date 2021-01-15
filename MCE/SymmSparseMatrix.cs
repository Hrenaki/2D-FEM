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
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
