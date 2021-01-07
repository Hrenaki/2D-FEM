using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCE
{
    public class CompressedMatrix
    {
        public double[] di, ggl, ggu;
        public int[] ig, jg;
        public int size { get; private set; }
        public CompressedMatrix(int size, int[] ig, int[] jg, double[] di, double[] ggu, double[] ggl)
        {
            this.size = size;
            this.ig = ig;
            this.jg = jg;
            this.di = di;
            this.ggu = ggu;
            this.ggl = ggl;
        }
        public static CompressedMatrix Parse(string size, string ig, string jg,
            string di, string ggu, string ggl)
        {
            return new CompressedMatrix(int.Parse(size),
                ig.Split(' ').Select(value => int.Parse(value)).ToArray(),
                jg.Split(' ').Select(value => int.Parse(value)).ToArray(),
                di.Split(' ').Select(value => double.Parse(value)).ToArray(),
                ggu.Split(' ').Select(value => double.Parse(value)).ToArray(),
                ggl.Split(' ').Select(value => double.Parse(value)).ToArray());
        }
    }
    public class CompressedSymmMatrix
    {
        public double[] di, gg;
        public int[] ig, jg;
        public int size { get; private set; }
        public CompressedSymmMatrix(int size, int[] ig, int[] jg, double[] di, double[] gg)
        {
            this.size = size;
            this.ig = ig;
            this.jg = jg;
            this.di = di;
            this.gg = gg;
        }
        public static CompressedSymmMatrix Parse(string size, string ig, string jg,
            string di, string gg)
        {
            return new CompressedSymmMatrix(int.Parse(size),
                ig.Split(' ').Select(value => int.Parse(value)).ToArray(),
                jg.Split(' ').Select(value => int.Parse(value)).ToArray(),
                di.Split(' ').Select(value => double.Parse(value)).ToArray(),
                gg.Split(' ').Select(value => double.Parse(value)).ToArray());
        }
    }
}
