using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OptimizationMethods;

using NumMath;
using NumMath.Splines;
using EParser;

namespace MCE.Problems.Magnetic
{
    public class MagneticProblem
    {
        RectangleMesh mesh;
        BoundaryCondition[] conditions;

        public Vector coeffs;
        Vector tmpVector;
        Vector globalB;

        public MagneticProblem(RectangleMesh mesh, BoundaryCondition[] conditions)
        {
            this.mesh = mesh;
            this.conditions = conditions;

            coeffs = new Vector(mesh.VertexCount);
            tmpVector = new Vector(mesh.VertexCount);
            globalB = new Vector(mesh.VertexCount);
        }

        private void GeneratePortrait(out double[] di, out int[] ig, out int[] jg, out double[] gg)
        {
            int vertexCount = mesh.VertexCount;
            int elemCount = mesh.ElementCount;

            di = new double[vertexCount];
            ig = new int[vertexCount + 1];

            int[] numbers = new int[4];
            int i, j, k, s;
            List<int> temp;

            List<int>[] adjacencyList = new List<int>[vertexCount];

            for (i = 0; i < elemCount; i++) // element loop
            {
                Array.Copy(mesh.Elements.ElementAt(i).Element, numbers, 4);
                Array.Sort(numbers); // global numbers of element's vertexes
                for (j = 1; j < numbers.Length; j++) // loop for global numbers
                {
                    for (k = 0; k < j; k++) // taking edge
                    {
                        if (adjacencyList[numbers[j]] == null) // if vertex's list is empty, create it
                        {
                            adjacencyList[numbers[j]] = new List<int>();
                            adjacencyList[numbers[j]].Add(numbers[k]);
                        }
                        else // if it's not, find position to insert
                        {    // due to numbers of related vertexes are sorted in ascending order
                            temp = adjacencyList[numbers[j]];
                            for (s = 0; s < temp.Count; s++)
                            {
                                if (temp[s] == numbers[k])
                                {
                                    s = -1;
                                    break;
                                }
                                if (numbers[k] < temp[s])
                                    break;
                            }
                
                            if (s != -1 || s == temp.Count)
                                temp.Insert(s, numbers[k]);
                        }
                    }
                }
            }

            int edgeCount = 0;
            foreach (var edges in adjacencyList)
            {
                if (edges != null)
                {
                    edgeCount += edges.Count;
                }
            }

            jg = new int[edgeCount];
            gg = new double[jg.Length];

            for (i = 1; i < adjacencyList.Length; i++)
            {
                ig[i + 1] = ig[i];
                if (adjacencyList[i] != null)
                    for (j = 0; j < adjacencyList[i].Count; j++)
                    {
                        jg[ig[i] + j] = adjacencyList[i][j];
                        ig[i + 1]++;
                    }
            }
        }

