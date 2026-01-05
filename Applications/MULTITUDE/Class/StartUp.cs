using MULTITUDE.Canvas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class
{
    class StartUp
    {
        // Non-single instanced
        //[STAThread()]
        //static void Main()
        //{
        //    // Create the application.
        //    App app = new App();
        //    app.InitializeComponent();
        //    // Launch the application
        //    app.Run();
        //}

        // Single instanced
        [STAThread()]
        static void Main(string[] args)
        {
            SingleInstanceApp app = new SingleInstanceApp();
            app.Run(args);
        }
    }
}