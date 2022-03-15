using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EParser;

namespace MCE
{
    public enum BoundaryConditionType
    {
        First = 1,
        Second,
        Third
    }
    public class BoundaryCondition
    {
        public BoundaryConditionType Type { get; private set; }
        public Func Value { get; private set; }
        public int[] Vertexes { get; private set; }
        public double Beta { get; private set; }

        public BoundaryCondition(BoundaryConditionType type, Func value, int[] vertexes, double beta = 0)
        {
            Type = type;
            Value = value;
            Vertexes = vertexes;
            Beta = beta;
        }
        public static BoundaryCondition Parse(ExpressionParser p, string type_str, string value_str, string vertexes_str, string beta_str = null)
        {
            BoundaryConditionType type = (BoundaryConditionType)int.Parse(type_str);
            int[] vertexes = vertexes_str.Split(' ').Select(v => int.Parse(v)).ToArray();
            double beta = 0;
            if (beta_str != null)
                beta = double.Parse(beta_str);
            return new BoundaryCondition(type, p.GetFunction(value_str), vertexes, beta);
        }
        public static BoundaryCondition Parse(string type_str, Func value, string vertexes_str, string beta_str = null)
        {
            BoundaryConditionType type = (BoundaryConditionType)int.Parse(type_str);
            int[] vertexes = vertexes_str.Split(' ').Select(v => int.Parse(v)).ToArray();
            double beta = 0;
            if (beta_str != null)
                beta = double.Parse(beta_str);
            return new BoundaryCondition(type, value, vertexes, beta);
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