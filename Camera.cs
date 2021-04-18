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
        public const int Width = 480;//256;
        public const int Height = 360;//144;
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
        public float AngleOfView;
        public float FarClippingValue;
        public float NearClippingValue;
        public float screenHeightInPerspective;
        public float HighestRow;
        public float VerticalAngle;
        public float HorizontalAngle;
        public float HorizontalViewRange;
        public float VerticalViewRange;

        public List<ScanPlane> PlanesList;

        public Camera()
        {
            renderForm = new RenderForm("VirtualCamera");
            renderForm.ClientSize = new Size(Width, Height);
            renderForm.AllowUserResizing = false;
            Position = new Vector3(0, 0, 0);
            Target = new Vector3(0, 0, 9f);

            SceneCache = new byte[Width * Height * 4];
            Zoom = 1;
            AngleOfView = 0.78f;
            FarClippingValue = 1f;
            NearClippingValue = 0.01f;

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
            screenHeightInPerspective = 1.0f;
            HighestRow = screenHeightInPerspective * 0.5f;

            VerticalAngle = 0.5f * AngleOfView;
            float ctg = (float)Width / Height;
            HorizontalAngle = (AngleOfView * ctg) / 2;

            VerticalViewRange = 1000;
            HorizontalViewRange = ctg * VerticalViewRange;
            CreatePlanes();
        }

        private void CreatePlanes()
        {
            // plaszczyzny w przestrzeni perspektywy
            PlanesList = new List<ScanPlane>();
            List<Vector3> tmpList;

            float spaceBetween = VerticalViewRange / Height;

            for (int i = 0; i < Height; i++)
            {
                tmpList = new List<Vector3>();
                tmpList.Add(new Vector3(0, HighestRow - i * spaceBetween, 0));

                tmpList.Add(new Vector3(-1, HighestRow - i * spaceBetween , FarClippingValue));
                tmpList.Add(new Vector3(1, HighestRow - i * spaceBetween, FarClippingValue));
                
                PlanesList.Add(new ScanPlane(tmpList));
            }
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
        private List<LineEquation> getEdges(List<Object3D> objects)
        {
            List<LineEquation> eqs = new List<LineEquation>();
            for (int o = 0; o < objects.Count; o++)
            {
                for(int w = 0; w < objects[o].Walls.Count; w++)
                {
                    eqs.AddRange(objects[o].Walls[w].CalculateEquationsForEdges(o,w));
                }
            }
            return eqs;
        }

        public List<Tuple<int, int, Vector2>> FindIntersections(ScanPlane plane, List<LineEquation> list)
        {
            List<Tuple<int, int, Vector2>> intersections = new List<Tuple<int, int, Vector2>>();

            foreach (LineEquation eq in list)
            {
                if (eq.Deltas.Y == 0)
                {
                    if (eq.StartPoint.Y == plane.YValue)
                    {
                        Vector2 point = new Vector2(eq.StartPoint.X, eq.StartPoint.Z);
                        intersections.Add(new Tuple<int, int, Vector2>(eq.ObjID, eq.WallID, point));
                        point = new Vector2(eq.StartPoint.X + eq.Deltas.X, eq.StartPoint.Z + eq.Deltas.Z);
                        intersections.Add(new Tuple<int, int, Vector2>(eq.ObjID, eq.WallID, point));
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    float numerator = -(plane.SurfaceCoefficients[3] + plane.SurfaceCoefficients[1] * eq.StartPoint.Y);//-(plane.SurfaceCoefficients[0] * eq.StartPoint.X + plane.SurfaceCoefficients[1] * eq.StartPoint.Y + plane.SurfaceCoefficients[2] * eq.StartPoint.Z + plane.SurfaceCoefficients[3]);
                    float denominator = plane.SurfaceCoefficients[1] * eq.Deltas.Y;// plaszczyzny sa tylko rownolegle do pl. XoZ, wiec A i C = 0

                    float t = numerator / denominator;

                    Vector2 point = new Vector2(eq.StartPoint.X + eq.Deltas.X * t, eq.StartPoint.Z + eq.Deltas.Z * t);
                    intersections.Add(new Tuple<int, int, Vector2>(eq.ObjID, eq.WallID, point));

                }
            }
            return intersections;
        }       
        private Tuple<float,float> getLine(Tuple<int, int, Vector2> point1, Tuple<int, int, Vector2> point2)
        {
            float a = (point1.Item3.Y - point2.Item3.Y)/(point1.Item3.Y - point2.Item3.Y);
            float b = (point1.Item3.Y - a * point1.Item3.X);
            return Tuple.Create(a, b);
        }


        private SharpDX.Color[] getColors(List<Tuple<int, int, Vector2>> intersections)
        {   
            // wyliczam kolory linii // zachowuje kolor i glebokosc
            float mostLeft = - HorizontalViewRange / 2;
            float space = HorizontalViewRange / Width;
            float[] glebokosci = new float[Width];
            SharpDX.Color[] linia = new SharpDX.Color[Width];
            for (int i = 0; i< glebokosci.Length; i++)
            {
                glebokosci[i] = float.MaxValue;
                //wypełnienie
                linia[i] = new SharpDX.Color(0, 0, 0, 255);
            }
            
            while(intersections.Count > 0)
            {
                Tuple<int, int, Vector2> point1 = intersections[0];
                intersections.RemoveAt(0);
                Tuple<int, int, Vector2> point2 = null;
                //Obliczenie kolorów dla linii
                if (intersections.Count > 0 && intersections[0].Item1 == point1.Item1 && intersections[0].Item2 == point1.Item2)
                {

                    point2 = intersections[0];
                    intersections.RemoveAt(0);
                    if (point1.Item3.X > point2.Item3.X)
                    {
                        var tmp = point1;
                        point1 = point2;
                        point2 = tmp;
                    }

                    Tuple<float, float> line = getLine(point1, point2);
                    int firstIndex = (int)((point1.Item3.X / space) + (Width / 2));
                    int lastIndex = (int)((point1.Item3.X / space) + (Width / 2));
                    // sprawdzanie czy mieści się w ekranie
                    if (firstIndex > Width && lastIndex > Width || firstIndex < 0 && lastIndex < 0)
                        continue;
                    else
                    {
                        if (firstIndex < 0)
                            firstIndex = 0;
                        if (lastIndex >= Width)
                            lastIndex = Width -1;
                    }
                    // zaznaczanie na lini koloru razem z głebokością
                    for (int i = firstIndex; i <= lastIndex; i++)
                    {
                        float z = 0;
                        z = i * space * line.Item1 + line.Item2;
                        if(glebokosci[i] > z && z >= 0)
                        {
                            glebokosci[i] = z;
                            linia[i] = objects[point1.Item1].Color;
                        }
                    }
                }
                else
                {
                    if ( point1.Item3.X > mostLeft && - mostLeft > point1.Item3.X && point1.Item3.Y >= 0)
                    {
                        int index = (int)((point1.Item3.X / space) + (Width / 2));
                        if (glebokosci[index] > point1.Item3.Y)
                        {
                            linia[index] = objects[point1.Item1].Color;
                            glebokosci[index] = point1.Item3.Y;
                        }
                    }
                }
            }
            return linia;
        }
        private void Draw()
        {
            // rzut na przestrzeń perspektywy
            List<Object3D> newObjects= Converter.Render(objects, this);
            List<LineEquation> edges = getEdges(newObjects);
            for(int scanlineNumber = 0; scanlineNumber < PlanesList.Count; scanlineNumber++)
            {
                ScanPlane scanline = PlanesList[scanlineNumber];
                // znajdź punkty przecięcia lini z płaszczyzną
                List<Tuple<int, int, Vector2>> intersections = FindIntersections(scanline, edges);
                Console.WriteLine("Znalazłem tyle przecięć: {0}", intersections.Count);
                SharpDX.Color[] rzadPixeli = getColors(intersections);

                float mostLeft = -0.5f * HorizontalViewRange;
                float step = HorizontalViewRange / Width;

                for (int x = 0; x < Width; x++)
                {
                    Converter.UpdatePixelValue(x, scanlineNumber, rzadPixeli[x], this);
                }
            }

            SceneBuffer.CopyFromMemory(SceneCache, 4 * Width);
            CameraView.BeginDraw();
          //  CameraView.Clear(new Color4(0, 0, 0, 255f));
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
                    Matrix.RotationX(0.005f, out XAxisRotationMatrix);
                    foreach (var obj in objects)
                    {
                        obj.Position = Vector3.TransformCoordinate(obj.Position, XAxisRotationMatrix);
                        for (int i = 0; i < obj.Walls.Count; i++)
                        {
                            for(int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                            {
                                obj.Walls[i].Vertices[j] = Vector3.TransformCoordinate(obj.Walls[i].Vertices[j], XAxisRotationMatrix);
                            }
                        }
                    }
                }
                else
                {
                    Matrix.RotationX(-0.005f, out XAxisRotationMatrix); // inicjowanie/nadpisywanie macierzy
                    foreach (var obj in objects)
                    {
                        obj.Position = Vector3.TransformCoordinate(obj.Position, XAxisRotationMatrix);
                        for (int i = 0; i < obj.Walls.Count; i++)
                        {
                            for (int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                            {
                                obj.Walls[i].Vertices[j] = Vector3.TransformCoordinate(obj.Walls[i].Vertices[j], XAxisRotationMatrix);
                            }
                        }
                    }
                }
            }
            
            if (Keyboard.IsKeyDown(Key.Y)) // rotacja wokol osi OY
            {
                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    Matrix.RotationY(-0.005f, out YAxisRotationMatrix);
                    foreach (var obj in objects)
                    {
                        obj.Position = Vector3.TransformCoordinate(obj.Position, YAxisRotationMatrix);
                        for (int i = 0; i < obj.Walls.Count; i++)
                        {
                            for (int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                            {
                                obj.Walls[i].Vertices[j] = Vector3.TransformCoordinate(obj.Walls[i].Vertices[j], YAxisRotationMatrix);
                            }
                        }
                    }
                }
                else
                {
                    Matrix.RotationY(0.005f, out YAxisRotationMatrix); // inicjowanie/nadpisywanie macierzy
                    foreach (var obj in objects)
                    {
                        obj.Position = Vector3.TransformCoordinate(obj.Position, YAxisRotationMatrix);
                        for (int i = 0; i < obj.Walls.Count; i++)
                        {
                            for (int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                            {
                                obj.Walls[i].Vertices[j] = Vector3.TransformCoordinate(obj.Walls[i].Vertices[j], YAxisRotationMatrix);
                            }
                        }
                    }
                }
            }
            
            if (Keyboard.IsKeyDown(Key.Z)) // obrot wokol osi OZ
            {
                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    Matrix.RotationZ(-0.005f, out ZAxisRotationMatrix);
                    foreach (var obj in objects)
                    {
                        obj.Position = Vector3.TransformCoordinate(obj.Position, ZAxisRotationMatrix);
                        for (int i = 0; i < obj.Walls.Count; i++)
                        {
                            for (int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                            {
                                obj.Walls[i].Vertices[j] = Vector3.TransformCoordinate(obj.Walls[i].Vertices[j], ZAxisRotationMatrix);
                            }
                        }
                    }
                }
                else
                {
                    Matrix.RotationZ(0.005f, out ZAxisRotationMatrix); // inicjowanie/nadpisywanie macierzy
                    foreach (var obj in objects)
                    {
                        obj.Position = Vector3.TransformCoordinate(obj.Position, ZAxisRotationMatrix);
                        for (int i = 0; i < obj.Walls.Count; i++)
                        {
                            for (int j = 0; j < obj.Walls[i].Vertices.Count; j++)
                            {
                                obj.Walls[i].Vertices[j] = Vector3.TransformCoordinate(obj.Walls[i].Vertices[j], ZAxisRotationMatrix);
                            }
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
