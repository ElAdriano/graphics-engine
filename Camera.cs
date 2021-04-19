using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using System.Windows.Input;

namespace VirtualCamera
{
    public class Camera : IDisposable
    {
        private RenderForm renderForm;

        public const int Width = 640;
        public const int Height = 480;

        public WindowRenderTarget CameraView;
        public Vector3 Position;
        public Vector3 Target;
        public float Zoom;

        private Matrix XAxisRotationMatrix;
        private Matrix YAxisRotationMatrix;
        private Matrix ZAxisRotationMatrix;

        public byte[] SceneCache;
        public SharpDX.Direct2D1.Bitmap SceneBuffer;
        private List<Object3D> objects;

        public Camera()
        {
            renderForm = new RenderForm("VirtualCamera");
            renderForm.ClientSize = new Size(Width, Height);
            renderForm.AllowUserResizing = false;
            Position = new Vector3(0, 0, 0);
            Target = new Vector3(0, 0, 9f);

            SceneCache = new byte[Width * Height * 4];
            Zoom = 1;

            RenderTargetProperties renderTargetProperties = new RenderTargetProperties()
            {
                Type = SharpDX.Direct2D1.RenderTargetType.Hardware,
                PixelFormat = new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.R8G8B8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Ignore),
                DpiX = 0,
                DpiY = 0,
                Usage = SharpDX.Direct2D1.RenderTargetUsage.None,
                MinLevel = SharpDX.Direct2D1.FeatureLevel.Level_10
            };
            var hwndProperties = new SharpDX.Direct2D1.HwndRenderTargetProperties()
            {
                Hwnd = renderForm.Handle,
                PixelSize = new SharpDX.Size2(Width, Height),
                PresentOptions = SharpDX.Direct2D1.PresentOptions.Immediately
            };

            CameraView = new WindowRenderTarget(new Factory(), renderTargetProperties, hwndProperties);
            SceneBuffer = new SharpDX.Direct2D1.Bitmap(CameraView, new SharpDX.Size2(Width, Height), new SharpDX.Direct2D1.BitmapProperties(CameraView.PixelFormat));

            objects = new List<Object3D>();
        }

        public void AddObject(Object3D obj)
        {
            this.objects.Add(obj);
        }

        private void ClearScene(byte r = 0, byte g = 0, byte b = 0, byte a = 255)
        {
            for(int i = 0; i < Width * Height; i++)
            {
                SceneCache[4 * i] = r;
                SceneCache[4 * i + 1] = g;
                SceneCache[4 * i + 2] = b;
                SceneCache[4 * i + 3] = a;
            }
        }

        private void Draw()
        {
            Converter.Render(objects, this);

            SceneBuffer.CopyFromMemory(SceneCache, 4 * Width);
            CameraView.BeginDraw();
            CameraView.Clear(new Color4(0, 0, 0, 1f));
            CameraView.DrawBitmap(SceneBuffer, 1f, BitmapInterpolationMode.Linear);
            CameraView.EndDraw();
        }

        public void Run()
        {
            RenderLoop.Run(renderForm, RenderCallback);
        }

