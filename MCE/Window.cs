using System;
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

        Mesh2D mesh2D;
        float[] meshVertexes;
        float meshHeight, meshWidth;
        float meshStepX = 0.25f;
        float meshStepY = 0.25f;

        int VBO;
        int VBOMesh;

        int ColorBuffer;
        int VAOTriangles;
        int EBOTriangles;

        int VAOEdges;       
        int EBOEdges;

        int VAOMesh;

        Shader shader;

        Matrix4 view, proj;
        float speed = 0.5f;
        Vector3 position = new Vector3(0.0f, 0.0f, 3f);
        Vector3 front = new Vector3(0.0f, 0.0f, -1.0f);
        Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);
        Vector3 right = new Vector3(1.0f, 0.0f, 0.0f);

        Mode mode = Mode.Edges;

        float mouseX, mouseY;

        enum Mode
        {
            Edges,
            Faces
        }

        public Window(double[][] vertexes, int[][] elements, double[] values, int width, int height, string title) : base(width, height, GraphicsMode.Default, title)
        {
            cl = new ColorMap(values);
            mesh2D = new Mesh2D();

            double maxX = vertexes[0][0];
            double maxY = vertexes[0][1];
            double minX = maxX;
            double minY = maxY;

            double[] curVert;
            int i;

            for (i = 1; i < vertexes.GetLength(0); i++)
            {
                curVert = vertexes[i];
                if (curVert[0] > maxX)
                    maxX = curVert[0];
                if (curVert[1] > maxY)
                    maxY = curVert[1];
                if (curVert[0] < minX)
                    minX = curVert[0];
                if (curVert[1] < minY)
                    minY = curVert[1];
            }

            this.vertexes = new float[vertexes.Length * 2];
            for(i = 0; i < vertexes.Length; i++)
            {
                this.vertexes[2 * i] = (float) (vertexes[i][0]);
                this.vertexes[2 * i + 1] = (float) (vertexes[i][1]);
            }

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
                elems[3 * i] = (uint)numbers[0];
                elems[3 * i + 1] = (uint)numbers[1];
                elems[3 * i + 2] = (uint)numbers[2];

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
            int pos = 0;
            for(i = 0; i < adjacencyList.Length; i++)
                foreach(int v in adjacencyList[i])
                {
                    edges[pos] = (uint)i;
                    edges[pos + 1] = (uint)v;
                    pos += 2;
                }

            colors = cl.ToColorBuffer();            
        }
        public Window(int width, int height, string title) 
            : base(width, height, GraphicsMode.Default, title)
        { }
        protected override void OnLoad(EventArgs e)
        {
            GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);

            VBO = GL.GenBuffer();
            VBOMesh = GL.GenBuffer();

            VAOTriangles = GL.GenVertexArray();
            ColorBuffer = GL.GenBuffer();
            EBOTriangles = GL.GenBuffer();

            VAOEdges = GL.GenVertexArray();            
            EBOEdges = GL.GenBuffer();

            VAOMesh = GL.GenVertexArray();

            #region VAO for triangles
            GL.BindVertexArray(VAOTriangles);

            // vertexes
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertexes.Length * sizeof(float), 
                vertexes, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 
                2 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // colors
            GL.BindBuffer(BufferTarget.ArrayBuffer, ColorBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, colors.Length * sizeof(float), 
                colors, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(1);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBOTriangles);
            GL.BufferData(BufferTarget.ElementArrayBuffer, elems.Length * sizeof(uint),
                elems, BufferUsageHint.StaticDraw);
            #endregion

            // Drops ColorBuffer and sets VBO
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

            #region VAO for Edges
            GL.BindVertexArray(VAOEdges);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBOEdges);
            GL.BufferData(BufferTarget.ElementArrayBuffer, edges.Length * sizeof(uint),
                edges, BufferUsageHint.StaticDraw);
            
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 
                2 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            #endregion

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            #region VAO for Mesh
            GL.BindVertexArray(VAOMesh);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOMesh);
            GL.BufferData(BufferTarget.ArrayBuffer, 0,
                new float[] { }, BufferUsageHint.StreamDraw);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float,
                false, 2 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            #endregion

            // Drops all buffers
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.Enable(EnableCap.DepthTest);

            shader = new Shader("shader.vert", "shader.frag");

            // Default view and proj matrix
            view = Matrix4.LookAt(position, position + front, up);
            proj = Matrix4.CreateOrthographic(Math.Abs(position.Z), Math.Abs(position.Z) * Height / Width, 0.1f, 100.0f);

            base.OnLoad(e);
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shader.Use();
            meshWidth = Math.Abs(position.Z);
            meshHeight = meshWidth * Height / Width;

            view = Matrix4.LookAt(position, position + front, up);
            proj = Matrix4.CreateOrthographic(meshWidth, 
                meshHeight, 0.1f, 100.0f);

            shader.SetMatrix("view", view);
            shader.SetMatrix("proj", proj);

            switch (mode)
            {
                case Mode.Edges:
                    GL.BindVertexArray(VAOEdges);
                    GL.LineWidth(3);
                    GL.DrawElements(PrimitiveType.Lines, edges.Length, DrawElementsType.UnsignedInt, 0);
                    GL.LineWidth(1);
                    GL.BindVertexArray(0);
                    break;
                case Mode.Faces:
                    GL.BindVertexArray(VAOTriangles);
                    GL.DrawElements(PrimitiveType.Triangles, elems.Length, DrawElementsType.UnsignedInt, 0);
                    GL.BindVertexArray(0);
                    break;
            }
            
            mesh2D.BuildMesh(position.X - meshWidth / 2.0, position.X + meshWidth / 2.0,
                position.Y - meshHeight / 2.0, position.Y + meshHeight / 2.0, 
                meshStepX, meshStepY);
            
            meshVertexes = mesh2D.ToVertexBuffer();
            
            GL.BindVertexArray(VAOMesh);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOMesh);
            GL.BufferData(BufferTarget.ArrayBuffer, meshVertexes.Length * sizeof(float),
                meshVertexes, BufferUsageHint.StreamDraw);
            GL.DrawArrays(PrimitiveType.Lines, 0, mesh2D.LinesCount);
            GL.BindVertexArray(0);

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
        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            //if(e.Mouse.IsButtonDown(MouseButton.Left))
            //{
            //    position.X -= e.XDelta / (float)Width * 1.5f;
            //    position.Y += e.YDelta / (float)Height * 1.5f;
            //}
            base.OnMouseMove(e);
        }
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left && mode == Mode.Edges)
            {
                mouseX = 2.0f * e.X / (float)Width - 1.0f;
                mouseY = 2.0f * -e.Y / (float)Height + 1.0f;

                Vector4 t = (proj * view).Inverted() * new Vector4(mouseX, mouseY, 0, 1.0f);
                Console.WriteLine((position.X + t.X) + " " + (position.Y + t.Y));
            }
            base.OnMouseDown(e);
        }
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (Keyboard.GetState().IsKeyDown(Key.Escape))
                Exit();
            base.OnUpdateFrame(e);
        }
        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
            base.OnResize(e);
        }
        protected override void OnUnload(EventArgs e)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(VBO);
            GL.DeleteBuffer(VBOMesh);
            shader.Dispose();

            base.OnUnload(e);
        }
    }
}
