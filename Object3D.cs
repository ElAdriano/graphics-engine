using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace VirtualCamera
{
    public class Object3D
    {
        public string Name { get; set; }
        public Vector3[] Vertices { get; private set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public List<Vector2> ConnectedPoints { get; set; }

        public Object3D(string name, Vector3 Position)
        {
            Vertices = new Vector3[8];
            Rotation = new Vector3(0, 0, 0);
            this.Position = Position;

            Vertices[0] = new Vector3(1, 1, 1);
            Vertices[1] = new Vector3(-1, 1, 1);
            Vertices[2] = new Vector3(-1, -1, 1);
            Vertices[3] = new Vector3(1, -1, 1);

            Vertices[4] = new Vector3(1, 1, -1);
            Vertices[5] = new Vector3(-1, 1, -1);
            Vertices[6] = new Vector3(-1, -1, -1);
            Vertices[7] = new Vector3(1, -1, -1);

            for (int i = 0; i < 8; i++)
            {
                Vertices[i] = new Vector3(Position.X + Vertices[i].X, Position.Y + Vertices[i].Y, Position.Z + Vertices[i].Z);
            }

            ConnectedPoints = new List<Vector2>();
            for (int i = 0; i < Vertices.Length - 1; i++)
            {
                for (int j = i + 1; j < Vertices.Length; j++)
                {
                    Vector3 pt1 = Vertices[i];
                    Vector3 pt2 = Vertices[j];
                    double d = Math.Sqrt(Math.Pow((pt1.X - pt2.X), 2) + Math.Pow((pt1.Y - pt2.Y), 2) + Math.Pow((pt1.Z - pt2.Z), 2));
                    if (d == 2)
                    {
                        ConnectedPoints.Add(new Vector2(i, j));
                    }
                }
            }

            Name = name;
        }
    }
}
