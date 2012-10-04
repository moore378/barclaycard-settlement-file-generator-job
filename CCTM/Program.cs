using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Windows.Forms;

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
        if (args.Length == 1 && args[0] == "gui")
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CctmForm(true));
        }
        else
        {
            ServiceBase.Run(new CctmService());
        }
    }
  }
}