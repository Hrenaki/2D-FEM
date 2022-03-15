using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;

using NumMath.Splines;

namespace MCE.Problems.Magnetic
{
    public class Material
    {
        public double CurrentsDistributionDensity { get; set; }
        public double RelativeMagneticPermeability { get; set; }
        public ISpline1D RelativeMagneticPermeabilitySpline { get; set; }

        public string ToString(string format)
        {
            return string.Join(" ", RelativeMagneticPermeability.ToString(format), CurrentsDistributionDensity.ToString(format));
        }
    }

    class RectangleArea
    {
        public double MinX { get; set; }
        public double MaxX { get; set; }

        public double MinY { get; set; }
        public double MaxY { get; set; }

        public Material Material { get; set; }

        public bool IsInside(Point p1, Point p2, Point p3, Point p4, double eps)
        {
            return IsInside(p1, eps) && IsInside(p2, eps) && IsInside(p3, eps) && IsInside(p4, eps);
        }

        public bool IsInside(Point p, double eps)
        {
            if (MinX <= p.X && p.X <= MaxX &&
                MinY <= p.Y && p.Y <= MaxY)
                return true;

            if (Math.Abs(p.X - MinX) > eps && Math.Abs(p.X - MaxX) > eps)
                return false;

            if (Math.Abs(p.Y - MinY) > eps && Math.Abs(p.Y - MaxY) > eps)
                return false;

            return true;
        }
    }

    struct Segment
    {
        public double Start;
        public double End;

        public double Step;
        public double DischargeRate;
        public int DischargeSign;
    }

