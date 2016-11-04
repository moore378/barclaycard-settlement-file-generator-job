using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using Rtcc.Main;
using System.IO;

using MjhGeneral.ServiceProcess;

namespace Rtcc
{
    [WindowsService(Name = Constants.ApplicationName, Description = Constants.Description)]
    partial class RtccService : ServiceBase
    {
        private RtccMain rtccMain;
        private RtccConfigs configs;

        public RtccService()
        {
            // Make sure the service runs in the same directory as the executable.
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            InitializeComponent();
        }

        public RtccService(RtccMain rtccMain, RtccConfigs configs):this()
        {
            this.rtccMain = rtccMain;
            this.configs = configs;
        }

        protected override void OnStart(string[] args)
        {
            //Debugger.Launch();
            rtccMain.StartListening();
        }

        protected override void OnStop()
        {
            // Force a close...
            rtccMain = null;
        }
    }
}
