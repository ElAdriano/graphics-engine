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

        public static void RenderPoint(Vector2 point, Camera camera)
        {
            if (point.X >= 0.0 && point.Y >= 0.0 && point.X < Camera.Width && point.Y < Camera.Height) // point is visible on screen
            {
                UpdatePixelValue((int)point.X, (int)point.Y, 255, 0, 0, 255, camera);
            }
        }

        private static Vector2 CastTo2D(Vector3 point, Matrix transformationMatrix, Camera camera)
        {
            var movedPoint = Vector3.TransformCoordinate(point, transformationMatrix);

            var x = movedPoint.X * Camera.Width * camera.Zoom + Camera.Width / 2.0f;
            var y = -movedPoint.Y * Camera.Height * camera.Zoom + Camera.Height / 2.0f;
            return (new Vector2(x, y));
        }

        private static double CalculatePointsDistance(Vector2 point1, Vector2 point2)
        {
            return Math.Sqrt(Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2));
        }

        private static void RenderLineBetweenPoints(Vector2 point0, Vector2 point1, Camera camera)
        {
            Vector2 points2dVector = (point1 - point0) / 100;
            Vector2 pointToRender = point0;
            RenderPoint(point0, camera);
            RenderPoint(point1, camera);

            double lineLength = CalculatePointsDistance(point0, point1);
            double renderedLength = CalculatePointsDistance(point0, pointToRender);
            while (renderedLength < lineLength)
            {
                pointToRender += points2dVector;
                RenderPoint(pointToRender, camera);
                renderedLength = CalculatePointsDistance(point0, pointToRender);
            }
        }

        public static List<Object3D> Render(List<Object3D> objects, Camera camera)
        {
            List<Object3D> newObjects = new List<Object3D>();
            foreach(var obj in objects)
            {
                var worldMatrix = Matrix.Translation(obj.Position) * Matrix.RotationYawPitchRoll(obj.Rotation.X, obj.Rotation.Y, obj.Rotation.Z);
                var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
                var projectionMatrix = Matrix.PerspectiveFovRH(camera.AngleOfView, (float)Camera.Width / Camera.Height, camera.NearClippingValue, camera.FarClippingValue);

                var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;

                List<Wall> newWalls = new List<Wall>();
                foreach (Wall wall in obj.Walls)
                {
                    List<Vector3> newVertices = new List<Vector3>();
                    foreach (Vector3 vertex in wall.Vertices)
                    {
                        newVertices.Add(Vector3.TransformCoordinate(vertex, transformMatrix));
                        //tutaj można zaczac zrownoleglanie jeslibedzie dzialac jak gowno     
                    }
                    newWalls.Add(new Wall(newVertices));
                }
                newObjects.Add(new Object3D(obj.Name, Vector3.TransformCoordinate(obj.Position, transformMatrix), newWalls, obj.Color));
            }
            return newObjects;
        }
    }
}
