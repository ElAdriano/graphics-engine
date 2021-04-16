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
            using (StreamReader reader = new StreamReader("objects.json"))
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
            }
        }
    }
}
