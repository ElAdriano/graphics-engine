using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace VirtualCamera
{
    public class Sphere
    {
        public Vector3 Origin;
        public float R, AmbientIntensity, Brightness;
        public Color4 Color;
        public Vector3 Ambient;

        public Sphere(Vector3 origin, float r, Color4 color)
        {
            Origin = origin;
            R = r;
            Color = color;
        }
        public Vector3 Normal(Vector3 point)
        {
            Vector3 normal = point - Origin;
            normal.Normalize();
            return normal;
        }
    }
}
