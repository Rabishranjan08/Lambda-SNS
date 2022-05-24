using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace S3ToSQL.Interfaces
{
    public interface IConfigurationService
    {
        IConfiguration GetConfiguration();
    }
}
