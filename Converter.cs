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

        public static void Render(Object3D obj, Camera camera)
        {
            var worldMatrix = Matrix.Translation(obj.Position) * Matrix.RotationYawPitchRoll(obj.Rotation.X, obj.Rotation.Y, obj.Rotation.Z);
            var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
            var projectionMatrix = Matrix.PerspectiveFovRH(0.78f, (float)Camera.Width / Camera.Height, 0.01f, 1.0f);

            var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;

            List<Vector2> CubeVertices = new List<Vector2>();
            for (int i = 0; i < obj.Vertices.Length; i++)
            {
                var vertex = obj.Vertices[i];

                Vector3 targetVector = camera.Target - camera.Position;
                Vector3 objectVector = vertex - camera.Position;

                double scalarValue = targetVector.X * objectVector.X + targetVector.Y * objectVector.Y + targetVector.Z * objectVector.Z;
                double targetVectorLength = Math.Sqrt(Math.Pow(targetVector.X, 2) + Math.Pow(targetVector.Y, 2) + Math.Pow(targetVector.Z, 2));
                double objectVectorLength = Math.Sqrt(Math.Pow(objectVector.X, 2) + Math.Pow(objectVector.Y, 2) + Math.Pow(objectVector.Z, 2));
                double angle = Math.Acos(scalarValue / (targetVectorLength * objectVectorLength));
                if (angle < Math.PI/3) // ograniczenie widoku do 60 stopni
                {
                    var calculatedPoint = CastTo2D(vertex, transformMatrix, camera);
                    RenderPoint(calculatedPoint, camera);
                    CubeVertices.Add(calculatedPoint);
                }
                else
                {
                    CubeVertices.Add(new Vector2(-1,-1));
                }
            }

            Vector2 point1, point2;
            foreach (var connection in obj.ConnectedPoints)
            {
                point1 = CubeVertices[(int)connection.X];
                point2 = CubeVertices[(int)connection.Y];

                if (point1.X != -1 && point2.X != -1 && point1.Y != -1 && point2.Y != -1)
                {
                    RenderLineBetweenPoints(point1, point2, camera);
                }
            }
        }
    }
}
