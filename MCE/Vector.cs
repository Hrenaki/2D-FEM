using System;
using System.Linq;

namespace MCE
{
    public class Vector
    {
        public double[] values;
        public int size { get { return values.Length; } }
        public double magnitude { get { double val = 0; foreach (double v in values) val += v * v; return Math.Sqrt(val); } }
        public double sqrMagnitude { get { double val = 0; foreach (double v in values) val += v * v; return val; } }
        public double this[int i] { get { return values[i]; } set { values[i] = value; } }
        public Vector(int size)
        {
            values = new double[size];
        }
        public Vector(params double[] values)
        {
            this.values = values;
        }
        public Vector(Vector vec)
        {
            values = new double[vec.size];
            vec.CopyTo(this);
        }
        public static Vector GenerateSimpleVector(int size)
        {
            Vector vec = new Vector(size);
            for (int i = 0; i < size; i++)
                vec[i] = i + 1;
            return vec;
        }
        public void CopyTo(Vector vec)
        {
            for (int i = 0; i < values.Length; i++)
                vec.values[i] = values[i];
        }
        public override string ToString()
        {
            return string.Join("\n ", values.Select(v => v.ToString("E5")));
        }
        public static Vector operator-(Vector lhs, Vector rhs)
        {
            Vector res = new Vector(lhs.size);
            for (int i = 0; i < res.size; i++)
                res[i] = lhs[i] - rhs[i];
            return res;
        }
    }
}