        public void SolveLinear(Vector q0, double mu0)
        {
            int[] ig, jg;
            double[] di, gg;

            GeneratePortrait(out di, out ig, out jg, out gg);

            int size = 4;
            double[,] localMatrix = new double[size, size]; // local matrix
            double[] localB = new double[size]; // local B

            Material material;

            int[] numbers = new int[size]; // global numbers of element's vertexes

            int i, j, k, s, m;
            Point curVert;

            int condSize = conditions.Length;
            int[] curBound;

            int elemCount = mesh.ElementCount;
            int vertexCount = mesh.VertexCount;

            IReadOnlyCollection<RectangleElement> elements = mesh.Elements;
            IReadOnlyCollection<Point> vertices = mesh.Vertices;
            RectangleElement element;

            double hx, hy;
            double hyx, hxy;
            double coeff;

            // element loop
            for (k = 0; k < elemCount; k++)
            {
                element = mesh.Elements.ElementAt(k);
                material = mesh.Materials[element.Element[size]];

                int[] globalToLocalNumbers = new int[size];

                Array.Copy(element.Element, numbers, size);
                Array.Sort(numbers);

                mesh.GetElementBounds(element, out double minX, out double maxX, out double minY, out double maxY);

                hx = maxX - minX;
                hy = maxY - minY;

                int localNumber = 0;
                i = 0;
                foreach(Point currentVertex in numbers.Select(vertexIndex => vertices.ElementAt(vertexIndex)))
                {
                    localNumber = currentVertex.Y == minY ? 0 : 2;
                    localNumber += currentVertex.X == minX ? 0 : 1;
                    globalToLocalNumbers[i] = localNumber;
                    i++;
                }

                coeff = 1.0 / (6.0 * mu0 * material.RelativeMagneticPermeability);
                hyx = hy / hx;
                hxy = hx / hy;

                // first row
                localMatrix[0, 0] = coeff * (-2.0 * hyx + hxy); // 0 - 1
                localMatrix[0, 1] = coeff * (hyx - 2.0 * hxy); // 0 - 2
                localMatrix[0, 2] = coeff * (-hyx - hxy); // 0 - 3
                localMatrix[0, 3] = coeff * (2.0 * hyx + 2.0 * hxy); // 0 - 0
                
                // second row
                localMatrix[1, 0] = localMatrix[0, 0]; // 1 - 0
                localMatrix[1, 1] = coeff * (-hyx - hxy); // 1 - 2
                localMatrix[1, 2] = coeff * (hyx - 2.0 * hxy); // 1 - 3
                localMatrix[1, 3] = localMatrix[0, 3]; // 1 - 1
                
                // third row
                localMatrix[2, 0] = localMatrix[0, 1]; // 2 - 0
                localMatrix[2, 1] = localMatrix[1, 1]; // 2 - 1
                localMatrix[2, 2] = coeff * (-2.0 * hyx + hxy); // 2 - 3
                localMatrix[2, 3] = localMatrix[0, 3]; // 2 - 2

                // forth row
                localMatrix[3, 0] = localMatrix[0, 2]; // 3 - 0
                localMatrix[3, 1] = localMatrix[1, 2]; // 3 - 1
                localMatrix[3, 2] = localMatrix[2, 2]; // 3 - 2
                localMatrix[3, 3] = localMatrix[0, 3]; // 3 - 3

                // local B
                localB[0] = material.CurrentsDistributionDensity * 0.25 * hx * hy;
                localB[1] = localB[0];
                localB[2] = localB[0];
                localB[3] = localB[0];

                for (i = 0; i < size; i++)
                    di[numbers[i]] += localMatrix[globalToLocalNumbers[i], 3];

                for(i = 0; i < size; i++)
                {
                    m = ig[numbers[i]];
                    for(j = 0; j < i; j++)
                    {
                        for(s = m; s < ig[numbers[i] + 1]; s++)
                        {
                            if(jg[s] == numbers[j])
                            {
                                gg[s] += localMatrix[globalToLocalNumbers[i], globalToLocalNumbers[j]];
                                m++;
                                break;
                            }
                        }
                    }
                }
            
                // adding local B to global B
                for (i = 0; i < size; i++)
                    globalB[numbers[i]] += localB[globalToLocalNumbers[i]];
            }

            Func localFunction;
            // First type boundary conditions
            foreach (BoundaryCondition condition in conditions.Where(condition => condition.Type == BoundaryConditionType.First))
            {
                curBound = condition.Vertexes;
                localFunction = condition.Value;
                for (k = 0; k < curBound.Length; k++)
                {
                    i = curBound[k];
                    curVert = vertices.ElementAt(i);
            
                    di[i] = 1;
                    globalB[i] = 0; //localFunction(curVert.X, curVert.Y);
            
                    for (s = ig[i]; s < ig[i + 1]; s++)
                    {
                        globalB[jg[s]] -= gg[s] * globalB[i];
                        gg[s] = 0;
                    }
                    
                    for (s = i + 1; s < vertexCount; s++)
                    {
                        for (j = ig[s]; j < ig[s + 1] && jg[j] <= i; j++)
                        {
                            if (jg[j] == i)
                            {
                                globalB[s] -= gg[j] * globalB[i];
                                gg[j] = 0;
                                break;
                            }
                        }
                    }
                }
            }

            SymmSparseMatrix A = new SymmSparseMatrix(vertexCount, ig, jg, di, gg);
            SoLESolver.SolveCGM(A, globalB, coeffs);
            q0.Copy(coeffs);
        }

