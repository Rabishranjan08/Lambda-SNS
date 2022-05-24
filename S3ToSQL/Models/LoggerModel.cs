using System;
using System.Collections.Generic;
using System.Text;

namespace S3ToSQL.Models
{
    public class LoggerModel
    {

        public string LogLevel { get; set; }
        public long? EmailNotificationId { get; set; }
        public string Message { get; set; }
        public string MemberName { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public string AWSRequestId { get; set; }
    }
}
