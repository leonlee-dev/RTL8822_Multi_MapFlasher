using RTKModule;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MapFlasher
{
    public class Log : ILog
    {
        private string path;
        private StringBuilder sb;

        public Log(int defaultBuffer = 1024 * 1024 * 1)
        {
            sb = new StringBuilder(defaultBuffer);
        }

        public string Read()
        {
            throw new NotImplementedException();
        }

        public void Write(string text)
        {
            sb.Append(text);
        }

        public void WriteLine(string text)
        {
            sb.AppendLine(text);
        }

        public void Clear()
        {
            sb.Length = 0;
        }

        public void Save(string path)
        {
            this.path = path;
            File.WriteAllText(path, sb.ToString());
        }

        public void AppendFile(string path, string content)
        {
            throw new NotImplementedException();
        }
    }
}
