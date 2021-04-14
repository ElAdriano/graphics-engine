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
        public float[] SurfaceCoefficients;
        public Wall(List<Vector3> v)
        {
            if (v.Count < 3)
            {
                throw new Exception("Niewystarczajaca liczba wierzcholkow by stworzyc plaszczyzne");
            }
            Vertices = v;
            FindSurface();
        }

        public void FindSurface()
        {
            SurfaceCoefficients = new float[4];
            Vector3 FirstVector = Vertices[0] - Vertices[1];
            Vector3 SecondVector;
            bool NonLinearVectorsFound = false;
            for(int i = 2; i < Vertices.Count; i++)
            {
                SecondVector = Vertices[0] - Vertices[i];
                NonLinearVectorsFound = !AreLinearVectors(FirstVector, SecondVector);
                if (NonLinearVectorsFound)
                {
                    CalculateCoefficients(Vertices[0], FirstVector, SecondVector);
                    break;
                }
            }
            if (!NonLinearVectorsFound)
            {
                throw new Exception("All vectors are linear");
            }
        }

        private void CalculateCoefficients(Vector3 includedPoint, Vector3 v1, Vector3 v2)
        {
            SurfaceCoefficients[0] = v1.Y * v2.Z - v2.Y * v1.Z;
            SurfaceCoefficients[1] = v2.X * v1.Z - v1.X * v2.Z;
            SurfaceCoefficients[2] = v1.X * v2.Y - v2.X * v1.Y;
            SurfaceCoefficients[3] = -(SurfaceCoefficients[0] * includedPoint.X + SurfaceCoefficients[1] * includedPoint.Y + SurfaceCoefficients[2] * includedPoint.Z);
        }

        private bool AreLinearVectors(Vector3 v1, Vector3 v2)
        {
            double ScalarMulValue = v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
            v1.LengthSquared();
            v2.LengthSquared();
            if ( ScalarMulValue / (v1.LengthSquared() * v2.LengthSquared()) == 1){ // cosinus value equal 1 => angle between vectors is 0 degrees
                return true;
            }
            return false;
        }
    }
}
