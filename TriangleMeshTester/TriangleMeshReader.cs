using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TriangleMeshTester
{
    public static class TriangleMeshReader
    {
        public static Result TryReadMeshFrom(string filename, out TriangleMesh triangleMesh)
        {
            triangleMesh = null;

            if (!File.Exists(filename))
                return new Result() { IsSuccessful = false, Message = $"File '{filename}' doesn't exist." };

            triangleMesh = new TriangleMesh();
            Result result;
            var separator = new string[] { " " };
            using(StreamReader sr = new StreamReader(filename))
            {
                while(!sr.EndOfStream)
                {
                    var lineValues = sr.ReadLine().Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    if (lineValues.Length < 2)
                        return new Result() { IsSuccessful = false, Message = $"File format is wrong. ({lineValues})" };

                    switch(lineValues[0])
                    {
                        case "v":
                            if (lineValues.Length != 3)
                                return new Result() { IsSuccessful = false, Message = $"Vertex format is wrong. ({lineValues})" };

                            double x, y;
                            if (!double.TryParse(lineValues[1], out x) || !double.TryParse(lineValues[2], out y))
                                return new Result() { IsSuccessful = false, Message = $"Can't parse vertex from values: {lineValues[1]}, {lineValues[2]} " };

                            result = triangleMesh.TryAddPoint(x, y, 1E-7);
                            if (!result.IsSuccessful)
                                return new Result() { IsSuccessful = false, Message = $"Can't add vertex ({x}, {y}).", InnerResult = result };
                            break;
                        case "e":
                            if (lineValues.Length != 4)
                                return new Result() { IsSuccessful = false, Message = $"Element format is wrong. ({lineValues})" };

                            int[] element = new int[3];
                            for(int i = 0; i < element.Length; i++)
                            {
                                if (!int.TryParse(lineValues[i + 1], out element[i]))
                                    return new Result() { IsSuccessful = false, Message = $"Can't parse element from values: {lineValues.Skip(1)}" };
                            }

                            result = triangleMesh.TryAddElement(element);
                            if (!result.IsSuccessful)
                                return new Result() { IsSuccessful = false, Message = $"Can't add element ({lineValues.Skip(1)})", InnerResult = result };
                            break;
                        case "b":
                            var borderValues = lineValues.Skip(1);
                            List<int> border = new List<int>();
                            foreach(var value in borderValues)
                            {
                                int index;
                                if (!int.TryParse(value, out index))
                                    return new Result() { IsSuccessful = false, Message = "Can't parse border." };

                                border.Add(index);
                            }

                            result = triangleMesh.TrySetBorder(border.ToArray());
                            if (!result.IsSuccessful)
                                return new Result() { IsSuccessful = false, Message = "Can't set border.", InnerResult = result };
                            break;
                    }
                }
            }

            return Result.OK;
        }
    }
}
