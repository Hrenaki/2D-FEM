using System;
using System.Collections.Generic;

namespace MCE
{
    class Mesh
    {
        public List<double[]> meshXY { get; }

        double minX, maxX;
        double minY, maxY;
        double eps = 1E-10;

        double dx;
        double dy;

        double[][] vertexes;
        int[][] elems;

        public Mesh(double[][] vertexes, int[][] elems)
        {
            this.vertexes = vertexes;
            this.elems = elems;
            meshXY = new List<double[]>();
        }
        public void BuildMesh(double dx, double dy)
        {
            minX = vertexes[0][0];
            minY = vertexes[0][1];
            maxX = minX;
            maxY = minY;

            this.dx = dx;
            this.dy = dy;

            double[] curVert;
            int i, j;

            for(i = 1; i < vertexes.GetLength(0); i++)
            {
                curVert = vertexes[i];
                if (curVert[0] < minX)
                    minX = curVert[0];
                if (curVert[1] < minY)
                    minY = curVert[1];
                if (curVert[0] > maxX)
                    maxX = curVert[0];
                if (curVert[1] > maxY)
                    maxY = curVert[1];
            }

            double x, y;
            for(x = minX, i = 0; x <= maxX; x = minX + (++i) * dx)
            {
                for(y = minY, j = 0; y <= maxY; y = minY + (++j) * dy)
                {
                    if (IsBelongs(x, y))
                        addPoint(x, y);
                    else AddNeighbours(x, y);
                }
            }
        }
        private bool IsBelongs(double x, double y)
        {
            int[] numbers;
            double[] s = new double[3];
            double[] v1, v2, v3;
            double detD;
            int i;
            for (i = 0; i < elems.Length; i++)
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

                if (Math.Abs(detD - s[0] - s[1] - s[2]) < eps)
                    break;
            }

            if (i == elems.Length)
                return false;
            return true;
        }
        private bool IsBelongs(double x, double y, out int elem)
        {
            int[] numbers;
            double[] s = new double[3];
            double[] v1, v2, v3;
            double detD;
            int i;
            for (i = 0; i < elems.Length; i++)
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

                if (Math.Abs(detD - s[0] - s[1] - s[2]) < eps)
                    break;
            }

            if (i == elems.Length)
            {
                elem = -1;
                return false;
            }
            elem = i;
            return true;
        }
        private void AddNeighbours(double x, double y)
        {
            double tempX = x - dx, tempY = y - dy;
            int k;
            int temp;

            k = 1;
            // слева
            while(!IsBelongs(tempX, y, out temp) && Math.Abs(tempX - minX) > eps)
                tempX = x - (++k) * dx;
            if (temp != -1)
                AddNeighbour(tempX, y, x, y, temp);

            // справа
            k = 1;
            tempX = x + dx;
            while (!IsBelongs(tempX, y, out temp) && Math.Abs(tempX - maxX) < eps)
                tempX = x + (++k) * dx;
            if (temp != -1)
                AddNeighbour(tempX, y, x, y, temp);

            // снизу
            k = 1;
            while (!IsBelongs(x, tempY, out temp) && Math.Abs(tempY - minY) < eps)
                tempY = y - (++k) * dy;
            if (temp != -1)
                AddNeighbour(x, tempY, x, y, temp);

            // сверху
            k = 1;
            tempY = y + dy;
            while (!IsBelongs(x, tempY, out temp) && Math.Abs(tempY - maxY) > eps)
                tempY = y + (++k) * dy;
            if (temp != -1)
                AddNeighbour(x, tempY, x, y, temp);
        }
        private void AddNeighbour(double tempX, double tempY, double rootX, double rootY, int elem)
        {
            double[] intersPoint;
            int[] numbers = elems[elem];
            intersPoint = GetIntersectionPoint(tempX, tempY, rootX, rootY,
                vertexes[numbers[0]][0], vertexes[numbers[0]][1],
                vertexes[numbers[1]][0], vertexes[numbers[1]][1]);
            if (intersPoint != null)
                addPoint(intersPoint[0], intersPoint[1]);
            else
            {
                intersPoint = GetIntersectionPoint(tempX, tempY, rootX, rootY,
                vertexes[numbers[0]][0], vertexes[numbers[0]][1],
                vertexes[numbers[2]][0], vertexes[numbers[2]][1]);
                if (intersPoint != null)
                    addPoint(intersPoint[0], intersPoint[1]);
                else
                {
                    intersPoint = GetIntersectionPoint(tempX, tempY, rootX, rootY,
                        vertexes[numbers[1]][0], vertexes[numbers[1]][1],
                        vertexes[numbers[2]][0], vertexes[numbers[2]][1]);
                    addPoint(intersPoint[0], intersPoint[1]);
                }
            }
        }
        private void addPoint(double x, double y)
        {
            if(meshXY.Find(v => v[0].ToString("E6") == x.ToString("E6") && v[1].ToString("E6") == y.ToString("E6")) == null)
                meshXY.Add(new double[] { x, y });
        }
        private double[] GetIntersectionPoint(double p0X, double p0Y, double p1X, double p1Y,
            double p2X, double p2Y, double p3X, double p3Y)
        {
            double s1X, s1Y, s2X, s2Y;
            s1X = p1X - p0X; 
            s1Y = p1Y - p0Y;
            s2X = p3X - p2X; 
            s2Y = p3Y - p2Y;

            double s, t;
            s = (-s1Y * (p0X - p2X) + s1X * (p0Y - p2Y)) / (-s2X * s1Y + s1X * s2Y);
            t = (s2X * (p0Y - p2Y) - s2Y * (p0X - p2X)) / (-s2X * s1Y + s1X * s2Y);

            s = double.Parse(s.ToString("F8"));
            t = double.Parse(t.ToString("F8"));

            if (s >= 0 && s <= 1.0 && t >= 0 && t <= 1.0)
                return new double[] { p0X + (t * s1X), p0Y + (t * s1Y) };
            return null; 
        }
        public void RebuildMesh(double dx, double dy)
        {
            meshXY.Clear();
            this.dx = dx;
            this.dy = dy;
        }
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

        public uint[] ToElementBuffer()
        {
            uint[] lines = new uint[2 * (x.Count + y.Count)];
            for (uint i = 0; i < lines.Length; i++)
                lines[i] = i;
            return lines;
        }
    }
}
