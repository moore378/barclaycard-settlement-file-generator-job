using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;

namespace Cctm
{
    partial class CctmService : ServiceBase
    {
        CctmForm hiddenForm;

        public CctmService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //Debugger.Launch();
            hiddenForm = new CctmForm(false, false);
            hiddenForm.InitializeHidden();
        }

        protected override void OnStop()
        {
            hiddenForm.CctmForm_FormClosing(null, new FormClosingEventArgs(CloseReason.UserClosing, false));
        }
    }
}
