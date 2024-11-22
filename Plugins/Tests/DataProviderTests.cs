using FluentAssertions;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Moq;
using System;
using System.Xml;
using Xunit;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;
using FluentAssertions.Equivalency.Tracing;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using static System.Formats.Asn1.AsnWriter;
using Microsoft.Identity.Client;

namespace Starburst.Plugins.Tests
{
    public class StarburstDataProviderTests
    {
        [Fact]
        public void TestExecuteQuery()
        {
            var organizationService = new Mock<IOrganizationService>();
            var organizationServiceFactory = new Mock<IOrganizationServiceFactory>();
            organizationServiceFactory.Setup(x => x.CreateOrganizationService(It.IsAny<Guid>())).Returns(organizationService.Object);
            var serviceProvider = new Mock<IServiceProvider>();
            var pluginContext = new RemoteExecutionContext();
            var tracingService = new MockTracingService();
            serviceProvider.Setup(x => x.GetService(typeof(IPluginExecutionContext))).Returns(pluginContext);
            serviceProvider.Setup(x => x.GetService(typeof(IOrganizationServiceFactory))).Returns(organizationServiceFactory.Object);
            serviceProvider.Setup(x => x.GetService(typeof(IOrganizationService))).Returns(organizationService.Object);
            serviceProvider.Setup(x => x.GetService(typeof(IExecutionContext))).Returns(pluginContext);
            serviceProvider.Setup(x => x.GetService(typeof(ITracingService))).Returns(tracingService);
            var mockHttpClient = new Mock<HttpClientWrapper>();
            mockHttpClient.Setup(s => s.GetResponse(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpContent>()))
                .Returns((string uri, string credentialType, string authCredentials, HttpContent requestBody) =>
                {
                    if (requestBody != null)
                    {
                        return File.ReadAllText("../../../Data/customer_results_post.json");
                    }
                    else
                    {
                        return File.ReadAllText("../../../Data/customer_results_get.json");
                    }
                });
            serviceProvider.Setup(x => x.GetService(typeof(HttpClientWrapper))).Returns(mockHttpClient.Object);

            var localPluginContext = new LocalPluginContext(serviceProvider.Object);

            organizationService.Setup(s => s.Execute(It.IsAny<RetrieveEntityRequest>()))
                .Returns((RetrieveEntityRequest request) => 
                {
                    var entityMetadata = new EntityMetadata()
                    {
                        LogicalName = "customer",
                        ExternalName = "sample.burstbank.customer",
                        ExternalCollectionName = "sample.burstbank.customer"
                    };
                    entityMetadata.SetPropertyValue("PrimaryIdAttribute", "custkey");
                    entityMetadata.SetPropertyValue("Attributes", new AttributeMetadata[] 
                    { 
                        new StringAttributeMetadata() 
                        { 
                            LogicalName = "custkey", ExternalName = "custkey" 
                        }, 
                        new StringAttributeMetadata() 
                        { 
                            LogicalName = "last_name", ExternalName = "last_name" 
                        } 
                    });
                    return new RetrieveEntityResponse() 
                    { 
                        Results = new ParameterCollection 
                        { 
                            {
                                "EntityMetadata", entityMetadata 
                            } 
                        } 
                    };
                });
               

            GenericMapper mapper = new GenericMapper(localPluginContext);
            var dataProvider = new StarburstDataProvider.DataProvider(string.Empty, string.Empty, string.Empty);
            var results = dataProvider.ExecuteQuery(localPluginContext, mapper, "SELECT custkey, last_name, country FROM sample.burstbank.customer LIMIT 10");
            results.Should().NotBeNull();
            results.Entities.Should().NotBeEmpty();
            results.Entities.Count.Should().Be(10);
        }

        [Fact]
        public void TestExecuteQueryWithFilter()
        {
            var organizationService = new Mock<IOrganizationService>();
            var organizationServiceFactory = new Mock<IOrganizationServiceFactory>();
            organizationServiceFactory.Setup(x => x.CreateOrganizationService(It.IsAny<Guid>())).Returns(organizationService.Object);
            var serviceProvider = new Mock<IServiceProvider>();
            var pluginContext = new RemoteExecutionContext();
            var tracingService = new MockTracingService();
            serviceProvider.Setup(x => x.GetService(typeof(IPluginExecutionContext))).Returns(pluginContext);
            serviceProvider.Setup(x => x.GetService(typeof(IOrganizationServiceFactory))).Returns(organizationServiceFactory.Object);
            serviceProvider.Setup(x => x.GetService(typeof(IOrganizationService))).Returns(organizationService.Object);
            serviceProvider.Setup(x => x.GetService(typeof(IExecutionContext))).Returns(pluginContext);
            serviceProvider.Setup(x => x.GetService(typeof(ITracingService))).Returns(tracingService);
            var mockHttpClient = new Mock<HttpClientWrapper>();
            mockHttpClient.Setup(s => s.GetResponse(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpContent>()))
                .Returns((string uri, string credentialType, string authCredentials, HttpContent requestBody) =>
                {
                    if (requestBody != null)
                    {
                        return File.ReadAllText("../../../Data/customer_results_post.json");
                    }
                    else
                    {
                        return File.ReadAllText("../../../Data/customer_results_get.json");
                    }
                });
            serviceProvider.Setup(x => x.GetService(typeof(HttpClientWrapper))).Returns(mockHttpClient.Object);

            var localPluginContext = new LocalPluginContext(serviceProvider.Object);

            organizationService.Setup(s => s.Execute(It.IsAny<RetrieveEntityRequest>()))
                .Returns((RetrieveEntityRequest request) =>
                {
                    var entityMetadata = new EntityMetadata()
                    {
                        LogicalName = "customer",
                        ExternalName = "sample.burstbank.customer",
                        ExternalCollectionName = "sample.burstbank.customer"
                    };
                    entityMetadata.SetPropertyValue("PrimaryIdAttribute", "custkey");
                    entityMetadata.SetPropertyValue("Attributes", new AttributeMetadata[]
                    {
                        new StringAttributeMetadata()
                        {
                            LogicalName = "custkey", ExternalName = "custkey"
                        },
                        new StringAttributeMetadata()
                        {
                            LogicalName = "last_name", ExternalName = "last_name"
                        }
                    });
                    return new RetrieveEntityResponse()
                    {
                        Results = new ParameterCollection
                        {
                            {
                                "EntityMetadata", entityMetadata
                            }
                        }
                    };
                });


            GenericMapper mapper = new GenericMapper(localPluginContext);
            var dataProvider = new StarburstDataProvider.DataProvider(string.Empty, string.Empty, string.Empty);
            var results = dataProvider.ExecuteQuery(localPluginContext, mapper, "SELECT custkey, last_name, country FROM sample.burstbank.customer WHERE country = 'US' LIMIT 10");
            results.Should().NotBeNull();
            results.Entities.Should().NotBeEmpty();
            results.Entities.Count.Should().Be(10);
        }
    }
}