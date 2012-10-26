using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Windows.Forms;
using System.Linq;

namespace Cctm
{
  static class Program
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Contains("gui"))
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CctmForm(true, args.Contains("extended_database_logging")));
        }
        else
        {
            ServiceBase.Run(new CctmService());
        }
    }
  }
}