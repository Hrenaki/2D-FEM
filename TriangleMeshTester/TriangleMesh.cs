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
        private List<int> border = new List<int>();

        public double[][] Vertices => vertices.ToArray();
        public int[][] Elements => elements.ToArray();

        public int[] Border => border.ToArray();

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

        public Result TrySetBorder(int[] border)
        {
            this.border.Clear();

            if (border.Length < 2)
                return new Result() { IsSuccessful = false, Message = "Border must contain 2 points at least." };

            if (border[0] != border[border.Length - 1])
                return new Result() { IsSuccessful = false, Message = "Border isn't closed." };

            int start, end;
            bool edgeExists;
            for(int i = 0; i < border.Length - 1; i++)
            {
                edgeExists = false;
                start = Math.Max(border[i], border[i + 1]);
                end = Math.Max(border[i], border[i + 1]);

                foreach(var element in elements)
                {
                    if(element[0] == start && (element[1] == end || element[2] == end) ||
                       element[1] == start && element[2] == end)
                    {
                        edgeExists = true;
                        break;
                    }
                }

                if (!edgeExists)
                    return new Result() { IsSuccessful = false, Message = $"Edge ({border[i]}, {border[i + 1]}) doesn't exist." };
            }

            this.border.AddRange(border);
            return Result.OK;
        }
    }
}
