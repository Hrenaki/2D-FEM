using System;
using System.Collections.Generic;

namespace MCE
{
    class Mesh
    {

    }

    class Mesh2D
    {
        List<float> x, y;
        public int LinesCount { get { return 2 * (x.Count + y.Count); } }
        public double dx { get; private set; } 
        public double dy { get; private set; } 
        public double MinX { get { return x[0]; } }
        public double MaxX { get { return x[x.Count - 1]; } }
        public double MinY { get { return y[0]; } }
        public double MaxY { get { return y[y.Count - 1]; } }

        public Mesh2D()
        {
            x = new List<float>();
            y = new List<float>();
        }

        public void BuildMesh(double minX, double maxX, double minY, double maxY,
            double dx, double dy)
        {
            this.dx = dx;
            this.dy = dy;

            double t;
            int i;
            double specPoint = (minX < 0 ? Math.Ceiling(minX / dx) : Math.Floor(minX / dx)) * dx;

            if(x.Count != 0)
                x.Clear();

            x.Add((float)minX);
            if (Math.Abs(x[x.Count - 1] - specPoint) > 1E-10)
                x.Add((float)specPoint);

            for (t = specPoint + dx, i = 1; t <= maxX; t = specPoint + (++i) * dx)
                x.Add((float)t);

            if (Math.Abs(x[x.Count - 1] - maxX) > 1E-10)
                x.Add((float)maxX);

            if(y.Count != 0)
                y.Clear();

            specPoint = (minY < 0 ? Math.Ceiling(minY / dy) : Math.Floor(minY / dy)) * dy;

            y.Add((float)minY);
            if (Math.Abs(y[y.Count - 1] - specPoint) > 1E-10)
                y.Add((float)specPoint);

            for (t = specPoint + dy, i = 1; t <= maxY; t = specPoint + (++i) * dy)
                y.Add((float)t);

            if (Math.Abs(y[y.Count - 1] - maxY) > 1E-10)
                y.Add((float)maxY);
        }
        public float[] ToVertexBuffer()
        {
            float[] vertexes = new float[4 * (x.Count + y.Count)];
            int yCount = y.Count;
            int xCount = x.Count;
            int k;
            for (k = 0; k < xCount; k++)
            {
                vertexes[4 * k] = x[k];
                vertexes[4 * k + 1] = y[0];
                vertexes[4 * k + 2] = x[k];
                vertexes[4 * k + 3] = y[yCount - 1];
            }
            xCount *= 4;
            for(k = 0; k < yCount; k++)
            {
                vertexes[xCount + 4 * k] = x[0];
                vertexes[xCount + 4 * k + 1] = y[k];
                vertexes[xCount + 4 * k + 2] = x[x.Count - 1];
                vertexes[xCount + 4 * k + 3] = y[k];
            }
            return vertexes;
        }
    }
}
