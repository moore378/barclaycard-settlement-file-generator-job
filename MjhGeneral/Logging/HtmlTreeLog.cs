using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Logging
{
    public class HtmlTreeLog : ITreeLog
    {
        private List<LogEntry> entries = new List<LogEntry>();
        private string path;
        private Counter counter;

        public HtmlTreeLog(string path)
        {
            this.path = path;
            this.counter = new Counter();
        }

        [DebuggerNonUserCode]
        public void Log(string msg)
        {
            entries.Add(new TextLogEntry { msg = msg, timestamp = DateTime.Now, seq = Interlocked.Increment(ref counter.value) });
            Console.WriteLine(msg);
        }

        [DebuggerNonUserCode]
        public ITreeLog CreateChild(string childName)
        {
            Console.WriteLine(childName);
            ChildLogEntry newEntry = new ChildLogEntry(childName, counter);
            entries.Add(newEntry);
            return newEntry;
        }

        public void Dispose()
        {
            Save();
        }

        private void Save()
        {
            using (var f = new StreamWriter(path))
            {
                f.WriteLine("<!DOCTYPE html><html><head><link rel=\"stylesheet\" type=\"text/css\" href=\"style.css\"/></head><body>");
                foreach (var entry in entries)
                    entry.WriteHtml(f);
                f.WriteLine("</body></html>");
            }
        }

        private class ChildLogEntry : LogEntry, ITreeLog
        {
            private List<LogEntry> entries;
            private string name;
            private int seq;
            private Counter counter;

            [DebuggerNonUserCode]
            public ChildLogEntry(string childName, Counter counter)
            {
                this.seq = Interlocked.Increment(ref counter.value);
                this.counter = counter;
                this.name = childName;
                this.entries = new List<LogEntry>();
            }

            [DebuggerNonUserCode]
            public void Log(string msg)
            {
                entries.Add(new TextLogEntry { msg = msg, timestamp = DateTime.Now, seq = Interlocked.Increment(ref counter.value) });
                Console.WriteLine(msg);
            }

            [DebuggerNonUserCode]
            public ITreeLog CreateChild(string childName)
            {
                Console.WriteLine(childName);
                ChildLogEntry newEntry = new ChildLogEntry(childName, counter);
                entries.Add(newEntry);
                return newEntry;
            }

            public void Dispose()
            {
            }

            public override void WriteHtml(StreamWriter f)
            {
                f.WriteLine("<div><span class=\"seq\">" + seq + "</span>" + name + "</div>");
                f.WriteLine("<div>");
                foreach (var entry in entries)
                    entry.WriteHtml(f);
                f.WriteLine("</div>");
            }
        }

        private abstract class LogEntry
        {
            public abstract void WriteHtml(StreamWriter f);
        }

        private class TextLogEntry : LogEntry
        {
            public string msg;
            public DateTime timestamp;
            public int seq;

            public override void WriteHtml(StreamWriter f)
            {
                f.WriteLine("<div><span class=\"seq\">" + seq + "</span>" + msg + "</div>");
            }
        }

        private class Counter
        {
            public int value;
        }
    }
}
