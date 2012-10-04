using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using Rtcc.Main;

namespace Rtcc
{
    partial class RtccService : ServiceBase
    {
        private RtccMain rtccMain;
        private RtccConfigs configs;

        public RtccService()
        {
            InitializeComponent();
        }

        public RtccService(RtccMain rtccMain, RtccConfigs configs)
        {
            this.rtccMain = rtccMain;
            this.configs = configs;
        }

        protected override void OnStart(string[] args)
        {
            Debugger.Launch();
            rtccMain.StartListening();
        }

        protected override void OnStop()
        {
        }
    }
}
