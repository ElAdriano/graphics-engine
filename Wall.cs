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
        public float[] PlaneCoefficients; // world view

        public List<Vector3> TwoDimentionalBorders;

        public List<Tuple<int, float, float, float, float>> CastEquations;

        public Wall(List<Vector3> v)
        {
            if (v.Count < 3)
            {
                throw new Exception("Niewystarczajaca liczba wierzcholkow by stworzyc plaszczyzne");
            }
            Vertices = v;
            FindPlanes();
            TwoDimentionalBorders = new List<Vector3>();
            CastEquations = new List<Tuple<int, float, float, float, float>>();
        }

        /*
         Struktura rownania
        Tuple<int, float, float, float, float>(typ, a, b, range_start, range_end)
        typ - typ rownania (0 dla krzywej linii, 1 dla linii pionowej, 2 dla linii poziomej)
        a   - wspolczynnik kierunkowy prostej
        b   - wyraz wolny prostej o rownaniu y = ax + b
        range_start  - ograniczenie dolne zakresu rozwiazan (wlacznie) - dla linii pionowej jest to zakres y, dla innych jest to zakres x
        range_end  - ograniczenie gorne zakresu rozwiazan (wlacznie) - dla linii pionowej jest to zakres y, dla innych jest to zakres x
         */
        public void FindCast2DEquations()
        {
            CastEquations.Clear();
            Vector3 p1, p2;
            for(int i = 0; i < TwoDimentionalBorders.Count(); i++)
            {
                p1 = TwoDimentionalBorders[i];
                p2 = TwoDimentionalBorders[(i + 1) % TwoDimentionalBorders.Count()];
                if (p1.X == p2.X)
                {
                    CastEquations.Add(new Tuple<int, float, float, float, float>(1, p1.X, 0, Math.Min(p1.Y, p2.Y), Math.Max(p1.Y, p2.Y)));
                }
                else
                {
                    float a = (p1.Y - p2.Y) / (p1.X - p2.X);
                    float b = p1.Y - a * p1.X;
                    if (a == 0)
                    {
                        CastEquations.Add(new Tuple<int, float, float, float, float>(2, a, b, Math.Min(p1.X, p2.X), Math.Max(p1.X, p2.X))); // rownanie dla castu
                    }
                    else
                    {
                        CastEquations.Add(new Tuple<int, float, float, float, float>(0, a, b, Math.Min(p1.X, p2.X), Math.Max(p1.X, p2.X))); // rownanie dla castu 
                    }
                }
            }
        }

        /*public Vector3 GetNearestCastedPoint(int pixelX, int pixelY)
        {
            int nearestIndex = 0;
            double nearestDistance = (float)Math.Sqrt(Math.Pow(TwoDimentionalBorders[0].X - pixelX, 2) + Math.Pow(TwoDimentionalBorders[0].Y - pixelY, 2));
            for(int i = 1; i < TwoDimentionalBorders.Count(); i++)
            {
                double tmp = (float)Math.Sqrt(Math.Pow(TwoDimentionalBorders[i].X - pixelX, 2) + Math.Pow(TwoDimentionalBorders[i].Y - pixelY, 2));
                if (tmp < nearestDistance)
                {
                    nearestDistance = tmp;
                    nearestIndex = i;
                }
            }

            return TwoDimentionalBorders[nearestIndex];
        }*/

        public int GetVertex(bool nearest)
        {
            if (nearest)
            {
                double height = double.MaxValue;
                for(int i = 0; i < Vertices.Count(); i++)
                {
                    height = Math.Min(Vertices[i].Y, height);
                }

                int result = 0;
                for (int i = 1; i < Vertices.Count(); i++)
                {
                    if (Vertices[i].Z > Vertices[result].Z && Vertices[i].Y == height)
                    {
                        result = i;
                    }
                }
                return result;
            }
            else
            {
                double height = double.MaxValue;
                for (int i = 0; i < Vertices.Count(); i++)
                {
                    height = Math.Min(Vertices[i].Y, height);
                }
                int result = 0;
                for (int i = 1; i < Vertices.Count(); i++)
                {
                    if (Vertices[i].Z < Vertices[result].Z && Vertices[i].Y == height)
                    {
                        result = i;
                    }
                }
                return result;
            }
        }

        public void FindPlanes()
        {
            PlaneCoefficients = new float[4];
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
            PlaneCoefficients[0] = v1.Y * v2.Z - v2.Y * v1.Z;
            PlaneCoefficients[1] = v2.X * v1.Z - v1.X * v2.Z;
            PlaneCoefficients[2] = v1.X * v2.Y - v2.X * v1.Y;
            PlaneCoefficients[3] = -(PlaneCoefficients[0] * includedPoint.X + PlaneCoefficients[1] * includedPoint.Y + PlaneCoefficients[2] * includedPoint.Z);
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
