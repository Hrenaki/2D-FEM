﻿using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace MCE
{
    class Window : GameWindow
    {
        float[] vertexes;
        uint[] edges;
        uint[] elems;

        ColorMap cl;
        float[] colors;

        int VertexBufferObject;

        int ColorBuffer;
        int VertexArrayObjectTriangles;
        int ElementBufferObjectTriangles;

        int VertexArrayObjectEdges;       
        int ElementBufferObjectEdges;

        Shader shader;

        Matrix4 view, proj;
        float speed = 0.5f;
        float fov = 45.0f;
        Vector3 position = new Vector3(0.0f, 0.0f, 3.0f);
        Vector3 front = new Vector3(0.0f, 0.0f, -1.0f);
        Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);
        Vector3 right = new Vector3(1.0f, 0.0f, 0.0f);
        int viewUniformLoc, projUniformLoc;

        Mode mode = Mode.Edges;

        enum Mode
        {
            Edges,
            Faces
        }

        public Window(double[][] vertexes, int[][] elements, double[] values, int width, int height, string title) : base(width, height, GraphicsMode.Default, title)
        {
            cl = new ColorMap(values);

            double maxX = vertexes[0][0];
            double maxY = vertexes[0][1];

            double[] curVert;
            int i;

            for (i = 1; i < vertexes.GetLength(0); i++)
            {
                curVert = vertexes[i];
                if (curVert[0] > maxX)
                    maxX = curVert[0];
                if (curVert[1] > maxY)
                    maxY = curVert[1];
            }

            int pos = 0;
            this.vertexes = new float[vertexes.Length * 2];
            for(i = 0; i < vertexes.Length; i++)
            {
                this.vertexes[pos] = (float) (vertexes[i][0] / maxX);
                this.vertexes[pos + 1] = (float) (vertexes[i][1] / maxY);
                pos += 2;
            }

            pos = 0;
            elems = new uint[elements.Length * 3];

            int j, k, s;
            List<int> temp;
            List<int>[] adjacencyList = new List<int>[vertexes.Length];
            adjacencyList[0] = new List<int>();
            int[] numbers;

            for(i = 0; i < elements.Length; i++)
            {
                numbers = elements[i];
                Array.Sort(numbers);
                elems[pos] = (uint)numbers[0];
                elems[pos + 1] = (uint)numbers[1];
                elems[pos + 2] = (uint)numbers[2];
                pos += 3;

                for (j = 1; j < numbers.Length; j++)
                {
                    for (k = 0; k < j; k++)
                    {
                        if (adjacencyList[numbers[j]] == null)
                        {
                            adjacencyList[numbers[j]] = new List<int>();
                            adjacencyList[numbers[j]].Add(numbers[k]);
                        }
                        else
                        {    
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

            edges = new uint[(vertexes.Length + elements.Length - 1) * 2];
            pos = 0;
            for(i = 0; i < adjacencyList.Length; i++)
                foreach(int v in adjacencyList[i])
                {
                    edges[pos] = (uint)i;
                    edges[pos + 1] = (uint)v;
                    pos += 2;
                }

            colors = cl.ColorsToArray();            
        }
        public Window(int width, int height, string title) : base(width, height, GraphicsMode.Default, title)
        { }
        protected override void OnLoad(EventArgs e)
        {
            GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);

            VertexBufferObject = GL.GenBuffer();

            VertexArrayObjectTriangles = GL.GenVertexArray();
            ColorBuffer = GL.GenBuffer();
            ElementBufferObjectTriangles = GL.GenBuffer();

            VertexArrayObjectEdges = GL.GenVertexArray();            
            ElementBufferObjectEdges = GL.GenBuffer();

            #region VAO for triangles
            GL.BindVertexArray(VertexArrayObjectTriangles);

            // vertexes
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertexes.Length * sizeof(float), vertexes, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // colors
            GL.BindBuffer(BufferTarget.ArrayBuffer, ColorBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, colors.Length * sizeof(float), colors, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(1);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObjectTriangles);
            GL.BufferData(BufferTarget.ElementArrayBuffer, elems.Length * sizeof(uint),
                elems, BufferUsageHint.StaticDraw);
            #endregion

            // Drops ColorBuffer and sets VBO
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);

            #region VAO for lines
            GL.BindVertexArray(VertexArrayObjectEdges);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObjectEdges);
            GL.BufferData(BufferTarget.ElementArrayBuffer, edges.Length * sizeof(uint),
                edges, BufferUsageHint.StaticDraw);
            
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            #endregion

            // Drops all buffers
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.Enable(EnableCap.DepthTest);

            shader = new Shader("shader.vert", "shader.frag");

            // Default view and proj matrixes
            view = Matrix4.LookAt(position, position + front, up);
            proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45.0f), Width / Height,
                0.01f, 100.0f);

            base.OnLoad(e);
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shader.Use();
            viewUniformLoc = GL.GetUniformLocation(shader.Handle, "view");
            projUniformLoc = GL.GetUniformLocation(shader.Handle, "proj");

            view = Matrix4.LookAt(position, position + front, up);
            proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(fov), Width / Height,
                0.01f, 100.0f);
            GL.UniformMatrix4(viewUniformLoc, false, ref view);
            GL.UniformMatrix4(projUniformLoc, false, ref proj);

            switch (mode)
            {
                case Mode.Edges:
                    GL.BindVertexArray(VertexArrayObjectEdges);                    
                    GL.DrawElements(PrimitiveType.Lines, edges.Length, DrawElementsType.UnsignedInt, 0);
                    GL.BindVertexArray(0);
                    break;
                case Mode.Faces:
                    GL.BindVertexArray(VertexArrayObjectTriangles);
                    GL.DrawElements(PrimitiveType.Triangles, elems.Length, DrawElementsType.UnsignedInt, 0);
                    GL.BindVertexArray(0);
                    break;
            }

            SwapBuffers();
            base.OnRenderFrame(e);
        }
        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.E)
                mode = mode == Mode.Edges ? Mode.Faces : Mode.Edges;

            if (!Focused)
                return;

            // Camera moving
            if(e.Key == Key.Space)
                position += speed * front;
            if (e.Key == Key.LShift)
                position -= speed * front;
            if (e.Key == Key.D)
                position += speed * right;
            if (e.Key == Key.A)
                position -= speed * right;
            if (e.Key == Key.W)
                position += speed * up;
            if (e.Key == Key.S)
                position -= speed * up;

            base.OnKeyDown(e);
        }
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            float x = (e.X / (float)Width - 0.5f) / 5f;
            float y = (-e.Y / (float)Height + 0.5f) / 5f;
            
            // Zoom
            if (e.Delta > 0)
            {
                position += speed * front;
                position.X += x;
                position.Y += y;
            }
            else
            {
                position -= speed * front;
                position.X -= x;
                position.Y -= y;
            }

            base.OnMouseWheel(e);
        }
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (Keyboard.GetState().IsKeyDown(Key.Escape))
                Exit();
            base.OnUpdateFrame(e);
        }
        protected override void OnUnload(EventArgs e)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(VertexBufferObject);
            shader.Dispose();

            base.OnUnload(e);
        }
    }
}
