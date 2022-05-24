using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Util;
using S3ToSQL.Interfaces;
using S3ToSQL.Services;
using Amazon.SecretsManager;
using S3ToSQL.Models;
using Amazon.KeyManagementService;
using OpenTracing.Util;
using NewRelic.Api.Agent;
using NewRelic.OpenTracing.AmazonLambda;
using System.IO;
using Amazon.KeyManagementService.Model;
using Amazon.SecretsManager.Model;
using Newtonsoft.Json;
using static S3ToSQL.Constants;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ManualEmail
{
    public class Function
    {
        public ILoggerService _loggerService { get; }
        public IConfigurationService _configService { get; }
        public IAmazonS3 _s3Client { get; }
        //public IProcessMessage _processMessage { get; }
        IAmazonS3 S3Client { get; set; }
        public IEnvironmentService _environmentService { get; }
        public IAmazonSecretsManager _amazonSecretsManager { get; }
        public IAmazonKeyManagementService _amazonKeyManagementService { get; }

        SqlCredentialsModel sqlCredentials;
        private string _connectionString;




        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {
            S3Client = new AmazonS3Client();
            var serviceCollection = new ServiceCollection();
            InjectServices(serviceCollection);
            // ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            _configService = serviceProvider.GetService<IConfigurationService>();
            _loggerService = serviceProvider.GetService<ILoggerService>();
            _environmentService = serviceProvider.GetService<IEnvironmentService>();
            _s3Client = serviceProvider.GetService<IAmazonS3>();
            _amazonSecretsManager = serviceProvider.GetService<IAmazonSecretsManager>();
            _amazonKeyManagementService = serviceProvider.GetService<IAmazonKeyManagementService>();


            // _processMessage = serviceProvider.GetService<IProcessMessage>();
            GlobalTracer.Register(NewRelic.OpenTracing.AmazonLambda.LambdaTracer.Instance); S3Client = new AmazonS3Client();
        }

        public Function(IConfigurationService configService,
         ILoggerService loggerService,
         IAmazonSecretsManager amazonSecretsManager,
         IAmazonKeyManagementService amazonKeyManagementService)
        {
            _configService = configService;
            _loggerService = loggerService;
            _amazonSecretsManager = amazonSecretsManager;
            _amazonKeyManagementService = amazonKeyManagementService;
        }

        private void InjectServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IConfigurationService, ConfigurationService>();
            serviceCollection.AddSingleton<ILoggerService, LoggerService>();
            serviceCollection.AddTransient<IEnvironmentService, EnvironmentService>();
            serviceCollection.AddAWSService<IAmazonS3>();
            serviceCollection.AddAWSService<IAmazonSecretsManager>();
            serviceCollection.AddAWSService<IAmazonKeyManagementService>();

        }

        public async Task FunctionHandlerWrapper(S3Event evnt, ILambdaContext context)
        {


            _loggerService.SetContext(context);
            if (evnt != null)
            {
                try
                {
                    _loggerService.LogMessage(String.Format("S3Event in FHW : {0}  , {1} ", evnt.Records[0].AwsRegion, evnt.Records[0].S3.Bucket.Arn));
                }
                catch (Exception e)
                {
                    _loggerService.LogMessage(String.Format("S3event null in FHW"));
                }
            }
            else
            {
                _loggerService.LogMessage(String.Format("S3Event in FHW is null"));
            }
            _loggerService.LogMessage(String.Format("Lambdacontext Fun name in FHW : {0}  ", context.FunctionName));
            _loggerService.LogMessage(String.Format("Lambdacontext Fun  in FHW : {0}  ", context.InvokedFunctionArn));

            GetSqlCredentials();
            GetConnectionString();
            await new TracingRequestHandler().LambdaWrapper(FunctionHandler, evnt, context);
        }

        /// <summary>
        /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
        /// </summary>
        /// <param name="s3Client"></param>
        public Function(IAmazonS3 s3Client)
        {
            this.S3Client = s3Client;
        }


        private SqlCredentialsModel GetSqlCredentials()
        {
            try
            {
                sqlCredentials = GetDBCredentialsFromSecret();
                _loggerService.LogMessage(String.Format("SqlCredentials obtained : {0}  ", sqlCredentials.SqlUserName));

                // _loggerService.LogMessage("SqlCredentials obtained : {0} ", sqlCredentials.SqlUserName);

            }
            catch (Exception ex)
            {
                LambdaLogger.Log(string.Format("Source object before copy from sqlcred: {0}", ex));

            }
            return sqlCredentials;
        }


        public SqlCredentialsModel GetDBCredentialsFromSecret()
        {
            string secretName = Environment.GetEnvironmentVariable(EnvironmentVariables.Database_Secret);
            _loggerService.LogMessage("secretname is received from GDCFS: {0} ", secretName);
            //string secretName = "arn:aws:secretsmanager:us-west-2:415596832415:secret:development/servicing/correspondence/db-eq5Kku";
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
                _loggerService.LogMessage(e.Message);
                throw;
            }
            catch (InternalServiceErrorException e)
            {
                // An error occurred on the server side.
                // Deal with the exception here, and/or rethrow at your discretion.
                _loggerService.LogMessage(e.Message);
                throw;
            }
            catch (Amazon.SecretsManager.Model.InvalidParameterException e)
            {
                // You provided an invalid value for a parameter.
                // Deal with the exception here, and/or rethrow at your discretion
                _loggerService.LogMessage(e.Message);
                throw;
            }
            catch (InvalidRequestException e)
            {
                // You provided a parameter value that is not valid for the current state of the resource.
                // Deal with the exception here, and/or rethrow at your discretion.
                _loggerService.LogMessage(e.Message);
                throw;
            }
            catch (Amazon.SecretsManager.Model.ResourceNotFoundException e)
            {
                // We can't find the resource that you asked for.
                // Deal with the exception here, and/or rethrow at your discretion.
                _loggerService.LogMessage(e.Message);
                throw;
            }
            catch (System.AggregateException e)
            {
                // More than one of the above exceptions were triggered.
                // Deal with the exception here, and/or rethrow at your discretion.
                _loggerService.LogMessage(e.Message);
                throw;
            }

            // Decrypts secret using the associated KMS CMK.
            // Depending on whether the secret is a string or binary, one of these fields will be populated.
            if (response.SecretString != null)
            {
                string secret = response.SecretString;

                _loggerService.LogMessage("AfterKMS secret: {0} ", secret);
                //_loggerService.Info(secret);
                return JsonConvert.DeserializeObject<SqlCredentialsModel>(secret);
            }

            return new SqlCredentialsModel();
        }

        private void GetConnectionString()
        {
            try
            {
                _connectionString = String.Format(_configService.GetConfiguration()["connectionString"], sqlCredentials.SqlUserName, sqlCredentials.Password);
                // _loggerService.LogMessage("Connection string obtained : {0} ", _connectionString);
                _loggerService.LogMessage(String.Format("Connection string obtained : {0} ", _connectionString));

            }
            catch (Exception e)
            {
                _loggerService.LogMessage("Exception in connection string  : {0} ", e.Message);
                // throw;
            }
        }

        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
        /// to respond to S3 notifications.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            if (evnt != null)
            {
                try
                {
                    _loggerService.LogMessage(String.Format("Event value in Function Handler : {0}", evnt.Records[0].S3));
                    _loggerService.LogMessage(String.Format("event in function handler : {0} ,  {1} ", evnt.Records[0].S3.Bucket.Name, evnt.Records[0].S3.Bucket.Arn));
                }
                catch (Exception e)
                {
                    _loggerService.LogMessage(String.Format("Exception in evnt : {0}", e.Message));
                }
            }
            else
            {
                _loggerService.LogMessage(String.Format("evnt is null"));
            }
            //ProcessMessage pm = new ProcessMessage();
            _loggerService.LogMessage(String.Format("Profile name before  "));


            string profileName = _configService.GetConfiguration()["AWSProfileName"];
            //=> _connectionString = _configService.GetConfigration()["PennE2DocsContext"];
            if (profileName != null)
            {
                _loggerService.LogMessage(String.Format(" Profile name obtained :   {0} ", profileName));
            }
            else
            {
                _loggerService.LogMessage(String.Format("Profile name is Null"));
            }

            var s3Event = evnt.Records?[0].S3;

            if (s3Event != null)
            {
                _loggerService.LogMessage(String.Format("Bucket info {0} ,{1} ", s3Event.Bucket.Name, s3Event.Bucket.Arn));

            }

            //_loggerService.LogMessage(String.Format(" s3Event received "));
            else
            {
                _loggerService.LogMessage(String.Format(" s3Event not received and is null"));

                return null;
            }


            try
            {
                _loggerService.LogMessage(String.Format("Entered function "));

                string TopicArn = _configService.GetConfiguration()["topicArn"];
                var snsClient = new AmazonSimpleNotificationServiceClient();
                string Message = "Hello at " + DateTime.Now.ToShortTimeString() + String.Format(" File has been started to process :{0} ", WebUtility.UrlDecode(s3Event.Object.Key).ToString());

                var request = new PublishRequest(TopicArn, Message);

                var resp = snsClient.PublishAsync(request).Result;

                _loggerService.LogMessage(String.Format("SNS topic sent reading file :{0}", WebUtility.UrlDecode(s3Event.Object.Key).ToString()));

                // string cs = "data source=rds-correspondence-dev;initial catalog=PortComm;integrated security=true";
                string cs = _connectionString;
                _loggerService.LogMessage(String.Format("Connection string in function :{0} ", cs));

                string bucketName = s3Event.Bucket.Name;
                _loggerService.LogMessage(String.Format("Bucket name obtained :{0} ", bucketName));

                string _FilePath = WebUtility.UrlDecode(s3Event.Object.Key);

                var objectResponse = await S3Client.GetObjectAsync(bucketName, _FilePath);
                var bytes = new byte[objectResponse.ResponseStream.Length];

                try
                {
                    _loggerService.LogMessage(String.Format("Text file received :{0} ", bytes.ToString()));
                }
                catch
                {
                    _loggerService.LogMessage(String.Format("Not able to convert into string "));
                }
                if (bytes != null)
                {
                    _loggerService.LogMessage(String.Format("Data fetched successfully from file"));

                    //_loggerService.LogMessage("Data fetched successfully from file");
                }
                objectResponse.ResponseStream.Read(bytes, 0, bytes.Length);
                var z = Encoding.UTF8.GetString(bytes);
                string value = z.ToString();
                _loggerService.LogMessage(String.Format(" Value of text : {0}", value));

                string[] strArray = value.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                string s = strArray[0];
                int cnt = 1;
                foreach (char x in s)
                {
                    if (x.Equals(','))
                    {
                        cnt++;
                    }
                }
                // _loggerService.LogMessage(String.Format(" Value of text : {0}", value));
                CSVToDataTable dt = new CSVToDataTable();
                DataTable Dt = new DataTable();


                Dt = dt.ConvertCsvToDataTable(value, cnt);
                if (Dt != null)
                {
                    _loggerService.LogMessage(String.Format(" Datatable created successfully"));

                }

                using (SqlConnection con = new SqlConnection(cs))
                {

                    using (SqlCommand cmd = new SqlCommand("[SourceData].[usp_InsertManualEmail]"))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Connection = con;
                        cmd.Parameters.AddWithValue("@EmailType", Dt);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                _loggerService.LogMessage(String.Format("SP successfully executed"));

                Message = "Hello at " + DateTime.Now.ToShortTimeString() + String.Format(" File has been processed :{0} ", WebUtility.UrlDecode(s3Event.Object.Key).ToString());

                request = new PublishRequest(TopicArn, Message);
                resp = snsClient.PublishAsync(request).Result;
                _loggerService.LogMessage(String.Format("SNS topic sent Processed Successfully :{0}", WebUtility.UrlDecode(s3Event.Object.Key).ToString()));

                var response = await this.S3Client.GetObjectMetadataAsync(s3Event.Bucket.Name, s3Event.Object.Key);
                return response.Headers.ContentType;
            }
            catch (Exception e)
            {
                _loggerService.LogMessage(String.Format("Exception incatch block :{0} ", e.Message));

                context.Logger.LogLine($"Error getting object {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
                context.Logger.LogLine(e.Message);
                context.Logger.LogLine(e.StackTrace);
                throw;
            }
        }
    }
}
