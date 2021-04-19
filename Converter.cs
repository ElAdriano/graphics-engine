using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCamera
{
    public class Converter
    {
        public static void UpdatePixelValue(int x, int y, byte red, byte green, byte blue, byte alpha, Camera camera)
        {
            var index = (x + y * camera.CameraView.PixelSize.Width) * 4;

            camera.SceneCache[index] = red;
            camera.SceneCache[index + 1] = green;
            camera.SceneCache[index + 2] = blue;
            camera.SceneCache[index + 3] = alpha;
        }

        public static void RenderPoint(Vector2 point, Camera camera, SharpDX.Color Color)
        {
            if (point.X >= 0.0 && point.Y >= 0.0 && point.X < Camera.Width && point.Y < Camera.Height) // point is visible on screen
            {
                UpdatePixelValue((int)point.X, (int)point.Y, Color.R, Color.G, Color.B, Color.A, camera);
            }
        }

        // zwraca x, y punktu na ekranie i przeksztalcone z
        private static Vector3 CastTo2D(Vector3 point, Matrix transformationMatrix, Camera camera)
        {
            var movedPoint = Vector3.TransformCoordinate(point, transformationMatrix);

            var x = movedPoint.X * Camera.Width * camera.Zoom + Camera.Width / 2.0f;
            var y = -movedPoint.Y * Camera.Height * camera.Zoom + Camera.Height / 2.0f;
            return new Vector3(x, y, movedPoint.Z);
        }

        private static double CalculatePointsDistance(Vector2 point1, Vector2 point2)
        {
            return Math.Sqrt(Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2));
        }

        private static void RenderLineBetweenPoints(Vector2 point0, Vector2 point1, Camera camera, SharpDX.Color color)
        {
            Vector2 points2dVector = (point1 - point0) / 100;
            Vector2 pointToRender = point0;
            RenderPoint(point0, camera, color);
            RenderPoint(point1, camera, color);

            double lineLength = CalculatePointsDistance(point0, point1);
            double renderedLength = CalculatePointsDistance(point0, pointToRender);
            while (renderedLength < lineLength)
            {
                pointToRender += points2dVector;
                RenderPoint(pointToRender, camera, color);
                renderedLength = CalculatePointsDistance(point0, pointToRender);
            }
        }

        private static bool PixelInPolygon(int pixelX, int pixelY, Wall wallCast)
        {
            bool PointInside = false;
            for (int i = 0, j = wallCast.TwoDimentionalBorders.Count() - 1; i < wallCast.TwoDimentionalBorders.Count(); j = i++)
            {
                float xi = wallCast.TwoDimentionalBorders[i].X, yi = wallCast.TwoDimentionalBorders[i].Y;
                float xj = wallCast.TwoDimentionalBorders[j].X, yj = wallCast.TwoDimentionalBorders[j].Y;

                var intersect = ((yi > pixelY) != (yj > pixelY)) && (pixelX < (xj - xi) * (pixelY - yi) / (yj - yi) + xi);
                if (intersect)
                {
                    PointInside = !PointInside;
                }
            }
            return PointInside;
        }

        private static int GetPixelNearestOwner(List<Tuple<int, int>> PixelOwners, List<Object3D> objects, int pixelX, int pixelY, Camera camera)
        {

            int owner = 0;
            double minZ = double.MinValue;
            double z = 0;
            for (int i = 0; i < PixelOwners.Count; i++)
            {
                // precastX, precastY - wspolrzedne punktow w przestrzeni projekcji
                var precastX = (pixelX - Camera.Width / 2.0f) / (Camera.Width * camera.Zoom);
                var precastY = (Camera.Height / 2.0f - pixelY) / (Camera.Height * camera.Zoom);
                
                float[] planeCoefficients = objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].PlaneCoefficients;
                if (planeCoefficients[2] == 0)
                { 
                    // pionowa plaszczyzna
                    if (planeCoefficients[1] == 0)
                    {
                        int nearestWallVertexIndex = objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].GetVertex(true);
                        int furthestWallVertexIndex = objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].GetVertex(false);

                        Vector3 nearestPointCast = objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].TwoDimentionalBorders[nearestWallVertexIndex];
                        Vector3 furthestPointCast = objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].TwoDimentionalBorders[furthestWallVertexIndex];
                        double wholeDist = CalculatePointsDistance(new Vector2(nearestPointCast.X, nearestPointCast.Y), new Vector2(furthestPointCast.X, furthestPointCast.Y));

                        float a = (furthestPointCast.Y - nearestPointCast.Y) / (furthestPointCast.X - nearestPointCast.X);
                        float b = furthestPointCast.Y - a * nearestPointCast.X;
                        double pixelDist = CalculatePointsDistance(new Vector2(nearestPointCast.X, nearestPointCast.Y), new Vector2(pixelX, a * pixelX + b));

                        double multiplier = pixelDist / wholeDist;
                        float substractedDist = (float)multiplier * (float)Math.Abs(objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].Vertices[nearestWallVertexIndex].Z - objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].Vertices[furthestWallVertexIndex].Z);
                        z = objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].Vertices[nearestWallVertexIndex].Z - substractedDist;

                        if (z > minZ)
                        {
                            minZ = z;
                            owner = PixelOwners[i].Item1;
                        }
                    }
                    // pozioma plaszczyzna
                    else if (planeCoefficients[0] == 0)
                    {
                        int nearestWallVertexIndex = objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].GetVertex(true);
                        int furthestWallVertexIndex = objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].GetVertex(false);

                        Vector3 nearestPointCast = objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].TwoDimentionalBorders[nearestWallVertexIndex];
                        Vector3 furthestPointCast = objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].TwoDimentionalBorders[furthestWallVertexIndex];
                        double wholeDist = CalculatePointsDistance(new Vector2(nearestPointCast.X, nearestPointCast.Y), new Vector2(furthestPointCast.X, furthestPointCast.Y));

                        float a = (furthestPointCast.Y - nearestPointCast.Y) / (furthestPointCast.X - nearestPointCast.X);
                        float b = furthestPointCast.Y - a * nearestPointCast.X;

                        double pixelDist = CalculatePointsDistance(new Vector2(nearestPointCast.X, nearestPointCast.Y), new Vector2((pixelY - b)/a, pixelY));

                        double multiplier = pixelDist / wholeDist;
                        float substractedDist = (float)multiplier * (float)Math.Abs(objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].Vertices[nearestWallVertexIndex].Z - objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].Vertices[furthestWallVertexIndex].Z);
                        z = objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].Vertices[nearestWallVertexIndex].Z - substractedDist;

                        if (z > minZ)
                        {
                            minZ = z;
                            owner = PixelOwners[i].Item1;
                        }
                    }
                    // plaszczyzna pochylona
                    else
                    {
                        List<double> distances = new List<double>();

                        for (int vertexCastId = 0; vertexCastId < objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].TwoDimentionalBorders.Count(); vertexCastId++)
                        {
                            var precastVertexX = (objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].TwoDimentionalBorders[vertexCastId].X - Camera.Width / 2.0f) / (Camera.Width * camera.Zoom);
                            var precastVertexY = (Camera.Height / 2.0f - objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].TwoDimentionalBorders[vertexCastId].Y) / (Camera.Height * camera.Zoom);
                            double distance = CalculatePointsDistance(
                                new Vector2(precastX, precastY),
                                new Vector2(precastVertexX, precastVertexY)
                            );
                            distances.Add(distance);
                        }

                        double weightSum = 0;
                        for (int vertexCastId = 0; vertexCastId < distances.Count(); vertexCastId++)
                        {
                            z += (double)Math.Pow((1 / distances[vertexCastId]), 2) * objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].Vertices[vertexCastId].Z;
                            weightSum += (double)Math.Pow((1 / distances[vertexCastId]), 2);
                        }
                        z /= weightSum;

                        // Console.WriteLine("Z wynosi:{0}", z);
                        //z = -(planeCoefficients[3] + planeCoefficients[0] * precastX + planeCoefficients[1] * precastY) / planeCoefficients[2];
                        if (z > minZ)
                        {
                            minZ = z;
                            owner = PixelOwners[i].Item1;
                        }
                    }
                }
                else
                {
                    List<double> distances = new List<double>();

                    for (int vertexCastId = 0; vertexCastId < objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].TwoDimentionalBorders.Count(); vertexCastId++)
                    {
                        var precastVertexX = (objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].TwoDimentionalBorders[vertexCastId].X - Camera.Width / 2.0f) / (Camera.Width * camera.Zoom);
                        var precastVertexY = (Camera.Height / 2.0f - objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].TwoDimentionalBorders[vertexCastId].Y) / (Camera.Height * camera.Zoom);
                        double distance = CalculatePointsDistance(
                            new Vector2(precastX, precastY), 
                            new Vector2(precastVertexX, precastVertexY)
                        );
                        distances.Add(distance);
                    }

                    double weightSum = 0;
                    for (int vertexCastId = 0; vertexCastId < distances.Count(); vertexCastId++)
                    {
                        z += (double)Math.Pow((1 / distances[vertexCastId]), 2) * objects[PixelOwners[i].Item1].Walls[PixelOwners[i].Item2].Vertices[vertexCastId].Z;
                        weightSum += (double)Math.Pow((1 / distances[vertexCastId]), 2);
                    }
                    z /= weightSum;

                    // Console.WriteLine("Z wynosi:{0}", z);
                    //z = -(planeCoefficients[3] + planeCoefficients[0] * precastX + planeCoefficients[1] * precastY) / planeCoefficients[2];
                    if (z > minZ)
                    {
                        minZ = z;
                        owner = PixelOwners[i].Item1;
                    }
                }
            }
            return owner;
        }

        private static SharpDX.Color GetPixelValue(List<Object3D> objects, int pixelX, int pixelY, Camera camera)
        {
            List<Tuple<int, int>> PixelOwners = new List<Tuple<int, int>>(); // para <index obiektu, index sciany>
            for(int objNum = 0; objNum < objects.Count(); objNum++)
            {
                if(objects[objNum].Position.Z > 0)
                {
                    continue;
                }
                for(int wallNum = 0; wallNum < objects[objNum].Walls.Count(); wallNum++)
                {
                    bool containsPixel = PixelInPolygon(pixelX, pixelY, objects[objNum].Walls[wallNum]);
                    if(containsPixel)
                    {
                        PixelOwners.Add(new Tuple<int, int>(objNum, wallNum));
                    }
                }
            }

            if(PixelOwners.Count() == 0)
            {
                return new SharpDX.Color(0, 0, 0, 255); // piksel nie nalezy do zadnego rzutu
            }
            else if (PixelOwners.Count() == 1) 
            {
                return objects[PixelOwners[0].Item1].Color;
            }
            else
            {
                int finalOwner = GetPixelNearestOwner(PixelOwners, objects, pixelX, pixelY, camera);
                return objects[finalOwner].Color;
            }
        }

        /**
         * Zwracana jest lista krotek - <int, int, float>
         * Item1 (pierwszy int) - index obiektu dla ktorego zostalo znalezione rozwiazanie
         * Item2 (drugi int)    - index sciany dla ktorej zostalo znalezione rozwiazanie
         * Item3 (float)        - wartosc rozwiazania
         */
        private static List<Tuple<int, int, float>> FindIntersections(List<Object3D> objects, int yValue)
        {
            List<Tuple<int, int, float>> solutions = new List<Tuple<int, int, float>>();
            for(int objNum = 0; objNum < objects.Count(); objNum++)
            {
                for(int wallNum = 0; wallNum < objects[objNum].Walls.Count(); wallNum++)
                {
                    foreach (var equation in objects[objNum].Walls[wallNum].CastEquations)
                    {
                        // linia pionowa
                        if (equation.Item1 == 1 && equation.Item4 <= yValue && yValue <= equation.Item5)
                        {
                            solutions.Add(new Tuple<int, int, float>(objNum, wallNum, equation.Item2)); // ktory obiekt, ktora sciana, pixel x do aktualizacji
                        }
                        // linia pozioma
                        else if (equation.Item1 == 2 && (int)equation.Item3 == yValue)
                        {
                            solutions.Add(new Tuple<int, int, float>(objNum, wallNum, equation.Item4));
                            solutions.Add(new Tuple<int, int, float>(objNum, wallNum, equation.Item5));
                        }
                        // linia krzywa (zaleznosc y = ax + b
                        else if (equation.Item1 == 0)
                        {
                            float x = (yValue - equation.Item3) / equation.Item2;
                            if (equation.Item4 <= x && x <= equation.Item5)
                            {
                                solutions.Add(new Tuple<int, int, float>(objNum, wallNum, x));
                            }
                        }
                    }
                }
            }

            solutions.Sort((sol1, sol2) => { 
                if (sol1.Item3 == sol2.Item3)
                {
                    return 0;
                }
                else if (sol1.Item3 < sol2.Item3)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            });
            return solutions;
        }

        public static void Render(List<Object3D> objects, Camera camera)
        {
            // wyliczam cast scian na 2D
            for (int objNum = 0; objNum < objects.Count; objNum++)
            {
                Object3D obj = objects[objNum];
                if (obj.Position.Z > 0)
                {
                    continue;
                }
                var worldMatrix = Matrix.Translation(obj.Position) * Matrix.RotationYawPitchRoll(obj.Rotation.X, obj.Rotation.Y, obj.Rotation.Z);
                var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
                var projectionMatrix = Matrix.PerspectiveFovRH(0.78f, (float)Camera.Width / Camera.Height, 0.01f, 1.0f);

                var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;
                Dictionary<string, Vector2> renderedVertices = new Dictionary<string, Vector2>();

                foreach (Wall wall in obj.Walls)
                {
                    wall.TwoDimentionalBorders.Clear();
                    foreach (Vector3 vertex in wall.Vertices)
                    {
                        var calculatedPoint = CastTo2D(vertex, transformMatrix, camera);
                        wall.TwoDimentionalBorders.Add(calculatedPoint);

                        if (!renderedVertices.ContainsKey(vertex.ToString()))
                        {
                            RenderPoint(new Vector2(calculatedPoint.X, calculatedPoint.Y), camera, obj.Color);
                            renderedVertices.Add(vertex.ToString(), new Vector2(calculatedPoint.X, calculatedPoint.Y));
                        }
                    }
                    wall.FindCast2DEquations();
                }

                /*string vertex1Id, vertex2Id;
                foreach (Wall wall in obj.Walls)
                {
                    for (int i = 0; i < wall.Vertices.Count - 1; i++)
                    {
                        vertex1Id = wall.Vertices[i].ToString();
                        vertex2Id = wall.Vertices[i + 1].ToString();
                        if (renderedVertices.ContainsKey(vertex1Id) && renderedVertices.ContainsKey(vertex2Id))
                        {
                            RenderLineBetweenPoints(renderedVertices[vertex1Id], renderedVertices[vertex2Id], camera, new SharpDX.Color(255, 255, 255, 255));
                        }
                    }

                    vertex1Id = wall.Vertices[wall.Vertices.Count - 1].ToString();
                    vertex2Id = wall.Vertices[0].ToString();
                    if (renderedVertices.ContainsKey(vertex1Id) && renderedVertices.ContainsKey(vertex2Id))
                    {
                        RenderLineBetweenPoints(renderedVertices[vertex1Id], renderedVertices[vertex2Id], camera, obj.Color);
                    }
                }*/
            }

            for (int pixelY = 0; pixelY < Camera.Height; pixelY++)
            {
                List<Tuple<int, int, float>> solutions = FindIntersections(objects, pixelY);

                if (solutions.Count() > 0) 
                {
                    for (int pixelX = (int)solutions[0].Item3; pixelX <= (int)Math.Ceiling(solutions[solutions.Count() - 1].Item3); pixelX++)
                    {
                        SharpDX.Color pixelColor = GetPixelValue(objects, pixelX, pixelY, camera);
                        RenderPoint(new Vector2(pixelX, pixelY), camera, pixelColor);
                    }
                }
            }
        }
    }
}
