using System;
using System.Collections.Generic;
using System.Text;

namespace S3ToSQL
{
    public static class Constants
    {
        public static class EnvironmentVariables
        {
            public const string AspnetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";
            public const string Database_Secret = "DB_SECRET_ARN";
        }

        public static class Environments
        {
            public const string Production = "Production";
        }

        public static class DefaultValues
        {
            public const decimal DecimalValue = 0.00M;
        }
    }
}
