using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Windows.Forms;

namespace Rtcc.Main
{
    class RtccConfigs
    {
        bool detailedLogging;

        public string MonetraHostName { get; private set; }
        public ushort MonetraPort { get; private set; }
        public bool DetailedLogging
        { 
            get { return detailedLogging; }
            set
            {
                detailedLogging = value;
                Rtcc.Properties.Settings.Default.DetailedLoggingEnabled = detailedLogging;
                Rtcc.Properties.Settings.Default.Save();
            }
        }
                
        public void LoadFromFile()
        {
            try
            {
                MonetraHostName = Rtcc.Properties.Settings.Default.MontraHostName;
                MonetraPort = Rtcc.Properties.Settings.Default.MonetraPort;
                detailedLogging = Rtcc.Properties.Settings.Default.DetailedLoggingEnabled;
            }
            catch (NullReferenceException e)
            {
                MessageBox.Show("Unable to read configs from RTCC.exe.config. " + e.Message);
            }
        }
    }
}
