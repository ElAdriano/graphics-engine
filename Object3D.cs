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
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }

        public List<Wall> Walls;

        public SharpDX.Color Color;

        public Object3D(string name, Vector3 Position, List<List<Vector3>> Walls, SharpDX.Color Color)
        {
            this.Name = name;
            this.Position = Position;
            Rotation = new Vector3(0, 0, 0);
            this.Walls = new List<Wall>();
            this.Color = Color;

            foreach(List<Vector3> WallVertices in Walls)
            {
                try
                {
                    this.Walls.Add(new Wall(WallVertices));
                } catch(Exception e)
                {
                    throw new Exception(e.Message);
                }
            }
        }
    }
}
