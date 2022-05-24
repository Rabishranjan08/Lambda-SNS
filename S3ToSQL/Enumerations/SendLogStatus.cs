using System;
using System.Collections.Generic;
using System.Text;

namespace S3ToSQL.Enumerations
{
    public enum SendLogStatus
    {
        New = 10,
        InProgress = 20,
        Successful = 30,
        Updated = 40,
        Failed = 50
    }

}
