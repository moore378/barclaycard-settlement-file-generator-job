#undef useDummyServers

using System;
using System.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using CCCrypto;
using AuthorizationClientPlatforms;
using System.Runtime.InteropServices;
using Rtcc.Main;
using TransactionManagementCommon;
using TransactionManagementCommon.ControllerBase;
using Rtcc.RtsaInterfacing;

namespace Rtcc
{
    /// <summary>
    /// The main form
    /// </summary>
    public partial class RtccForm : Form
    {
        private RtccMain rtccMain;
        int total = 0;
        int totalFail = 0;

        bool logDetail = false;
        private RtccConfigs configs;
        private bool LogDetail
        {
            get { return logDetail; }
            set
            {
                logDetail = value;
                configs.DetailedLogging = value;
                importantLoggingOnlyToolStripMenuItem.Checked = !logDetail;
                detailedLoggingToolStripMenuItem.Checked = logDetail;
                toolStripDropDownButton1.Text = logDetail ? "Detailed logging enabled" : "Important logging only";
            }
        }

        /// <summary>
        /// The main form of the project
        /// </summary>
        internal RtccForm(RtccMain rtccMain, RtccConfigs configs)
        {
            InitializeComponent();
            this.configs = configs;
            this.rtccMain = rtccMain;
            this.rtccMain.Logged += RtccLogged;
            this.rtccMain.TransactionDone += TransactionDone;
            Text += " - " + ProductVersion.ToString();
            toolStripStatusLabel1.Text = "Waiting...";
            LogDetail = configs.DetailedLogging;
        }

        private void RtccLogged(object sender, LogEventArgs args)
        {
            // Log to screen
            if (logDetail || args.Level > LogLevel.Detail)
            {
                string text = DateTime.Now.ToString() + ":: " + args.Message;
                if (args.Level == LogLevel.Error)
                    text = "*******************************************************************************" + Environment.NewLine + text;
                text += Environment.NewLine;
                if (args.Exception != null)
                    text += args.Exception.ToString() + Environment.NewLine;

                if (textBox1.IsHandleCreated || textBox1.InvokeRequired)
                    textBox1.Invoke(new Action(() => { textBox1.AppendText(text); }));
                else
                    textBox1.AppendText(text);
            }
        }

        private void TransactionDone(object sender, TransactionDoneEventArgs args)
        {
            Interlocked.Increment(ref total);
            if (!args.Success)
                Interlocked.Increment(ref totalFail);

            toolStripStatusLabel1.Text = totalFail + " failed of " + total + " total";
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            //LogDetail = checkBox1.Checked;
        }

        private void RtccForm_Load(object sender, EventArgs e)
        {
            rtccMain.StartListening();
        }

        private void importantLoggingOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogDetail = false;
        }

        private void detailedLoggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogDetail = true;
        }
    }
}
