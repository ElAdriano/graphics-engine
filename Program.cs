using System;
using System.Collections.Generic;
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
                VirtualCamera.AddObject(new Object3D("FirstCube", new SharpDX.Vector3(-2, 0, -10)));
                VirtualCamera.AddObject(new Object3D("SecondCube", new SharpDX.Vector3(2, 0, -10)));
                VirtualCamera.AddObject(new Object3D("ThirdCube", new SharpDX.Vector3(2, 0, 10)));
                VirtualCamera.AddObject(new Object3D("FourthCube", new SharpDX.Vector3(-2, 0, 10)));

                VirtualCamera.Run();
            }
        }
    }
}
