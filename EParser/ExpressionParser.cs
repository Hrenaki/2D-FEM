using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EParser
{
    public delegate double Func(params double[] t);
    public class ExpressionParser
    {
        string[] variables;

        public ExpressionParser(params string[] variables)
        {
            this.variables = variables.Select(t => "$" + t).ToArray();
        }
        public ExpressionParser()
        {
            variables = new[] { "$x", "$y" };
        }
        public Func GetFunction(string expression)
        {
            expression = prepareExpression(expression);
            return compileFunction(expression);
        }
        private string prepareExpression(string expression)
        {
            string basis;
            string degree;
            int position, lhs, rhs;

            expression = expression.ToLower().Replace(" ", "");

            for (int i = 0; i < variables.Length; i++)
                expression = expression.Replace(variables[i], "(t[" + i + "])");

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
                position = expression.IndexOf("^");
                lhs = position - 1;
                rhs = position + 1;
                basis = "";
                degree = "";
                if (expression[position - 1] == ')')
                {
                    while (expression[lhs] != '(')
                        lhs--;
                    basis += expression.Substring(lhs + 1, position - lhs - 2);
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

            return expression;
        }
        private Func compileFunction(string expression)
        {
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();
            parameters.GenerateInMemory = true;
            parameters.GenerateExecutable = false;
            parameters.ReferencedAssemblies.Add("System.dll");

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