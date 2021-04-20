using System;
using CsvHelper.Configuration.Attributes;

namespace BobRfid
{
    class LogRecord
    {
        [Index(0)]
        public string DateTime { get; set; }

        [Index(1)]
        public string LogLevel { get; set; }
        
        [Index(2)]
        public string Logger { get; set; }
        
        [Index(3)]
        public string Message { get; set; }
    }
}
