using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class
{
    // Single-Instance application wrapper for WPF application
    public class SingleInstanceApp : Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase
    {
        public SingleInstanceApp()
        {
            IsSingleInstance = true;
        }

        // Create the WPF application class.
        private App app;
        protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs e)
        {
            app = new App();
            app.InitializeComponent();
            app.Run();
            return false;
        }

        // Direct multiple instances.
        protected override void OnStartupNextInstance(Microsoft.VisualBasic.ApplicationServices.StartupNextInstanceEventArgs e)
        {
            if (e.CommandLine.Count > 0)
            {
                app.NewStartRequest(e.CommandLine);
            }
        }
    }
}
