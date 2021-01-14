using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MCE
{
    class Program
    {
        static List<double[]> globalList;
        static ExpressionParser parser = new ExpressionParser();
        static void Main(string[] args)
        {
            //int i = 0;
            //string directory = Path.Combine(Environment.CurrentDirectory, "test4");
            //string elemsPath = "";
            //string vertexesPath = "";
            //List<string> boundaryConditionsPaths = new List<string>();
            //string materialPath = "";
            //string diffCoeffsPath = "";
            //Console.WriteLine("Enter directory with files: ");
            //
            //directory = Console.ReadLine();
            //string[] files = Directory.GetFiles(directory);
            //while (!Directory.Exists(directory) && files.Length != 0)
            //{
            //    Console.ForegroundColor = ConsoleColor.Red;
            //    Console.WriteLine("Directory isn't exist or empty. Try again");
            //    Console.ResetColor();
            //    directory = Console.ReadLine();
            //    files = Directory.GetFiles(directory);
            //}
            //
            //string temp;
            //for (i = 0; i < files.Length; i++)
            //{
            //    temp = Path.GetFileName(files[i]);
            //    switch (temp)
            //    {
            //        case "vertexes.txt": vertexesPath = files[i]; break;
            //        case "elements.txt": elemsPath = files[i]; break;
            //        case "materials.txt": materialPath = files[i]; break;
            //        case "diffcoeffs.txt": diffCoeffsPath = files[i]; break;
            //        default:
            //            if (temp.StartsWith("S") && temp.EndsWith(".txt"))
            //                boundaryConditionsPaths.Add(files[i]);
            //            break;
            //    }
            //}
            //string[] vertexes_str = File.ReadAllLines(vertexesPath);
            //string[] elems_str = File.ReadAllLines(elemsPath);
            //string[][] boundCond_str = new string[boundaryConditionsPaths.Count][];
            //for (i = 0; i < boundaryConditionsPaths.Count; i++)
            //    boundCond_str[i] = File.ReadAllLines(boundaryConditionsPaths[i]);
            //
            //int vertexesCount = vertexes_str.Length;
            //int elemCount = elems_str.Length;
            //
            //double[][] vertexes = new double[vertexesCount][];
            //for (i = 0; i < vertexesCount; i++)
            //    vertexes[i] = vertexes_str[i].Split(' ').Select(v => double.Parse(v)).ToArray();
            //
            //int[][] elems = new int[elemCount][];
            //for (i = 0; i < elemCount; i++)
            //    elems[i] = elems_str[i].Split(' ').Select(e => int.Parse(e)).ToArray();
            //
            //double[] materials = File.ReadAllText(materialPath).Split(' ').Select(m => double.Parse(m)).ToArray();
            //double[] diffCoeffs = File.ReadAllText(diffCoeffsPath).Split(' ').Select(m => double.Parse(m)).ToArray();
            //
            //BoundaryCondition[] conditions = new BoundaryCondition[boundCond_str.Length];
            //Func[] values = new Func[boundCond_str.Length];
            //values[0] = (x, y) => y;
            //values[1] = (x, y) => 1.0;
            //values[2] = (x, y) => 0;
            //
            //for (i = 0; i < conditions.Length; i++)
            //    conditions[i] = BoundaryCondition.Parse(boundCond_str[i][0], values[i], boundCond_str[i][1]);
            //
            //Func[] function = new Func[elemCount];
            //function[0] = (x, y) => 2.0 * y;
            //function[1] = (x, y) => y;
            //function[2] = (x, y) => y;
            //
            //Solver s = new Solver(elemCount, vertexesCount,
            //    elems, vertexes, materials, diffCoeffs,
            //    function, conditions);
            //s.Solve();
            //
            //Mesh mesh = new Mesh(vertexes, elems);
            //mesh.BuildMesh(0.05, 0.05);
            //
            //List<double[]> points = mesh.meshXY;
            //double[] point;
            //
            //using (StreamWriter sw = new StreamWriter(Path.Combine(directory, "function.txt")))
            //{
            //    sw.Write("x,y,f\n");
            //    for (i = 0; i < points.Count; i++)
            //    {
            //        point = points[i];
            //        sw.Write(point[0] + "," + point[1] + "," + s.GetValue(point[0], point[1]) + "\n");
            //    }
            //}
            //
            //System.Diagnostics.Process.Start(Path.Combine(directory, "graph.py"));
            //globalList = new List<double[]>();
            //globalList.Add(new double[] { 0.25, 0.5 });
            //globalList.Add(new double[] { 0.25, 1.5 });
            //globalList.Add(new double[] { 0.5, 1.75 });
            //globalList.Add(new double[] { 1.5, 1.75 });
            //globalList.Add(new double[] { 1.75, 1.5 });
            //globalList.Add(new double[] { 1.75, 0.5 });
            //globalList.Add(new double[] { 1.5, 0.25 });
            //globalList.Add(new double[] { 0.5, 0.25 });
            //

            //Console.ReadLine();

            Mesh2D xy = new Mesh2D();
            xy.BuildMesh(-50, 50, -50, 50, 10f, 10f);

            test1();
        }

        static void test1()
        {
            int i = 0;
            string directory = Path.Combine(Environment.CurrentDirectory, "test9");
            string elemsPath = "";
            string vertexesPath = "";
            List<string> boundaryConditionsPaths = new List<string>();
            string materialPath = "";
            string diffCoeffsPath = "";
            string functionPath = "";
            //Console.WriteLine("Enter directory with files: ");

            //directory = Console.ReadLine();
            string[] files = Directory.GetFiles(directory);
            while (!Directory.Exists(directory) && files.Length != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Directory isn't exist or empty. Try again");
                Console.ResetColor();
                directory = Console.ReadLine();
                files = Directory.GetFiles(directory);
            }

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
            string[] vertexes_str = File.ReadAllLines(vertexesPath);
            string[] elems_str = File.ReadAllLines(elemsPath);
            string[][] boundCond_str = new string[boundaryConditionsPaths.Count][];
            for (i = 0; i < boundaryConditionsPaths.Count; i++)
                boundCond_str[i] = File.ReadAllLines(boundaryConditionsPaths[i]);

            int vertexesCount = vertexes_str.Length;
            int elemCount = elems_str.Length;

            double[][] vertexes = new double[vertexesCount][];
            for (i = 0; i < vertexesCount; i++)
                vertexes[i] = vertexes_str[i].Split(' ').Select(v => double.Parse(v)).ToArray();

            int[][] elems = new int[elemCount][];
            for (i = 0; i < elemCount; i++)
                elems[i] = elems_str[i].Split(' ').Select(e => int.Parse(e)).ToArray();

            //double[] materials = File.ReadAllText(materialPath).Split(' ').Select(m => double.Parse(m)).ToArray();
            //double[] diffCoeffs = File.ReadAllText(diffCoeffsPath).Split(' ').Select(m => double.Parse(m)).ToArray();
            double[] materials = new double[elemCount];
            double[] diffCoeffs = new double[elemCount];
            
            for(i = 0; i < elemCount; i++)
            {
                materials[i] = 3.0;
                diffCoeffs[i] = 2.0;
            }
            
            BoundaryCondition[] conditions = new BoundaryCondition[boundCond_str.Length];
            Func[] values = new Func[boundCond_str.Length];
            
            for (i = 0; i < conditions.Length; i++)
                conditions[i] = BoundaryCondition.Parse(parser, boundCond_str[i][0], 
                    boundCond_str[i][1], boundCond_str[i][2]);
            
            Func[] function = new Func[elemCount];
            Func t = parser.getDelegate(File.ReadAllText(functionPath));
            for (i = 0; i < function.Length; i++)
                function[i] = t;
            
            Solver s = new Solver(elemCount, vertexesCount,
                elems, vertexes, materials, diffCoeffs,
                function, conditions);
            s.Solve();

            using (Window win = new Window(vertexes, elems, s.coeffs, 800, 800, "Mesh"))
            {
                win.Run(120.0);
            }

            




            //
            //Func<double, double, double> u = (x, y) => x * x * x * x + y * y;
            //List<double> uf = new List<double>();
            //List<double> ui = new List<double>();
            //
            //double sum1 = 0;
            //double sum2 = 0;
            //double[] curVert;
            //for(i = 0; i < globalList.Count; i++)
            //{
            //    curVert = globalList[i];
            //    ui.Add(u(curVert[0], curVert[1]));
            //    Console.WriteLine(ui[i].ToString("E4"));
            //}
            //Console.WriteLine();
            //for (i = 0; i < globalList.Count; i++)
            //{
            //    curVert = globalList[i];
            //    uf.Add(s.GetValue(curVert[0], curVert[1]));
            //    Console.WriteLine(uf[i].ToString("E4"));
            //}
            //Console.WriteLine();
            //for(i = 0; i < globalList.Count; i++)
            //{
            //    curVert = globalList[i];
            //    sum1 += (ui[i] - uf[i]) * (ui[i] - uf[i]);
            //    sum2 += ui[i] * ui[i];
            //    Console.WriteLine(Math.Abs(ui[i] - uf[i]).ToString("E4"));
            //}
            //Console.WriteLine();
            //Console.WriteLine(Math.Sqrt(sum1 / sum2).ToString("E4"));
            //
            //Mesh mesh = new Mesh(vertexes, elems);
            //mesh.BuildMesh(0.1, 0.1);
            //List<double[]> points = mesh.meshXY;
            //
            //using (StreamWriter sw = new StreamWriter("function.txt"))
            //{
            //    sw.WriteLine("x,y,z");
            //    for (i = 0; i < points.Count; i++)
            //    {
            //        curVert = points[i];
            //        sw.WriteLine(curVert[0] + "," + curVert[1] + "," + s.GetValue(curVert[0], curVert[1]));
            //    }
            //}

            //System.Diagnostics.Process.Start("graph.py");
            Console.ReadLine();
        }
    }
}
