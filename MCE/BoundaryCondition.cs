using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EParser;

namespace MCE
{
    public class BoundaryCondition
    {
        public int Type { get; private set; }
        public Func Value { get; private set; }
        public int[] Vertexes { get; private set; }

        public BoundaryCondition(int type, Func value, params int[] vertexes)
        {
            Type = type;
            Value = value;
            Vertexes = vertexes;
        }
        public static BoundaryCondition Parse(ExpressionParser p, string type_str, string value_str, string vertexes_str)
        {
            int t = int.Parse(type_str);
            int[] vertexes = vertexes_str.Split(' ').Select(v => int.Parse(v)).ToArray();

            return new BoundaryCondition(t, p.GetFunction(value_str), vertexes);
        }
        public static BoundaryCondition Parse(string type_str, Func value, string vertexes_str)
        {
            int t = int.Parse(type_str);
            int[] vertexes = vertexes_str.Split(' ').Select(v => int.Parse(v)).ToArray();

            return new BoundaryCondition(t, value, vertexes);
        }
        public bool CheckEdge(int v1, int v2)
        {
            for(int i = 0; i < Vertexes.Length - 1; i++)
                if (Vertexes[i] == v1 && Vertexes[i + 1] == v2 ||
                    Vertexes[i] == v2 && Vertexes[i + 1] == v1)
                    return true;
            return false;
        }
    }
}