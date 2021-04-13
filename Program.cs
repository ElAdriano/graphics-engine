using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                /*VirtualCamera.AddObject(new Object3D("FirstCube", new SharpDX.Vector3(-2, 0, -10)));
                VirtualCamera.AddObject(new Object3D("SecondCube", new SharpDX.Vector3(2, 0, -10)));
                VirtualCamera.AddObject(new Object3D("ThirdCube", new SharpDX.Vector3(2, 0, 10)));
                VirtualCamera.AddObject(new Object3D("FourthCube", new SharpDX.Vector3(-2, 0, 10)));*/
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
                    newObj = new Object3D(obj.Name, obj.Position, obj.Walls);
                    camera.AddObject(newObj);
                }
            }
        }
    }
}
