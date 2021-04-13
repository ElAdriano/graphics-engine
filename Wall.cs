using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCamera
{
    public class Wall
    {
        public List<Vector3> Vertices;

        public Wall(List<Vector3> v)
        {
            this.Vertices = v;
        }
    }
}
