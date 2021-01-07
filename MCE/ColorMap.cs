using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCE
{
    class ColorMap
    {
        public float[][] vColors { get; }

        public ColorMap(double[] mesh)
        {
            vColors = new float[mesh.Length][];

            float min = (float) mesh.Min();
            float h = (float) mesh.Max() - min;
            float temp;
            for (int i = 0; i < mesh.Length; i++)
            {
                temp = (float)(mesh[i] - min) / h;
                vColors[i] = new float[] { temp, 0.5f, 0.5f};
            }
        }
        public float[] ColorsToArray()
        {
            float[] colors = new float[vColors.Length * 3];
            int pos = 0;
            for(int i = 0; i < vColors.Length; i++)
            {
                colors[pos] = vColors[i][0];
                colors[pos + 1] = vColors[i][1];
                colors[pos + 2] = vColors[i][2];
                pos += 3;
            }
            return colors;
        }
    }
}
