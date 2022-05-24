using Amazon.Lambda.Core;
using Newtonsoft.Json;
using S3ToSQL.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace S3ToSQL.Services
{
    public class LoggerService : ILoggerService
    {
        public static ILambdaContext _context;
        public void LogMessage(string message, [CallerMemberName] string memberName = "")
        {
            var logMessage = new LogMessage
            {
                AWSRequestId = _context.AwsRequestId,
                MemberName = memberName,
                LambdaMessage = message
            };
            LambdaLogger.Log(JsonConvert.SerializeObject(logMessage));
        }

        public void SetContext(ILambdaContext context)
        {
            _context = context;
        }
    }

    public class LogMessage
    {
        public string LambdaMessage { get; set; }
        public string MemberName { get; set; }
        public string AWSRequestId { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
    //    public static ILambdaContext _context;
    //    public LoggerService()
    //    {

    //    }

    //    public void SetContext(ILambdaContext context)
    //    {
    //        _context = context;
    //    }

    //    public void Info(string message, [CallerMemberName] string memberName = "")
    //    {
    //        LoggerModel loggerModel = new LoggerModel()
    //        {
    //            LogLevel = LogLevel.Info.ToString(),
    //            Message = message,
    //            MemberName = memberName,
    //            AWSRequestId = _context?.AwsRequestId
    //        };

    //        LambdaLogger.Log(JsonConvert.SerializeObject(loggerModel));
    //    }
    //    public void Debug(long emailNotificationId, string message, [CallerMemberName] string memberName = "")
    //    {
    //        LoggerModel loggerModel = new LoggerModel()
    //        {
    //            LogLevel = LogLevel.Debug.ToString(),
    //            EmailNotificationId = emailNotificationId,
    //            Message = message,
    //            MemberName = memberName,
    //            AWSRequestId = _context?.AwsRequestId
    //        };

    //        LambdaLogger.Log(JsonConvert.SerializeObject(loggerModel));
    //    }
    //    public void Warn(string message, [CallerMemberName] string memberName = "")
    //    {
    //        LoggerModel loggerModel = new LoggerModel()
    //        {
    //           // LogLevel = LogLevel.Warn.ToString(),
    //            Message = message,
    //            MemberName = memberName,
    //            AWSRequestId = _context?.AwsRequestId
    //        };

    //        LambdaLogger.Log(JsonConvert.SerializeObject(loggerModel));
    //    }
    //    public void Error(long emailNotificationId, string message, [CallerMemberName] string memberName = "")
    //    {
    //        LoggerModel loggerModel = new LoggerModel()
    //        {
    //            LogLevel = LogLevel.Error.ToString(),
    //            EmailNotificationId = emailNotificationId,
    //            Message = message,
    //            MemberName = memberName,
    //            AWSRequestId = _context?.AwsRequestId
    //        };

    //        LambdaLogger.Log(JsonConvert.SerializeObject(loggerModel));
    //    }
    //}
}