    public struct Point
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public string ToString(string format)
        {
            return string.Format("{0} {1}", X.ToString(format), Y.ToString(format));
        }
    }

    public class RectangleElement
    {
        public int[] Element { get; } = new int[5];

        public int Vertex1 { get => Element[0]; set => Element[0] = value; }
        public int Vertex2 { get => Element[1]; set => Element[1] = value; }
        public int Vertex3 { get => Element[2]; set => Element[2] = value; }
        public int Vertex4 { get => Element[3]; set => Element[3] = value; }

        public int Material { get => Element[4]; set => Element[4] = value; }

        public override string ToString()
        {
            return string.Join(" ", Element);
        }
    }

    public static class RectangleMeshReader
    {
        public static RectangleMesh ReadFromSreda(string filename, out BoundaryCondition[] conditions)
        {
            RectangleArea[] areas;
            Dictionary<int, Material> materials = new Dictionary<int, Material>();

            Segment[] segmentsX;
            Segment[] segmentsY;

            using (StreamReader reader = new StreamReader(filename))
            {
                string[] separator = new string[] { " " };

                int areaCount = int.Parse(reader.ReadLine());
                areas = new RectangleArea[areaCount];

                for (int i = 0; i < areaCount; i++)
                {
                    string[] splittedString = reader.ReadLine().Split(separator, StringSplitOptions.RemoveEmptyEntries);

                    int materialNumber = int.Parse(splittedString.Last());
                    Material currentMaterial;
                    if (materials.ContainsKey(materialNumber))
                        currentMaterial = materials[materialNumber];
                    else
                    {
                        currentMaterial = new Material()
                        {
                            RelativeMagneticPermeability = double.Parse(splittedString[splittedString.Length - 3]),
                            CurrentsDistributionDensity = double.Parse(splittedString[splittedString.Length - 2])
                        };

                        materials.Add(materialNumber, currentMaterial);
                    }

                    areas[i] = new RectangleArea()
                    {
                        MinX = double.Parse(splittedString[0]),
                        MaxX = double.Parse(splittedString[1]),
                        MinY = double.Parse(splittedString[2]),
                        MaxY = double.Parse(splittedString[3]),
                        Material = currentMaterial
                    };
                }
                reader.ReadLine();

                Segment[] ReadSegments()
                {
                    string[] segmentsInfo = reader.ReadLine().Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    int segmentCount = int.Parse(segmentsInfo[1]);

                    double[] points = reader.ReadLine().Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(point => double.Parse(point)).ToArray();
                    double[] steps = reader.ReadLine().Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(step => double.Parse(step)).ToArray();
                    double[] dischargeRates = reader.ReadLine().Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(rate => double.Parse(rate)).ToArray();
                    int[] dischargeSigns = reader.ReadLine().Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(sign => int.Parse(sign)).ToArray();

                    Segment[] segments = new Segment[segmentCount];
                    segments[0] = new Segment()
                    {
                        Start = double.Parse(segmentsInfo[0]),
                        End = points[0],
                        Step = steps[0],
                        DischargeRate = dischargeRates[0],
                        DischargeSign = dischargeSigns[0]
                    };
                    for (int i = 1; i < segmentCount; i++)
                    {
                        segments[i] = new Segment()
                        {
                            Start = points[i - 1],
                            End = points[i],
                            Step = steps[i],
                            DischargeRate = dischargeRates[i],
                            DischargeSign = dischargeSigns[i]
                        };
                    }

                    return segments;
                }

                segmentsX = ReadSegments();

                reader.ReadLine();

                segmentsY = ReadSegments();

                reader.ReadLine();

                int[] doubleInfo = reader.ReadLine().Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(flag => int.Parse(flag)).ToArray();
            }

            List<double> GeneratePoints(Segment[] segments)
            {
                List<double> points = new List<double>();

                foreach (Segment segment in segments)
                {
                    points.Add(segment.Start);
                    double step = segment.Step;
                    double currentPoint;
                    double currentStep;
                    double prevStep;
                    double dischargeRate = segment.DischargeRate;

                    if (dischargeRate != 1)
                    {
                        int n = 0;

                        if (segment.DischargeSign != 1)
                        {
                            currentPoint = segment.End - step;
                            while (currentPoint > segment.Start && Math.Abs(currentPoint - segment.Start) > 1E-9)
                            {
                                step *= dischargeRate;
                                currentPoint -= step;
                                n++;
                            }

                            step = segment.Step * (segment.End - segment.Start) / (segment.End - currentPoint);
                            currentStep = step;
                            for (int i = 0; i < n; i++)
                                currentStep *= dischargeRate;

                            double length = segment.End - segment.Start;
                            currentPoint = segment.End - (step - currentStep) / (1 - dischargeRate);

                            for(int i = 0; i < n; i++)
                            {
                                points.Add(currentPoint);

                                prevStep = currentStep;
                                currentStep = step;
                                for (int k = 0; k < n - i - 1; k++)
                                    currentStep *= dischargeRate;
                                currentPoint = segment.End - (step - currentStep) / (1 - dischargeRate);
                            }
                        }
                        else
                        {
                            currentPoint = segment.Start + step;
                            while (currentPoint < segment.End && Math.Abs(currentPoint - segment.End) > 1E-9)
                            {
                                step *= dischargeRate;
                                currentPoint += step;
                                n++;
                            }
                            step = segment.Step * (segment.End - segment.Start) / (currentPoint - segment.Start);

                            currentStep = step * dischargeRate;
                            currentPoint = segment.Start + step;
                            for(int i = 0; i < n; i++)
                            {
                                points.Add(currentPoint);

                                currentStep *= dischargeRate;
                                currentPoint = segment.Start + (step - currentStep) / (1 - dischargeRate);
                            }
                        }
                    }
                    else
                    {
                        double actualIntervalCount = (segment.End - segment.Start) / step;
                        int intervalCount = (int) Math.Floor(actualIntervalCount);
                        if(intervalCount > 1 && Math.Abs(intervalCount - actualIntervalCount) < 1E-9)
                        {
                            for(int i = 1; i < intervalCount; i++)
                            {
                                points.Add(segment.Start + i * step);

                            }
                        }
                    }
                }
                points.Add(segments.Last().End);

                return points;
            }

            List<double> pointsX = GeneratePoints(segmentsX);
            List<double> pointsY = GeneratePoints(segmentsY);

            OutputPoints(pointsX, Path.Combine(Directory.GetParent(filename).FullName, "x.txt"));
            OutputPoints(pointsY, Path.Combine(Directory.GetParent(filename).FullName, "y.txt"));

            List<RectangleElement> elements = new List<RectangleElement>();
            List<Point> vertices = new List<Point>();

            int pointsXCount = pointsX.Count;
            int pointsYCount = pointsY.Count;

            for (int j = 0; j < pointsYCount; j++)
            {
                for(int i = 0; i < pointsXCount; i++)
                {
                    vertices.Add(new Point(pointsX[i], pointsY[j]));
                }
            }

            for (int j = 0; j < pointsYCount - 1; j++)
            {
                for(int i = 0; i < pointsXCount - 1; i++)
                {
                    var rect = new RectangleElement()
                    {
                        Vertex1 = j * pointsXCount + i,
                        Vertex2 = j * pointsXCount + i + 1,
                        Vertex3 = (j + 1) * pointsXCount + i + 1,
                        Vertex4 = (j + 1) * pointsXCount + i
                    };

                    int material = -1;
                    foreach (RectangleArea area in areas.Reverse())
                    {
                        if (area.IsInside(vertices[rect.Vertex1], vertices[rect.Vertex2], vertices[rect.Vertex3], vertices[rect.Vertex4], 1E-9))
                        {
                            material = materials.First(pair => pair.Value == area.Material).Key;
                            break;
                        }
                    }

                    if (material < 0)
                    {
                        throw new ArgumentException("Can't find material for element.");
                    }

                    rect.Material = material;
                    elements.Add(rect);
                }
            }

            conditions = new BoundaryCondition[2];

            List<int> firstBoundaryConditionIndices = new List<int>();
            for(int i = 0; i < pointsYCount - 1; i++)
            {
                firstBoundaryConditionIndices.Add(i * pointsXCount);
            }
            for(int i = 0; i < pointsXCount - 1; i++)
            {
                firstBoundaryConditionIndices.Add((pointsYCount - 1) * pointsXCount + i);
            }
            for(int i = pointsYCount; i >= 1; i--)
            {
                firstBoundaryConditionIndices.Add(i * pointsXCount - 1);
            }
            conditions[0] = new BoundaryCondition(BoundaryConditionType.First, t => 0, firstBoundaryConditionIndices.ToArray());

            conditions[1] = new BoundaryCondition(BoundaryConditionType.Second, t => 0, Enumerable.Range(0, pointsXCount).ToArray());

            return new RectangleMesh(vertices, elements, materials);
        }

        private static void OutputPoints(List<double> points, string filename)
        {
            using(StreamWriter sw = new StreamWriter(filename))
            {
                foreach(double point in points)
                {
                    sw.WriteLine(point);
                }
            }
        }
    }

    public static class RectangleMeshWriter
    {
        public static void WriteSplitted(RectangleMesh mesh, string vertexFile, string elementFile, string materialFile)
        {
            using (StreamWriter sw = new StreamWriter(vertexFile))
            {
                sw.WriteLine(mesh.VertexCount);

                foreach(Point vertex in mesh.Vertices)
                {
                    sw.WriteLine(vertex.ToString("f4"));
                }
            }

            using (StreamWriter sw = new StreamWriter(elementFile))
            {
                sw.WriteLine(mesh.ElementCount);

                foreach(RectangleElement element in mesh.Elements)
                {
                    sw.WriteLine(element.ToString());
                }
            }

            using (StreamWriter sw = new StreamWriter(materialFile))
            {
                IReadOnlyDictionary<int, Material> materials = mesh.Materials;
                sw.WriteLine(materials.Count);

                foreach(KeyValuePair<int, Material> material in materials)
                {
                    sw.WriteLine(material.Key + " " + material.Value.ToString("E1"));
                }
            }
        }
    }

    public class RectangleMesh
    {
        private List<Point> vertices;
        public IReadOnlyCollection<Point> Vertices => vertices.AsReadOnly();
        public int VertexCount => vertices.Count;

        private List<RectangleElement> elements;
        public IReadOnlyCollection<RectangleElement> Elements => elements.AsReadOnly();
        public int ElementCount => elements.Count;

        private Dictionary<int, Material> materials;
        public IReadOnlyDictionary<int, Material> Materials => new ReadOnlyDictionary<int, Material>(materials);

        public RectangleMesh(List<Point> vertices, List<RectangleElement> elements, Dictionary<int, Material> materials)
        {
            this.vertices = vertices;
            this.elements = elements;
            this.materials = materials;
        }

        public RectangleElement GetElementContainsPoint(Point p, double eps)
        {
            int[] vertexIndices = new int[4];

            foreach(RectangleElement element in elements)
            {
                double minX = double.MaxValue, maxX = double.MinValue;
                double minY = double.MaxValue, maxY = double.MinValue;

                vertexIndices[0] = element.Vertex1;
                vertexIndices[1] = element.Vertex2;
                vertexIndices[2] = element.Vertex3;
                vertexIndices[3] = element.Vertex4;

                foreach(Point vertex in vertexIndices.Select(index => vertices[index]))
                {
                    if(vertex.X > maxX)
                        maxX = vertex.X;
                    if(vertex.X < minX)
                        minX = vertex.X;

                    if(vertex.Y > maxY)
                        maxY = vertex.Y;
                    if(vertex.Y < minY)
                        minY = vertex.Y;
                }

                if (minX <= p.X && p.X <= maxX &&
                    minY <= p.Y && p.Y <= maxY)
                    return element;

                if ((Math.Abs(p.X - minX) < eps || Math.Abs(p.X - maxX) < eps) &&
                    (Math.Abs(p.Y - minY) < eps || Math.Abs(p.Y - maxY) < eps))
                    return element;
            }

            return null;
        }

        public void GetElementBounds(RectangleElement element, out double minX, out double maxX, out double minY, out double maxY)
        {
            //if (elements.Exists(elem => ))
            //    throw new ArgumentException($"There isn't such element.");

            minX = double.MaxValue; 
            maxX = double.MinValue;

            minY = double.MaxValue; 
            maxY = double.MinValue;

            foreach (Point vertex in element.Element.Take(4).Select(vertexIndex => vertices[vertexIndex]))
            {
                if (vertex.X > maxX)
                    maxX = vertex.X;
                if (vertex.X < minX)
                    minX = vertex.X;

                if (vertex.Y > maxY)
                    maxY = vertex.Y;
                if (vertex.Y < minY)
                    minY = vertex.Y;
            }
        }
    }
}