        public int SolveNonLinear(Vector q0, double mu0, 
                                  int maxIterationCount, double solverEpsilon, double iterationEpsilon,
                                  double relaxation)
        {
            int[] ig, jg;
            double[] di, gg;

            GeneratePortrait(out di, out ig, out jg, out gg);

            int size = 4;
            double[,] localMatrix = new double[size, size]; // local matrix
            double[] localB = new double[size]; // local B

            double minX, maxX, hx;
            double minY, maxY, hy;
            double hyx, hxy;

            double dAdx, dAdy;
            double B;
            double inverseMagneticPermeability;

            double coeff;
            
            double relativeResidual = 0;
            double prevRelativeResidual;

            Material material;

            int[] numbers = new int[size]; // global numbers of element's vertexes
            int[] globalToLocalNumbers = new int[size];

            int i, j, k, s, m;
            Point curVert;

            int condSize = conditions.Length;
            int[] curBound;

            int elemCount = mesh.ElementCount;
            int vertexCount = mesh.VertexCount;

            IReadOnlyCollection<RectangleElement> elements = mesh.Elements;
            IReadOnlyCollection<Point> vertices = mesh.Vertices;
            RectangleElement element;

            SoLESolver.Epsilon = solverEpsilon;

            double sqrIterationEpsilon = iterationEpsilon * iterationEpsilon;
            SymmSparseMatrix A = new SymmSparseMatrix(vertexCount, ig, jg, di, gg);

            Vector prevCoeffs = new Vector(vertexCount);

            coeffs.Copy(q0);

            int iteration;
            for (iteration = 0; iteration < maxIterationCount; iteration++)
            {
                A.ClearValues();
                globalB.ClearValues();

                #region A(q_k), b(q_k)
                // element loop
                for (k = 0; k < elemCount; k++)
                {
                    element = mesh.Elements.ElementAt(k);
                    material = mesh.Materials[element.Element[size]];

                    Array.Copy(element.Element, numbers, size);
                    Array.Sort(numbers);

                    mesh.GetElementBounds(element, out minX, out maxX, out minY, out maxY);
                    hx = maxX - minX;
                    hy = maxY - minY;

                    int localNumber = 0;
                    i = 0;
                    foreach (Point currentVertex in numbers.Select(vertexIndex => vertices.ElementAt(vertexIndex)))
                    {
                        localNumber = currentVertex.Y == minY ? 0 : 2;
                        localNumber += currentVertex.X == minX ? 0 : 1;
                        globalToLocalNumbers[i] = localNumber;
                        i++;
                    }

                    if (material.RelativeMagneticPermeabilitySpline != null)
                    {
                        dAdx = coeffs[numbers[0]] * (-1) / hx * 0.5 +
                               coeffs[numbers[1]] * 1 / hx * 0.5 +
                               coeffs[numbers[2]] * (-1) / hx * 0.5 +
                               coeffs[numbers[3]] * 1 / hx * 0.5;

                        dAdy = coeffs[numbers[0]] * (-1) / hy * 0.5 +
                               coeffs[numbers[1]] * (-1) / hy * 0.5 +
                               coeffs[numbers[2]] * 1 / hy * 0.5 +
                               coeffs[numbers[3]] * 1 / hy * 0.5;

                        B = Math.Sqrt(dAdx * dAdx + dAdy * dAdy);

                        inverseMagneticPermeability = 1.0 / mu0 * CalculateInverseMu(material.RelativeMagneticPermeabilitySpline, B);
                    }
                    else inverseMagneticPermeability = 1.0 / (mu0 * material.RelativeMagneticPermeability); 

                    coeff = 1.0 / 6.0 * inverseMagneticPermeability;

                    hyx = hy / hx;
                    hxy = hx / hy;

                    // first row
                    localMatrix[0, 0] = coeff * (-2.0 * hyx + hxy); // 0 - 1
                    localMatrix[0, 1] = coeff * (hyx - 2.0 * hxy); // 0 - 2
                    localMatrix[0, 2] = coeff * (-hyx - hxy); // 0 - 3
                    localMatrix[0, 3] = coeff * (2.0 * hyx + 2.0 * hxy); // 0 - 0

                    // second row
                    localMatrix[1, 0] = localMatrix[0, 0]; // 1 - 0
                    localMatrix[1, 1] = coeff * (-hyx - hxy); // 1 - 2
                    localMatrix[1, 2] = coeff * (hyx - 2.0 * hxy); // 1 - 3
                    localMatrix[1, 3] = localMatrix[0, 3]; // 1 - 1

                    // third row
                    localMatrix[2, 0] = localMatrix[0, 1]; // 2 - 0
                    localMatrix[2, 1] = localMatrix[1, 1]; // 2 - 1
                    localMatrix[2, 2] = coeff * (-2.0 * hyx + hxy); // 2 - 3
                    localMatrix[2, 3] = localMatrix[0, 3]; // 2 - 2

                    // forth row
                    localMatrix[3, 0] = localMatrix[0, 2]; // 3 - 0
                    localMatrix[3, 1] = localMatrix[1, 2]; // 3 - 1
                    localMatrix[3, 2] = localMatrix[2, 2]; // 3 - 2
                    localMatrix[3, 3] = localMatrix[0, 3]; // 3 - 3

                    // local B
                    localB[0] = material.CurrentsDistributionDensity * 0.25 * hx * hy;
                    localB[1] = localB[0];
                    localB[2] = localB[0];
                    localB[3] = localB[0];

                    for (i = 0; i < size; i++)
                        di[numbers[i]] += localMatrix[globalToLocalNumbers[i], 3];

                    for (i = 0; i < size; i++)
                    {
                        m = ig[numbers[i]];
                        for (j = 0; j < i; j++)
                        {
                            for (s = m; s < ig[numbers[i] + 1]; s++)
                            {
                                if (jg[s] == numbers[j])
                                {
                                    gg[s] += localMatrix[globalToLocalNumbers[i], globalToLocalNumbers[j]];
                                    m++;
                                    break;
                                }
                            }
                        }
                    }

                    // adding local B to global B
                    for (i = 0; i < size; i++)
                        globalB[numbers[i]] += localB[globalToLocalNumbers[i]];
                }

                Func localFunction;
                // First type boundary conditions
                foreach (BoundaryCondition condition in conditions.Where(condition => condition.Type == BoundaryConditionType.First))
                {
                    curBound = condition.Vertexes;
                    localFunction = condition.Value;
                    for (k = 0; k < curBound.Length; k++)
                    {
                        i = curBound[k];
                        curVert = vertices.ElementAt(i);

                        di[i] = 1;
                        globalB[i] = 0; //localFunction(curVert.X, curVert.Y);

                        for (s = ig[i]; s < ig[i + 1]; s++)
                        {
                            globalB[jg[s]] -= gg[s] * globalB[i];
                            gg[s] = 0;
                        }
                        
                        for (s = i + 1; s < vertexCount; s++)
                        {
                            for (j = ig[s]; j < ig[s + 1] && jg[j] <= i; j++)
                            {
                                if (jg[j] == i)
                                {
                                    globalB[s] -= gg[j] * globalB[i];
                                    gg[j] = 0;
                                    break;
                                }
                            }
                        }
                    }
                }
                #endregion

                prevRelativeResidual = relativeResidual;
                relativeResidual = (A * coeffs - globalB).sqrMagnitude / globalB.sqrMagnitude;

                double optRel = relaxation;
                if (prevRelativeResidual != 0)
                {
                    double log = Math.Log10(relativeResidual / prevRelativeResidual + 1);
                    if (log <= 0)
                    {
                        optRel = Math.Min(0.5 + 0.1 * (Math.Abs(log) + 1), 0.9);
                    }
                    else
                    {
                        optRel = Math.Max(0.1, 0.5 - 0.1 * Math.Abs(log + 1));
                    }
                }

                Console.WriteLine($"iteration = {iteration + 1}. Relative residual = {relativeResidual.ToString("E6")}.");

                if (relativeResidual < sqrIterationEpsilon)
                    break;

                prevCoeffs.Copy(coeffs);
                // q_k+1
                int steps = SoLESolver.SolveCGM(A, globalB, coeffs);
                Console.WriteLine($"CGM steps = {steps}.\nCGM solution relative residual = {((A * coeffs - globalB).sqrMagnitude / globalB.sqrMagnitude).ToString("E6")}.\nRelaxation = {optRel.ToString("F6")}");
                coeffs = optRel * coeffs + (1 - optRel) * prevCoeffs;

                Console.WriteLine("\n");
            }

            return iteration + 1;
        }

