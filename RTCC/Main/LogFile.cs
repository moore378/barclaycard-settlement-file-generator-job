using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TransactionManagementCommon;
using System.Threading;
using System.IO;

namespace Rtcc.Main
{
    class LogFile
    {
        Mutex fileLogLock = new Mutex();
        string[] newLineSeperator = new string[] { Environment.NewLine };
        string pathPrefix;
        string pathSuffix;
        StreamWriter currentFile;
        string currentFileName;

        public LogFile(string pathPrefix, string pathSuffix)
        {
            this.pathPrefix = pathPrefix;
            this.pathSuffix = pathSuffix;
        }

        public void Log(string msg)
        {
            fileLogLock.WaitOne();
            try
            {
                var fileStream = GetCurrentFile();
                
                string timeString = DateTime.Now.ToString("HH:mm:ss");
                
                foreach (var line in msg.Split(newLineSeperator, StringSplitOptions.None))
                    fileStream.WriteLine(timeString + " :: " + line);
            }
            finally
            {
                fileLogLock.ReleaseMutex();
            }
        }

        private StreamWriter GetCurrentFile()
        {
            string intendedFileName = MakeFileName();
            // Need to change files?
            if (intendedFileName != currentFileName)
            {
                currentFileName = intendedFileName;

                if (currentFile != null)
                {
                    currentFile.Close();
                    currentFile.Dispose();
                    currentFile = null;
                }
                else if (!Directory.Exists("logs"))
                    Directory.CreateDirectory("logs");

                currentFile = new StreamWriter(currentFileName, true, Encoding.ASCII);
            }

            return currentFile;           
        }

        private string MakeFileName()
        {
            string dateString = DateTime.Now.ToString("yyyy'_'MM'_'dd'_'HH");
            return pathPrefix + dateString + pathSuffix;
        }
    }
}
