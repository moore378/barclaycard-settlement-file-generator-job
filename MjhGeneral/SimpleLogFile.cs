using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MjhGeneral
{
    public class SimpleLogFile : IDisposable
    {
        private StreamWriter outputStream;
        private bool prependTimestamp;

        public SimpleLogFile(string fileName, bool prependTimestamp)
        {
            this.prependTimestamp = prependTimestamp;
 
            string directory = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            outputStream = new StreamWriter(fileName, true);
        }

        public void Log(string msg)
        {
            lock (outputStream)
            {
                if (prependTimestamp)
                    outputStream.WriteLine(DateTime.Now.ToString() + ":: " + msg);
                else
                    outputStream.WriteLine(msg);
            }
            
        }

        public void Dispose()
        {
            outputStream.Close();
            outputStream.Dispose();
        }
    }
}