        private double CalculateInverseMu(ISpline1D magneticPermeabilitySpline, double B)
        {
            if (B < magneticPermeabilitySpline.MinX)
                return 1.0 / magneticPermeabilitySpline.GetValue(magneticPermeabilitySpline.MinX);

            if(B > magneticPermeabilitySpline.MaxX)
            {
                return B / magneticPermeabilitySpline.MaxX * (1.0 / magneticPermeabilitySpline.GetValue(magneticPermeabilitySpline.MaxX) - 1.0) + 1.0; 
            }

            return 1.0 / magneticPermeabilitySpline.GetValue(B);
        }

        public double GetAzValue(Point p)
        {
            RectangleElement element = mesh.GetElementContainsPoint(p, 1E-9);
            if (element == null)
                return double.NaN;

            int[] numbers = new int[4];
            Array.Copy(element.Element, numbers, 4);
            Array.Sort(numbers);

            Point[] localVertices = numbers.Select(number => mesh.Vertices.ElementAt(number)).ToArray();

            mesh.GetElementBounds(element, out double minX, out double maxX, out double minY, out double maxY);
            double hx = maxX - minX;
            double hy = maxY - minY;

            int localNumber;
            int[] globalToLocalNumbers = new int[4];
            for (int i = 0; i < 4; i++)
            {
                localNumber = localVertices[i].Y == minY ? 0 : 2;
                localNumber += localVertices[i].X == minX ? 0 : 1;

                globalToLocalNumbers[i] = localNumber;
            }

            return GetValueOnElement(p.X, p.Y, numbers, globalToLocalNumbers, minX, maxX, minY, maxY);
        }

