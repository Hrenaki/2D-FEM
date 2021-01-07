using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Reflection;

namespace MCE
{
    public delegate double Func(params double[] t);
    public class ExpressionParser
    {
        string[] variables;

        public ExpressionParser(params string[] variables)
        {
            this.variables = variables;
        }
        public ExpressionParser()
        {
            variables = new[] { "x", "y" };
        }
        public Func getDelegate(string expression)
        {
            expression = expression.ToLower().Replace(" ", "");

            expression = expression.Replace("sin", "Math.Sin")
                                    .Replace("cos", "Math.Cos")
                                    .Replace("tg", "Math.Tan")
                                    .Replace("e^", "Math.Exp")
                                    .Replace("ln", "Math.Log")
                                    .Replace("lg", "Math.Log10")
                                    .Replace("abs", "Math.Abs")
                                    .Replace("sqrt", "Math.Sqrt")
                                    .Replace("sign", "Math.Sign")
                                    .Replace("e", "Math.E")
                                    .Replace("pi", "Math.PI");

            while (expression.Contains("^"))
            {
                int position = expression.IndexOf("^");
                int lhs = position - 1, rhs = position + 1;
                string basis = "";
                string degree = "";
                if (expression[position - 1] == ')')
                {
                    while (expression[lhs] != '(')
                        lhs--;
                    basis += expression.Substring(lhs + 1, position - 2);
                }
                else basis += expression[lhs];

                if (expression[position + 1] == '(')
                {
                    while (expression[rhs] != ')')
                        rhs++;
                    degree += expression.Substring(position + 2, rhs - 1 - position - 1);
                }
                else degree += expression[rhs];

                expression = expression.Replace((lhs == position - 1 ? basis : "(" + basis + ")") + "^" +
                    (rhs == position + 1 ? degree : "(" + degree + ")"),
                    "Math.Pow(" + basis + ", " + degree + ")");
            }

            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();
            parameters.GenerateInMemory = true;
            parameters.GenerateExecutable = false;
            parameters.ReferencedAssemblies.Add("System.dll");

            for(int i = 0; i < variables.Length; i++)
                expression = expression.Replace(variables[i], "t[" + i + "]");

            CompilerResults results = codeProvider.CompileAssemblyFromSource(parameters,
            @"
            using System;
            class Program
            {
                public static double func(params double[] t) 
                {
                    return " + expression + @";
                }
            }");

            MethodInfo res = results.CompiledAssembly.GetType("Program").GetMethod("func");
            return (Func)res.CreateDelegate(typeof(Func));
        }
    }
}
