using Amazon.Lambda.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace S3ToSQL.Interfaces
{
  public  interface ILoggerService
    {

        void SetContext(ILambdaContext context);
        void LogMessage(string message, [CallerMemberName] string memberName = "");
        //void Info(string message, [CallerMemberName] string memberName = "");
        //void Debug(long emailNotificationId, string message, [CallerMemberName] string memberName = "");
        //void Warn(string message, [CallerMemberName] string memberName = "");
        //void Error(long emailNotificationId, string message, [CallerMemberName] string memberName = "");

    }
}
