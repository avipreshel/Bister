using System.Diagnostics;
using System.Text;

namespace BisterLib
{
    public class StringBuilderVerbose
    { 
        StringBuilder _sb = new StringBuilder();
        bool _debugMode = false;

        public int Length => _sb.Length;

        public StringBuilderVerbose(bool debugMode = false)
        {
            _debugMode = debugMode;
        }

        public void AppendLine(string line)
        {
            Debug.WriteLine(line);
            _sb.AppendLine(line);
        }

        public void Append(string txt)
        {
            _sb.Append(txt);
        }

        public void Replace(string oldVal,string newVal)
        {
            _sb.Replace(oldVal, newVal);
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    }
}
