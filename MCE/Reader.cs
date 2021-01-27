using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCE
{
    class Reader
    {
        public Reader()
        { 

        }

        public static Problem ReadProblemFrom(string directory)
        {
            ExpressionParser parser = new ExpressionParser();

            string elemsPath = "";
            string vertexesPath = "";
            List<string> boundaryConditionsPaths = new List<string>();
            string materialPath = "";
            string diffCoeffsPath = "";
            string functionPath = "";
            string[] files = Directory.GetFiles(directory);

            if(!Directory.Exists(directory) && files.Length != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Directory isn't exist or empty. Try again");
                Console.ResetColor();
                return null;
            }

            int i;
            string temp;
            for (i = 0; i < files.Length; i++)
            {
                temp = Path.GetFileName(files[i]);
                switch (temp)
                {
                    case "vertexes.txt": vertexesPath = files[i]; break;
                    case "elements.txt": elemsPath = files[i]; break;
                    case "materials.txt": materialPath = files[i]; break;
                    case "diffcoeffs.txt": diffCoeffsPath = files[i]; break;
                    case "function.txt": functionPath = files[i]; break;
                    default:
                        if (temp.StartsWith("S") && temp.EndsWith(".txt"))
                            boundaryConditionsPaths.Add(files[i]);
                        break;
                }
            }

            // Reading vertexes
            string[] content = File.ReadAllLines(vertexesPath);
            int vertexesCount = content.Length;
            double[][] vertexes = new double[vertexesCount][];
            for (i = 0; i < vertexesCount; i++)
                vertexes[i] = content[i].Split(' ').Select(v => double.Parse(v)).ToArray();

            // Reading and compiling all function parts in right part of equation
            content = File.ReadAllLines(functionPath);
            Func[] possibleFunctions = new Func[content.Length];
            for (i = 0; i < possibleFunctions.Length; i++)
                possibleFunctions[i] = parser.getDelegate(content[i]);

            // Reading elements
            content = File.ReadAllLines(elemsPath);
            int elemCount = content.Length;
            int[][] elems = new int[elemCount][];
            Func[] function = new Func[elemCount];
            int[] elem;
            for (i = 0; i < elemCount; i++)
            {
                elem = content[i].Split(' ').Select(e => int.Parse(e)).ToArray();
                Array.Copy(elem, elems[i], 3);
                function[i] = possibleFunctions[elem[3]];
            }

            // Reading materials and diff coeffs
            double[] materials = File.ReadAllText(materialPath).Split(' ').Select(m => double.Parse(m)).ToArray();
            double[] diffCoeffs = File.ReadAllText(diffCoeffsPath).Split(' ').Select(m => double.Parse(m)).ToArray();

            // Reading boundary conditions
            string[][] boundCond_str = new string[boundaryConditionsPaths.Count][];
            for (i = 0; i < boundaryConditionsPaths.Count; i++)
                boundCond_str[i] = File.ReadAllLines(boundaryConditionsPaths[i]);
            BoundaryCondition[] conditions = new BoundaryCondition[boundCond_str.Length];
            Func[] values = new Func[boundCond_str.Length];
            for (i = 0; i < conditions.Length; i++)
                conditions[i] = BoundaryCondition.Parse(parser, boundCond_str[i][0],
                    boundCond_str[i][1], boundCond_str[i][2]);

            return new Problem(elemCount, vertexesCount, elems, vertexes,
                materials, diffCoeffs, function, conditions);
        }
    }
}
