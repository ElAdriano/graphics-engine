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
            if (v.Count < 3)
            {
                throw new Exception("Niewystarczajaca liczba wierzcholkow by stworzyc plaszczyzne");
            }
            Vertices = v;
        }

        public List<LineEquation> CalculateEquationsForEdges()
        {
            List<LineEquation> Equations = new List<LineEquation>();
            Vector3 pointer, tmp;
            for(int i = 0; i < Vertices.Count; i++)
            {
                pointer = Vertices[i];
                tmp = Vertices[(i + 1) % Vertices.Count];

                //Console.WriteLine(pointer.ToString());
                //Console.WriteLine(tmp.ToString());

                Vector3 diffV = pointer - tmp;
                Equations.Add(new LineEquation(pointer, diffV));
            }
            return Equations;
        }
    }
}
