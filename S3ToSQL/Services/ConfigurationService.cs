using Microsoft.Extensions.Configuration;
using S3ToSQL.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace S3ToSQL.Services
{
    public class ConfigurationService : IConfigurationService
    {
        public IEnvironmentService EnvService { get; }

        public ConfigurationService(IEnvironmentService envService)
        {
            EnvService = envService;
        }

        public IConfiguration GetConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{EnvService.EnvironmentName}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();
        }

        IConfiguration IConfigurationService.GetConfiguration()
        {
            throw new NotImplementedException();
        }
    }

}
