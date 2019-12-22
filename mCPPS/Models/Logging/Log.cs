using System;

namespace mCPPS.Models
{
    public class Log
    {
        public string Timestamp { get; set; }
        public string Type { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }

        public Log(string type, string source, string message)
        {
            Timestamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
            this.Type = type;
            this.Source = source;
            this.Message = message;  
        }

        public override string ToString()
        {
            return string.Format("[{0}] [{1}] >> {2}", Timestamp, Type, Message);
        }

        private static DateTime TimestampToDateTime(int timestamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return dtDateTime.AddSeconds(timestamp).ToLocalTime();
        }
    }
}
