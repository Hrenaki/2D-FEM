using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleMeshTester
{
    public class Result
    {
        public bool IsSuccessful { get; set; }
        public string Message { get; set; }
        public Result InnerResult { get; set; }

        public static Result OK => new Result() { IsSuccessful = true, Message = "OK" };

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
