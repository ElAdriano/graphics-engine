using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCamera
{
    public class LineEquation
    {
        public Vector3 StartPoint;
        public Vector3 Deltas;
        public int ObjID;
        public int WallID;

        public LineEquation(Vector3 StartPoint, Vector3 ValuesForT, int objectId, int wall)
        {
            this.StartPoint = StartPoint;
            this.Deltas = ValuesForT;
            this.ObjID = objectId;
            this.WallID = wall;
        }
    }
}
