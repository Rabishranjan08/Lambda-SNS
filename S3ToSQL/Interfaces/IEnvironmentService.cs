using System;
using System.Collections.Generic;
using System.Text;

namespace S3ToSQL.Interfaces
{
   public interface IEnvironmentService
    {
        string EnvironmentName { get; set; }
    }
}
