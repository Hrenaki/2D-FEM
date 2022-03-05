using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleMeshTester
{
    public class TriangleMesh
    {
        private List<double[]> vertices = new List<double[]>();
        private List<int[]> elements = new List<int[]>();

        public double[][] Vertices { get; }
        public int[][] Elements { get; }

        public Result TryAddPoint(double x, double y, double eps)
        {
            var result = new Result();

            var pointNotExists = !vertices.Exists(point => Math.Abs(point[0] - x) < eps && Math.Abs(point[1] - y) < eps);
            if (!pointNotExists)
                vertices.Add(new double[] { x, y });

            return new Result() { IsSuccessful = pointNotExists, Message = pointNotExists ? string.Empty : "Point already exists." };
        }

        public Result TryAddElement(int[] element)
        {
            if (element.Length != 3)
                return new Result() { IsSuccessful = false, Message = "Element isn't a triangle." };

            var sortedElement = new int[element.Length];
            Array.Copy(element, sortedElement, element.Length);
            Array.Sort(sortedElement);

            for(int i = 0; i < sortedElement.Length - 1; i++)
            {
                if (sortedElement[i] == sortedElement[i + 1])
                    return new Result() { IsSuccessful = false, Message = "Element is a degenerate triangle." };
            }

            var elementNotExists = !elements.Exists(e => e[0] == sortedElement[0] && e[1] == sortedElement[1] && e[2] == sortedElement[2]);
            if (elementNotExists)
                elements.Add(sortedElement);

            return new Result() { IsSuccessful = elementNotExists, Message = elementNotExists ? string.Empty : "Element already exists." };
        }
    }
}
