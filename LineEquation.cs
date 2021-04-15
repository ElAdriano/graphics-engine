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

        public LineEquation(Vector3 StartPoint, Vector3 ValuesForT)
        {
            this.StartPoint = StartPoint;
            this.Deltas = ValuesForT;
        }
    }
}
