using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCamera
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (Camera VirtualCamera = new Camera())
            {
                LoadObjects(VirtualCamera);
                VirtualCamera.Run();
            }
        }

        static void LoadObjects(Camera camera)
        {
            using (StreamReader reader = new StreamReader("../../przenikanie.json"))
            {
                string jsonContent = reader.ReadToEnd();
                ObjectJsonFileStructure parsedContent = JsonConvert.DeserializeObject<ObjectJsonFileStructure>(jsonContent);
                Object3D newObj;
                foreach (ObjectStructure obj in parsedContent.objects)
                {
                    try
                    {
                        newObj = new Object3D(obj.Name, obj.Position, obj.Walls, obj.Color);
                        camera.AddObject(newObj);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                //DevTests(new Vector3(2, 4, 1), new Vector3(-2,3,1), new Vector3(1,-4,2)); // -x + 4y + 31z -45 = 0
                //DevTests(new Vector3(3,2,1), new Vector3(2,-2,4), new Vector3(1,-4,2));   // 14x - 5y - 2z - 30 = 0
                //DevTests(new Vector3(3,1,1), new Vector3(1,-1,2), new Vector3(3,-1,2));   // 2y + 4z - 6 = 0
            }
        }

        static void DevTests(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            List<Vector3> tmp = new List<Vector3>();

            tmp.Add(v1);
            tmp.Add(v2);
            tmp.Add(v3);
            Wall wall = new Wall(tmp);
            wall.FindPlanes();
            Console.WriteLine("Surface equation: {0}x {1}y {2}z {3}", wall.PlaneCoefficients[0], wall.PlaneCoefficients[1], wall.PlaneCoefficients[2], wall.PlaneCoefficients[3]);
        }
    }
}