        public Point GetBValue(Point p)
        {
            RectangleElement element = mesh.GetElementContainsPoint(p, 1E-9);
            if (element == null)
                throw new ArithmeticException();

            int[] numbers = new int[4];
            Array.Copy(element.Element, numbers, 4);
            Array.Sort(numbers);

            Point[] localVertices = numbers.Select(number => mesh.Vertices.ElementAt(number)).ToArray();

            mesh.GetElementBounds(element, out double minX, out double maxX, out double minY, out double maxY);
            double hx = maxX - minX;
            double hy = maxY - minY;

            int localNumber;
            int[] globalToLocalNumbers = new int[4];
            for (int i = 0; i < 4; i++)
            {
                localNumber = localVertices[i].Y == minY ? 0 : 2;
                localNumber += localVertices[i].X == minX ? 0 : 1;

                globalToLocalNumbers[i] = localNumber;
            }

            double dx = 1E-8;
            double dy = 1E-8;

            double value = GetValueOnElement(p.X, p.Y, numbers, globalToLocalNumbers, minX, maxX, minY, maxY);
            double valueX, valueY;

            double dAdx, dAdy;

            if(p.X + dx <= maxX)
            {
                valueX = GetValueOnElement(p.X + dx, p.Y, numbers, globalToLocalNumbers, minX, maxX, minY, maxY);
                dAdx = (valueX - value) / dx;
            }
            else
            {
                valueX = GetValueOnElement(p.X - dx, p.Y, numbers, globalToLocalNumbers, minX, maxX, minY, maxY);
                dAdx = (value - valueX) / dx;
            }

            if(p.Y + dy <= maxY)
            {
                valueY = GetValueOnElement(p.X, p.Y + dy, numbers, globalToLocalNumbers, minX, maxX, minY, maxY);
                dAdy = (valueY - value) / dy;
            }
            else
            {
                valueY = GetValueOnElement(p.X, p.Y - dy, numbers, globalToLocalNumbers, minX, maxX, minY, maxY);
                dAdy = (value - valueY) / dy;
            }

            return new Point(dAdy, -dAdx);
        }

        private double GetValueOnElement(double x, double y, int[] numbers, int[] globalToLocalNumbers, double minX, double maxX, double minY, double maxY)
        {
            double hx = maxX - minX;
            double hy = maxY - minY;

            double coeff = 1.0 / (hx * hy);
            double[] localFunctionValues = new double[4]
            {
                coeff * (maxX - x) * (maxY - y),
                coeff * (x - minX) * (maxY - y),
                coeff * (maxX - x) * (y - minY),
                coeff * (x - minX) * (y - minY)
            };

            double value = 0;
            for(int i = 0; i < 4; i++)
            {
                value += coeffs[numbers[i]] * localFunctionValues[globalToLocalNumbers[i]];
            }

            return value;
        }
        private double GetModuleBOnElement(double x, double y, int[] numbers, int[] globalToLocalNumbers, double minX, double maxX, double minY, double maxY)
        {
            double dx = 1E-8;
            double dy = 1E-8;

            double value = GetValueOnElement(x, y, numbers, globalToLocalNumbers, minX, maxX, minY, maxY);
            double valueX, valueY;

            double dAdx, dAdy;

            if (x + dx <= maxX)
            {
                valueX = GetValueOnElement(x + dx, y, numbers, globalToLocalNumbers, minX, maxX, minY, maxY);
                dAdx = (valueX - value) / dx;
            }
            else
            {
                valueX = GetValueOnElement(x - dx, y, numbers, globalToLocalNumbers, minX, maxX, minY, maxY);
                dAdx = (value - valueX) / dx;
            }

            if (x + dy <= maxY)
            {
                valueY = GetValueOnElement(x, y + dy, numbers, globalToLocalNumbers, minX, maxX, minY, maxY);
                dAdy = (valueY - value) / dy;
            }
            else
            {
                valueY = GetValueOnElement(x, y - dy, numbers, globalToLocalNumbers, minX, maxX, minY, maxY);
                dAdy = (value - valueY) / dy;
            }

            return Math.Sqrt(dAdx * dAdx + dAdy * dAdy);
        }
    }
}