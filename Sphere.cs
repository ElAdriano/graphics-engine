using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace VirtualCamera
{
    //sphere (x-a)^2 + (y-b)^2 + (z-c)^2 = r^2
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

        public Vector3 getPoint(float x, float y)
        {
            //z = sqrt(r^2 - (x-a)^2 - (y-b)^2) + c
            float z2c = (float)(Math.Pow(R, 2) - Math.Pow((x - Origin.X), 2) - Math.Pow((y - Origin.Y), 2));
            float z = (float)Math.Sqrt(z2c) + Origin.Z;
            return new Vector3(x, y, z);
        }
    }
}