        private void CheckInput()
        {
            if (Keyboard.IsKeyDown(Key.Up))
            {
                foreach(var obj in objects)
                {
                    obj.Position = new Vector3(obj.Position.X, obj.Position.Y - 0.05f, obj.Position.Z);
                    for(int i = 0; i < obj.Walls.Count; i++)
                    {
                        for (int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                        {
                            obj.Walls[i].Vertices[j] = new Vector3(obj.Walls[i].Vertices[j].X, obj.Walls[i].Vertices[j].Y - 0.05f, obj.Walls[i].Vertices[j].Z);
                        }
                        obj.Walls[i].FindPlanes();
                    }
                }
            }

            if (Keyboard.IsKeyDown(Key.Down))
            {
                foreach (var obj in objects)
                {
                    obj.Position = new Vector3(obj.Position.X, obj.Position.Y + 0.05f, obj.Position.Z);
                    for (int i = 0; i < obj.Walls.Count; i++)
                    {
                        for (int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                        {
                            obj.Walls[i].Vertices[j] = new Vector3(obj.Walls[i].Vertices[j].X, obj.Walls[i].Vertices[j].Y + 0.05f, obj.Walls[i].Vertices[j].Z);
                        }
                        obj.Walls[i].FindPlanes();
                    }
                }
            }

            if (Keyboard.IsKeyDown(Key.Left))
            {
                foreach (var obj in objects)
                {
                    obj.Position = new Vector3(obj.Position.X + 0.05f, obj.Position.Y, obj.Position.Z);
                    for (int i = 0; i < obj.Walls.Count; i++)
                    {
                        for (int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                        {
                            obj.Walls[i].Vertices[j] = new Vector3(obj.Walls[i].Vertices[j].X + 0.05f, obj.Walls[i].Vertices[j].Y, obj.Walls[i].Vertices[j].Z);
                        }
                        obj.Walls[i].FindPlanes();
                    }
                }
            }

            if (Keyboard.IsKeyDown(Key.Right))
            {
                foreach (var obj in objects)
                {
                    obj.Position = new Vector3(obj.Position.X - 0.05f, obj.Position.Y, obj.Position.Z);
                    for (int i = 0; i < obj.Walls.Count; i++)
                    {
                        for (int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                        {
                            obj.Walls[i].Vertices[j] = new Vector3(obj.Walls[i].Vertices[j].X - 0.05f, obj.Walls[i].Vertices[j].Y, obj.Walls[i].Vertices[j].Z);
                        }
                        obj.Walls[i].FindPlanes();
                    }
                }
            } 

            if (Keyboard.IsKeyDown(Key.W))
            {
                foreach (var obj in objects)
                {
                    obj.Position = new Vector3(obj.Position.X, obj.Position.Y, obj.Position.Z + 0.05f);
                    for (int i = 0; i < obj.Walls.Count; i++)
                    {
                        for (int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                        {
                            obj.Walls[i].Vertices[j] = new Vector3(obj.Walls[i].Vertices[j].X, obj.Walls[i].Vertices[j].Y, obj.Walls[i].Vertices[j].Z + 0.05f);
                        }
                        obj.Walls[i].FindPlanes();
                    }
                }
            }

            if (Keyboard.IsKeyDown(Key.S))
            {
                foreach (var obj in objects)
                {
                    obj.Position = new Vector3(obj.Position.X, obj.Position.Y, obj.Position.Z - 0.05f);
                    for (int i = 0; i < obj.Walls.Count; i++)
                    {
                        for (int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                        {
                            obj.Walls[i].Vertices[j] = new Vector3(obj.Walls[i].Vertices[j].X, obj.Walls[i].Vertices[j].Y, obj.Walls[i].Vertices[j].Z - 0.05f);
                        }
                        obj.Walls[i].FindPlanes();
                    }
                }
            }


            if (Keyboard.IsKeyDown(Key.Add))
            {
                if (Zoom < 5)
                {
                    Zoom += 0.1f;
                }
            }

            if (Keyboard.IsKeyDown(Key.Subtract))
            {
                Zoom -= 0.1f;
                if (Zoom < 1)
                {
                    Zoom = 1;
                }
            }

            if (Keyboard.IsKeyDown(Key.X)) // rotacja wzgledem osi OX
            {
                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    Matrix.RotationX(0.01f, out XAxisRotationMatrix);
                    foreach (var obj in objects)
                    {
                        obj.Position = Vector3.TransformCoordinate(obj.Position, XAxisRotationMatrix);
                        for (int i = 0; i < obj.Walls.Count; i++)
                        {
                            for(int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                            {
                                obj.Walls[i].Vertices[j] = Vector3.TransformCoordinate(obj.Walls[i].Vertices[j], XAxisRotationMatrix);
                            }
                            obj.Walls[i].FindPlanes();
                        }
                    }
                }
                else
                {
                    Matrix.RotationX(-0.01f, out XAxisRotationMatrix); // inicjowanie/nadpisywanie macierzy
                    foreach (var obj in objects)
                    {
                        obj.Position = Vector3.TransformCoordinate(obj.Position, XAxisRotationMatrix);
                        for (int i = 0; i < obj.Walls.Count; i++)
                        {
                            for (int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                            {
                                obj.Walls[i].Vertices[j] = Vector3.TransformCoordinate(obj.Walls[i].Vertices[j], XAxisRotationMatrix);
                            }
                            obj.Walls[i].FindPlanes();
                        }
                    }
                }
            }
            
            if (Keyboard.IsKeyDown(Key.Y)) // rotacja wokol osi OY
            {
                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    Matrix.RotationY(-0.01f, out YAxisRotationMatrix);
                    foreach (var obj in objects)
                    {
                        obj.Position = Vector3.TransformCoordinate(obj.Position, YAxisRotationMatrix);
                        for (int i = 0; i < obj.Walls.Count; i++)
                        {
                            for (int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                            {
                                obj.Walls[i].Vertices[j] = Vector3.TransformCoordinate(obj.Walls[i].Vertices[j], YAxisRotationMatrix);
                            }
                            obj.Walls[i].FindPlanes();
                        }
                    }
                }
                else
                {
                    Matrix.RotationY(0.01f, out YAxisRotationMatrix); // inicjowanie/nadpisywanie macierzy
                    foreach (var obj in objects)
                    {
                        obj.Position = Vector3.TransformCoordinate(obj.Position, YAxisRotationMatrix);
                        for (int i = 0; i < obj.Walls.Count; i++)
                        {
                            for (int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                            {
                                obj.Walls[i].Vertices[j] = Vector3.TransformCoordinate(obj.Walls[i].Vertices[j], YAxisRotationMatrix);
                            }
                            obj.Walls[i].FindPlanes();
                        }
                    }
                }
            }
            
            if (Keyboard.IsKeyDown(Key.Z)) // obrot wokol osi OZ
            {
                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    Matrix.RotationZ(-0.01f, out ZAxisRotationMatrix);
                    foreach (var obj in objects)
                    {
                        obj.Position = Vector3.TransformCoordinate(obj.Position, ZAxisRotationMatrix);
                        for (int i = 0; i < obj.Walls.Count; i++)
                        {
                            for (int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                            {
                                obj.Walls[i].Vertices[j] = Vector3.TransformCoordinate(obj.Walls[i].Vertices[j], ZAxisRotationMatrix);
                            }
                            obj.Walls[i].FindPlanes();
                        }
                    }
                }
                else
                {
                    Matrix.RotationZ(0.01f, out ZAxisRotationMatrix); // inicjowanie/nadpisywanie macierzy
                    foreach (var obj in objects)
                    {
                        obj.Position = Vector3.TransformCoordinate(obj.Position, ZAxisRotationMatrix);
                        for (int i = 0; i < obj.Walls.Count; i++)
                        {
                            for (int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                            {
                                obj.Walls[i].Vertices[j] = Vector3.TransformCoordinate(obj.Walls[i].Vertices[j], ZAxisRotationMatrix);
                            }
                            obj.Walls[i].FindPlanes();
                        }
                    }
                }
            }
        }

        private void RenderCallback()
        {
            ClearScene();
            CheckInput();
            Draw();
        }

        public void Dispose()
        {
            renderForm.Dispose();
        }
    }
}
