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

        public const int Width = 256;
        public const int Height = 144;

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
            FarClippingValue = 1;
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

            VerticalViewRange = 10;
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

                tmpList.Add(new Vector3(FarClippingValue * (float)Math.Tan( -HorizontalAngle), HighestRow - i * spaceBetween , FarClippingValue));
                tmpList.Add(new Vector3(FarClippingValue * (float)Math.Tan( HorizontalAngle), HighestRow - i * spaceBetween, FarClippingValue));
                
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

        public List<Tuple<int, int, Vector2>> FindIntersections(ScanPlane plane, List<Object3D> objects)
        {
            List<Tuple<int, int, Vector2>> intersections = new List<Tuple<int, int, Vector2>>();
            for (int objIdx = 0; objIdx < objects.Count; objIdx++)
            {
                Object3D obj = objects[objIdx];
                for(int w = 0; w < obj.Walls.Count; w++)
                {
                    Wall wall = obj.Walls[w];
                    List<LineEquation> list = wall.CalculateEquationsForEdges();

                    foreach(LineEquation eq in list)
                    {
                        if(eq.Deltas.Y == 0)
                        {
                            if (eq.StartPoint.Y == plane.YValue)
                            {
                                Vector2 point = new Vector2(eq.StartPoint.X, eq.StartPoint.Z);
                                intersections.Add(new Tuple<int, int, Vector2>(objIdx, w, point));
                                point = new Vector2(eq.StartPoint.X + eq.Deltas.X, eq.StartPoint.Z + eq.Deltas.Z);
                                intersections.Add(new Tuple<int, int, Vector2>(objIdx, w, point));
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
                                                                                           //plane.SurfaceCoefficients[0] * eq.Deltas.X + plane.SurfaceCoefficients[1] * eq.Deltas.Y + plane.SurfaceCoefficients[2] * eq.Deltas.Z;
                            float t = numerator / denominator;

                            Console.WriteLine("\n\n\n");
                            Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");

                            Console.WriteLine("t = {0}, numerator = {1}, denominator = {2}", t, numerator, denominator);

                            Console.WriteLine("Surface coefficients : A = {0}, B = {1}, C = {2}, D = {3}", plane.SurfaceCoefficients[0], plane.SurfaceCoefficients[1], plane.SurfaceCoefficients[2], plane.SurfaceCoefficients[3]);

                            Console.WriteLine("eq startPoint = " + eq.StartPoint.ToString());
                            Console.WriteLine("eq deltas = " + eq.Deltas.ToString());
                            Vector2 point = new Vector2(eq.StartPoint.X + eq.Deltas.X * t, eq.StartPoint.Z + eq.Deltas.Z * t);
                            Console.WriteLine("point = " + point.ToString());
                            intersections.Add(new Tuple<int, int, Vector2>(objIdx, w, point));
                        }
                    }
                    /*
                     * for (int i = 0; i < intersections.Count; i++)
                    {
                        Vector2 point = intersections[i];
                        for(int j = 0; j < wall.Vertices.Count; j++)
                        {
                            Vector3 vertex = wall.Vertices[j];
                            if (vertex == new Vector3(point.X, plane.SurfaceCoefficients[3], point.Y))
                            {
                                if (wall.Vertices[(j + 1) % wall.Vertices.Count].Y == plane.SurfaceCoefficients[3] && )
                                {
                                    
                                }
                            }
                        }
                    }
                    */
                }
            }
            //intersections.Sort((x, y) => { return x.Item3.X < y.Item3.X ? -1 : 1; });
            return intersections;
        }
        
        public Tuple<List<Tuple<int, int, Vector2>>, List<Tuple<int, int, Vector2>>> FilterSinglePoints(List<Tuple<int, int, Vector2>> intersections)
        {
            Dictionary<string, int> objectsAndWallsMap = new Dictionary<string, int>();

            foreach(Tuple<int, int, Vector2> element in intersections)
            {
                if (objectsAndWallsMap.ContainsKey(element.Item1.ToString() + "_" + element.Item2.ToString()))
                {
                    objectsAndWallsMap[element.Item1.ToString() + "_" + element.Item2.ToString()]++;
                }
                else
                {
                    objectsAndWallsMap[element.Item1.ToString() + "_" + element.Item2.ToString()] = 1;
                }
            }

            List<Tuple<int, int, Vector2>> filteredPoints = new List<Tuple<int, int, Vector2>>();
            List<Tuple<int, int, Vector2>> restIntersections = new List<Tuple<int, int, Vector2>>();

            foreach(Tuple<int, int, Vector2> element in intersections)
            {
                if (objectsAndWallsMap[element.Item1.ToString() + "_" + element.Item2.ToString()] == 1)
                {
                    filteredPoints.Add(element);
                }
                else
                {
                    restIntersections.Add(element);
                }
            }

            return new Tuple<List<Tuple<int, int, Vector2>>, List<Tuple<int, int, Vector2>>>(filteredPoints, restIntersections);
        }

        public List<Tuple<int, int, Vector2, Tuple<float, float>>> CalculateLines(List<Tuple<int, int, Vector2>> restIntersections, ref List<Tuple<int, int, Vector2>> singlePoints) // wyliczanie linii tworzonych przez punkty przeciecia
        {
            List<Tuple<int, int, Vector2, Tuple<float, float>>> lines = new List<Tuple<int, int, Vector2, Tuple<float, float>>>();
            for(int i = 0; i < restIntersections.Count - 1; i++)
            {
                for(int j = i + 1; j < restIntersections.Count; j++)
                {
                    if(restIntersections[i].Item1 == restIntersections[j].Item1 && restIntersections[i].Item2 == restIntersections[j].Item2) // ten sam obiekt i sciana
                    {
                        Vector2 p1 = restIntersections[i].Item3, p2 = restIntersections[j].Item3;
                        if (p1.Y < 0 && p2.Y < 0)
                        {
                            continue;
                        }
                        if (p1.X - p2.X == 0)
                        {
                            if (p1.Y >= 0 && p2.Y < 0 || p2.Y >= 0 && p1.Y < 0)
                            {
                                singlePoints.Add(new Tuple<int, int, Vector2>(restIntersections[i].Item1, restIntersections[i].Item2, new Vector2(p1.X, 0)));
                            }
                            if (p1.Y > 0 && p2.Y > 0)
                            {
                                singlePoints.Add(new Tuple<int, int, Vector2>(restIntersections[i].Item1, restIntersections[i].Item2, new Vector2(p1.X, Math.Min(p1.Y, p2.Y))));
                            }
                        }
                        else
                        {
                            float coefficient_x = (p1.Y - p2.Y) / (p1.X - p2.X);
                            float coefficient_y = (p1.Y - coefficient_x * p1.X);
                            lines.Add(new Tuple<int, int, Vector2, Tuple<float,float>>(restIntersections[i].Item1, restIntersections[i].Item2, new Vector2(coefficient_x, coefficient_y), new Tuple<float,float>(p1.X, p2.X)));
                        }
                    }
                }
            }
            return lines;
        }

        public SharpDX.Color GetPixelColor(float horizontalValue, List<Tuple<int, int, Vector2, Tuple<float, float>>> lines, List<Tuple<int, int, Vector2>> singlePoints)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();

            SharpDX.Color returnedColor = new SharpDX.Color(0, 0, 0, 1f);
            float minZ = float.MaxValue;
            for(int i = 0; i < lines.Count; i++)
            {
                if (   lines[i].Item4.Item1 < horizontalValue && horizontalValue < lines[i].Item4.Item2 
                    || lines[i].Item4.Item2 < horizontalValue && horizontalValue < lines[i].Item4.Item1 )
                {
                    float z = lines[i].Item3.X * horizontalValue + lines[i].Item3.Y;
                    if (z < minZ)
                    {
                        minZ = z;
                        returnedColor = objects[lines[i].Item1].Color;
                    }
                }
            }

            foreach (Tuple<int, int, Vector2> point in singlePoints)
            {
                if (point.Item3.Y < minZ)
                {
                    minZ = point.Item3.Y;
                    returnedColor = objects[point.Item1].Color;
                }
            }
            return returnedColor;
        }

        private void Draw()
        {
            Converter.Render(objects, this);
            for(int scanlineNumber = 0; scanlineNumber < PlanesList.Count; scanlineNumber++)
            {
                ScanPlane scanline = PlanesList[scanlineNumber];
                List<Tuple<int, int, Vector2>> intersections = FindIntersections(scanline, objects);

                //Console.WriteLine("Intersection value -> z(x): x = {0}, z = {1}", intersections[0].Item3.X, intersections[0].Item3.Y);
                Tuple<List<Tuple<int, int, Vector2>>, List<Tuple<int, int, Vector2>>> filtrationResults = FilterSinglePoints(intersections);

                List<Tuple<int, int, Vector2>> singlePoints = filtrationResults.Item1;
                List<Tuple<int, int, Vector2, Tuple<float, float>>> lines = CalculateLines(filtrationResults.Item2, ref singlePoints); // filtrationResults.Item2 - rest intersections

                float mostLeft = -0.5f * HorizontalViewRange;
                float step = HorizontalViewRange / Width;

                for (int x = 0; x < Width; x++)
                {
                    SharpDX.Color color = GetPixelColor(mostLeft + x * step, lines, singlePoints);
                    //Console.WriteLine("Dla piksela ({0}, {1}) jest kolor: {2}", x, scanlineNumber, color.ToString());

                    Converter.UpdatePixelValue(x, scanlineNumber, (byte)(255 * color.R), (byte)(255 * color.G), (byte)(255 * color.B), (byte)(255 * color.A), this);
                }
            }

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
