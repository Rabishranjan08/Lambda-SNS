using S3ToSQL.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using static S3ToSQL.Constants;

namespace S3ToSQL.Services
{
    public class EnvironmentService : IEnvironmentService
    {
        public string EnvironmentName { get; set; }
        public EnvironmentService()
        {
            EnvironmentName = Environment.GetEnvironmentVariable(EnvironmentVariables.AspnetCoreEnvironment)
                ?? Environments.Production;
        }
    }
}
