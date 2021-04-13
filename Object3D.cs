﻿using System;
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

        public Object3D(string name, Vector3 Position, List<List<Vector3>> Walls)
        {
            this.Name = name;
            this.Position = Position;
            Rotation = new Vector3(0, 0, 0);
            this.Walls = new List<Wall>();

            foreach(List<Vector3> WallVertices in Walls)
            {
                this.Walls.Add( new Wall(WallVertices) );
            }
        }
    }
}
