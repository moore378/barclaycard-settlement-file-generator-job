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

        public LogFile(string pathPrefix, string pathSuffix)
        {
            this.pathPrefix = pathPrefix;
            this.pathSuffix = pathSuffix;

            // Create the directory if it doesn't exist.
            string path = Path.GetDirectoryName(pathPrefix);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void Log(string msg)
        {
            fileLogLock.WaitOne();
            try
            {
                // Write to the file and ensure that the buffering are flushed.
                // Note that this is similar to File.AppendAllLines().
                using (var fileStream = GetCurrentFile())
                {
                    string timeString = DateTime.Now.ToString("HH:mm:ss");

                    foreach (var line in msg.Split(newLineSeperator, StringSplitOptions.None))
                        fileStream.WriteLine(timeString + " :: " + line);
                }
            }
            finally
            {
                fileLogLock.ReleaseMutex();
            }
        }

        private StreamWriter GetCurrentFile()
        {
            StreamWriter currentFile;

            string intendedFileName = MakeFileName();

            currentFile = new StreamWriter(intendedFileName, true, Encoding.ASCII);

            return currentFile;           
        }

        private string MakeFileName()
        {
            string dateString = DateTime.Now.ToString("yyyy'_'MM'_'dd'_'HH");
            return pathPrefix + dateString + pathSuffix;
        }
    }
}
