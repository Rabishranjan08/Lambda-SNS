using System;
using System.Collections.Generic;
using System.Text;
using Amazon.Lambda.Core;
using S3ToSQL.Models;
using static S3ToSQL.Constants;
using System.IO;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Newtonsoft.Json;
using S3ToSQL.Interfaces;

namespace ManualEmail.Services
{
    public class ProcessMessage
    {
        SqlCredentialsModel sqlCredentials;
        private readonly IAmazonSecretsManager _amazonSecretsManager;
        private string _connectionString;
        private readonly IConfigurationService _configurationService;

        public ProcessMessage()
        {
            InitializeServices();
        }
        public ProcessMessage(IConfigurationService configurationService, IAmazonSecretsManager amazonSecretsManager, ILoggerService loggerService)
        {
            _configurationService = configurationService;
            ////_dataAccess = dataAccess;
            ////_loggerService = loggerService;
            ////_amazonS3 = amazonS3;
            ////_amazonSQS = amazonSQS;
            _amazonSecretsManager = amazonSecretsManager;
            InitializeServices();
        }

        private void InitializeServices()
        {
            GetSqlCredentials();
            GetConnectionString();
        }


        private SqlCredentialsModel GetSqlCredentials()
        {
            try
            {
                sqlCredentials = GetDBCredentialsFromSecret();

            }
            catch (Exception ex)
            {
                LambdaLogger.Log(string.Format("Source object before copy : {0}", ex));

            }

            if (sqlCredentials == null)
            {
                sqlCredentials = new SqlCredentialsModel
                {
                    SqlUserName = "00lambda-dev",
                    Password = "E16E6DvC6A+4644gCC5"
                };
            }

            return sqlCredentials;
        }


        public SqlCredentialsModel GetDBCredentialsFromSecret()
        {
            //string secretName = Environment.GetEnvironmentVariable(EnvironmentVariables.Database_Secret);
            string secretName = "arn:aws:secretsmanager:us-west-2:415596832415:secret:development/servicing/correspondence/db-eq5Kku";
            MemoryStream memoryStream = new MemoryStream();

            GetSecretValueRequest request = new GetSecretValueRequest();
            request.SecretId = secretName;
            request.VersionStage = "AWSCURRENT"; // VersionStage defaults to AWSCURRENT if unspecified.

            GetSecretValueResponse response = null;

            // In this sample we only handle the specific exceptions for the 'GetSecretValue' API.
            // See https://docs.aws.amazon.com/secretsmanager/latest/apireference/API_GetSecretValue.html
            // We rethrow the exception by default.

            try
            {
                response = _amazonSecretsManager.GetSecretValueAsync(request).Result;
            }
            catch (DecryptionFailureException e)
            {
                // Secrets Manager can't decrypt the protected secret text using the provided KMS key.
                // Deal with the exception here, and/or rethrow at your discretion.
                throw;
            }
            catch (InternalServiceErrorException e)
            {
                // An error occurred on the server side.
                // Deal with the exception here, and/or rethrow at your discretion.
                throw;
            }
            catch (InvalidParameterException e)
            {
                // You provided an invalid value for a parameter.
                // Deal with the exception here, and/or rethrow at your discretion
                throw;
            }
            catch (InvalidRequestException e)
            {
                // You provided a parameter value that is not valid for the current state of the resource.
                // Deal with the exception here, and/or rethrow at your discretion.
                throw;
            }
            catch (ResourceNotFoundException e)
            {
                // We can't find the resource that you asked for.
                // Deal with the exception here, and/or rethrow at your discretion.
                throw;
            }
            catch (System.AggregateException e)
            {
                // More than one of the above exceptions were triggered.
                // Deal with the exception here, and/or rethrow at your discretion.
                throw;
            }

            // Decrypts secret using the associated KMS CMK.
            // Depending on whether the secret is a string or binary, one of these fields will be populated.
            if (response.SecretString != null)
            {
                string secret = response.SecretString;
                //_loggerService.Info(secret);
                return JsonConvert.DeserializeObject<SqlCredentialsModel>(secret);
            }

            return new SqlCredentialsModel();
        }

        private void GetConnectionString()
        {
            try
            {
                _connectionString = String.Format(_configurationService.GetConfiguration()["connectionString"], sqlCredentials.SqlUserName, sqlCredentials.Password);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
