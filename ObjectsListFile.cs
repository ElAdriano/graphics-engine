using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCamera
{
    public class ObjectStructure
    {
        public string Name;
        public Vector3 Position;
        public SharpDX.Color Color;
        public List<List<Vector3>> Walls;
    }

    public class ObjectJsonFileStructure
    {
        public List<ObjectStructure> objects;
    }
}
