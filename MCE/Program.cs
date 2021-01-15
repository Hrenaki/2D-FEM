using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MCE
{
    class Program
    {
        static void Main(string[] args)
        {
            Problem problem = Reader.ReadProblemFrom(
                Path.Combine(Environment.CurrentDirectory, "test9"));
            problem.Solve();
        }
    }
}
