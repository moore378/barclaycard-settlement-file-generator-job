using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Cctm
{
    public partial class EventLogForm : Form
    {
        public EventLogForm()
        {
            InitializeComponent();
        }

        public void log(string message)
        {
            if (txtEventLog.InvokeRequired)
            {
                txtEventLog.Invoke(new Action(()=>txtEventLog.AppendText(message + Environment.NewLine)));
            }
            else
            {
                txtEventLog.AppendText((string)message + Environment.NewLine);
            }
        }

        private void EventLogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
