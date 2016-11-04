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

        LogModeType logMode = LogModeType.Detail;
        private RtccConfigs configs;

        private LogModeType LogMode
        {
            get { return logMode; }
            set
            {
                logMode = value;

                configs.DetailedLogging = (LogModeType.Detail == value);

                // Only check one valid option.
                switch (logMode)
                {
                    case LogModeType.Detail:
                        toolStripDropDownButton1.Text = "Detailed logging enabled";

                        detailedLoggingToolStripMenuItem.Checked = true;
                        importantLoggingOnlyToolStripMenuItem.Checked = false;
                        noLoggingToolStripMenuItem.Checked = false;
                        break;

                    case LogModeType.Important:
                        toolStripDropDownButton1.Text = "Important logging only";

                        detailedLoggingToolStripMenuItem.Checked = false;
                        importantLoggingOnlyToolStripMenuItem.Checked = true;
                        noLoggingToolStripMenuItem.Checked = false;
                        break;

                    default:
                        toolStripDropDownButton1.Text = "No logging temporarily";

                        detailedLoggingToolStripMenuItem.Checked = false;
                        importantLoggingOnlyToolStripMenuItem.Checked = false;
                        noLoggingToolStripMenuItem.Checked = true;
                        break;
                }
            }
        }

        /// <summary>
        /// The main form of the project
        /// </summary>
        internal RtccForm(string applicationName, RtccMain rtccMain, RtccConfigs configs)
        {
            InitializeComponent();
            this.configs = configs;
            this.rtccMain = rtccMain;
            this.rtccMain.Logged += RtccLogged;
            this.rtccMain.TransactionDone += TransactionDone;
            Text = applicationName + " - " + ProductVersion.ToString();
            toolStripStatusLabel1.Text = "Waiting...";

            LogMode = configs.DetailedLogging ? LogModeType.Detail : LogModeType.Important;
        }

        private void RtccLogged(object sender, LogEventArgs args)
        {
            // Log to screen
            if ((LogModeType.None != LogMode)
                && ((LogModeType.Detail == LogMode)
                || (args.Level > LogLevel.Detail)))
            {
                string text = DateTime.Now.ToString() + ":: " + args.Message;
                if (args.Level == LogLevel.Error)
                    text = "*******************************************************************************" + Environment.NewLine + text;
                text += Environment.NewLine;
                if (args.Exception != null)
                    text += args.Exception.ToString() + Environment.NewLine;

                // STM-25 Only keep the last thousand lines. If not then the 
                // log will grow continuously and consume memory.
                Action append = () =>
                    {
                        int keepLinesCount = 1000;

                        textBox1.AppendText(text);

                        if ((LogModeType.Important == LogMode)
                            && (textBox1.Lines.Length > keepLinesCount))
                        {
                            textBox1.Lines = textBox1.Lines.Skip(textBox1.Lines.Length - keepLinesCount).ToArray();

                            textBox1.SelectionStart = textBox1.Text.Length;
                            textBox1.ScrollToCaret();
                        }
                    };

                if (textBox1.IsHandleCreated || textBox1.InvokeRequired)
                {
                    textBox1.Invoke(append);
                }
                else
                {
                    append.Invoke();
                }
            }
        }

        private void TransactionDone(object sender, TransactionDoneEventArgs args)
        {
            Interlocked.Increment(ref total);
            if (!args.Success)
                Interlocked.Increment(ref totalFail);

            toolStripStatusLabel1.Text = totalFail + " failed of " + total + " total";
        }

        private void RtccForm_Load(object sender, EventArgs e)
        {
            rtccMain.StartListening();
        }

        private void importantLoggingOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogMode = LogModeType.Important;
        }

        private void detailedLoggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogMode = LogModeType.Detail;
        }

        // STM-25 Add option for "pausing" the logging. This then allows
        // users to scroll through the log window.
        private void noLoggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogMode = LogModeType.None;
        }

        enum LogModeType
        {
            None,
            Important,
            Detail
        }
    }
}
