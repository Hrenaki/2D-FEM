using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace UI
{
    class Shader : IDisposable
    {
        private int Handle;
        private bool disposedValue = false;

        public Shader(string vertexPath, string fragmentPath)
        {
            string VertexShaderSource;
            int VertexShader, FragmentShader;

            using (StreamReader reader = new StreamReader(vertexPath, Encoding.UTF8))
            {
                VertexShaderSource = reader.ReadToEnd();
            }

            string FragmentShaderSource;

            using (StreamReader reader = new StreamReader(fragmentPath, Encoding.UTF8))
            {
                FragmentShaderSource = reader.ReadToEnd();
            }

            VertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(VertexShader, VertexShaderSource);

            FragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(FragmentShader, FragmentShaderSource);

            GL.CompileShader(VertexShader);
            string infoLogFlag;
            if ((infoLogFlag = GL.GetShaderInfoLog(VertexShader)) != string.Empty)
                Console.WriteLine(infoLogFlag);

            GL.CompileShader(FragmentShader);
            if ((infoLogFlag = GL.GetShaderInfoLog(FragmentShader)) != string.Empty)
                Console.WriteLine(infoLogFlag);

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, VertexShader);
            GL.AttachShader(Handle, FragmentShader);

            GL.LinkProgram(Handle);

            GL.DetachShader(Handle, VertexShader);
            GL.DetachShader(Handle, FragmentShader);

            GL.DeleteShader(VertexShader);
            GL.DeleteShader(FragmentShader);
        }
        public void Use()
        {
            GL.UseProgram(Handle);           
        }
        public void SetMatrix(string name, Matrix4 matrix)
        {
            GL.UniformMatrix4(GL.GetUniformLocation(Handle, name), false, ref matrix);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                GL.DeleteProgram(Handle);
                disposedValue = true;
            }
        }
        ~Shader()
        {
            GL.DeleteProgram(Handle);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
