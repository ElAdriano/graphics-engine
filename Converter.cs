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
        public static void UpdatePixelValue(int x, int y,SharpDX.Color color, Camera camera)
        {
            var index = (x + y * camera.CameraView.PixelSize.Width) * 4;

            camera.SceneCache[index] = color.R;
            camera.SceneCache[index + 1] = color.G;
            camera.SceneCache[index + 2] = color.B;
            camera.SceneCache[index + 3] = color.A;
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
