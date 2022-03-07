using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TriangleMeshTester;

namespace UI
{
    class Program
    {
        static void Main(string[] args)
        {
            TriangleMeshTest.LoadMeshAndRun("mesh.txt", 1, 1);

            Console.ReadLine();
        }
    }
}
