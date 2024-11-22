// See https://aka.ms/new-console-template for more information
using ConsoleApp1;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Starburst.Plugins;
using Starburst.Plugins.StarburstDataProvider;
using Moq;
using Microsoft.Xrm.Sdk.Metadata;
using Starburst.Plugins.TestHarness;
using System.Net;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Client;
using Castle.Core.Resource;
using Microsoft.Identity.Client;
using System.ServiceModel.Description;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Security.Policy;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Rest;
using Newtonsoft.Json;
using System.Configuration;

string? starburstUrl = ConfigurationManager.AppSettings.Get("StarburstUrl");
string? starburstUser = ConfigurationManager.AppSettings.Get("StarburstUser");
string? starburstPass = ConfigurationManager.AppSettings.Get("StarburstPass");
string? dataverseUrl = ConfigurationManager.AppSettings.Get("DataverseUrl");
string? dataverseClientId = ConfigurationManager.AppSettings.Get("ClientId");
string? dataverseClientSecret = ConfigurationManager.AppSettings.Get("ClientSecret");
string? keyVaultSecretUrl = ConfigurationManager.AppSettings.Get("KeyVaultSecretUrl"); 
string? tenantId = ConfigurationManager.AppSettings.Get("TenantId");
string? authority = ConfigurationManager.AppSettings.Get("Authority");
string? resource = ConfigurationManager.AppSettings.Get("Resource");

ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
ServiceClient service = new ServiceClient(new Uri(dataverseUrl), dataverseClientId, dataverseClientSecret, false);
var organizationServiceFactory = new Mock<IOrganizationServiceFactory>();
organizationServiceFactory.Setup(x => x.CreateOrganizationService(It.IsAny<Guid>())).Returns(service);
var serviceProvider = new Mock<IServiceProvider>();
var pluginContext = new RemoteExecutionContext();
serviceProvider.Setup(x => x.GetService(typeof(IPluginExecutionContext))).Returns(pluginContext);
serviceProvider.Setup(x => x.GetService(typeof(IOrganizationServiceFactory))).Returns(organizationServiceFactory.Object);
serviceProvider.Setup(x => x.GetService(typeof(IOrganizationService))).Returns(service);
serviceProvider.Setup(x => x.GetService(typeof(IExecutionContext))).Returns(pluginContext);
var tracingService = new MockTracingService();
serviceProvider.Setup(x => x.GetService(typeof(ITracingService))).Returns(tracingService);
pluginContext.PrimaryEntityName = "mf_bankcustomer";
var localPluginContext = new LocalPluginContext(serviceProvider.Object);

Console.WriteLine("Hello, World!");
var content = new FormUrlEncodedContent(new Dictionary<string, string> {
              { "client_id", dataverseClientId },
              { "client_secret", dataverseClientSecret },
              { "grant_type", "client_credentials" },
              { "scope", $"{resource}/.default" },
              { "resource", resource },
            });
HttpClientWrapper client = new HttpClientWrapper();

string response = client.GetResponse($"{authority}/oauth2/token", string.Empty, string.Empty, content);
var myToken = JsonConvert.DeserializeObject<Token>(response);
response = client.GetResponse($"{keyVaultSecretUrl}?api-version=7.4", "Bearer", myToken.access_token, null);
var mySecret = JsonConvert.DeserializeObject<Starburst.Plugins.KeyVaultSecret>(response);

//PrestoQuery query = new PrestoQuery();
//Console.WriteLine((await query.Execute("SELECT custkey, last_name, country FROM sample.burstbank.customer")));

DataProvider provider = new DataProvider(starburstUrl, starburstUser, starburstPass);
GenericMapper mapper = new GenericMapper(localPluginContext);
Console.WriteLine (provider.ExecuteQuery(localPluginContext, mapper, "SELECT last_name, custkey FROM sample.burstbank.customer WHERE custkey IN ('1000001') OFFSET 0 ROWS FETCH NEXT 5000 ROWS ONLY"));