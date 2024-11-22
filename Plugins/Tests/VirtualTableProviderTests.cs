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
using MarkMpn.Sql4Cds.Engine;
using System.Text.RegularExpressions;

namespace Starburst.Plugins.Tests
{
    public class VirtualTableProviderTests
    {
        [Fact]
        public void TestQueryWithFilter()
        {
            var organizationService = new Mock<IOrganizationService>();
            var organizationServiceFactory = new Mock<IOrganizationServiceFactory>();
            organizationServiceFactory.Setup(x => x.CreateOrganizationService(It.IsAny<Guid>())).Returns(organizationService.Object);
            var serviceProvider = new Mock<IServiceProvider>();
            var pluginContext = new RemoteExecutionContext();
            var tracingService = new MockTracingService();

            var mockManagedIdentityService = new Mock<IManagedIdentityService>();
            mockManagedIdentityService.Setup(m => m.AcquireToken(It.IsAny<List<string>>())).Returns("123");

            var retrieverService = new Mock<IEntityDataSourceRetrieverService>();
            serviceProvider.Setup(x => x.GetService(typeof(IManagedIdentityService))).Returns(mockManagedIdentityService.Object);
            serviceProvider.Setup(x => x.GetService(typeof(IEntityDataSourceRetrieverService))).Returns(retrieverService.Object);
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
            var localPluginContext = new Mock<LocalPluginContext>(serviceProvider.Object);
            retrieverService.Setup(s => s.RetrieveEntityDataSource())
                .Returns(() =>
                {
                    var entity = new Entity();
                    entity[VirtualTableProvider.StatementUrlKey] = string.Empty;
                    entity[VirtualTableProvider.UserNameKey] = string.Empty;
                    entity[VirtualTableProvider.PasswordKey] = string.Empty;
                    return entity;
                });

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
                        },
                        new StringAttributeMetadata()
                        {
                            LogicalName = "country", ExternalName = "country"
                        },
                        new DateTimeAttributeMetadata()
                        {
                            LogicalName = "dob", ExternalName = "dob"
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


            string fetchXml = @"<fetch top='10'><entity name='customer'><all-attributes /><filter><condition attribute='country' operator='eq' value='US'/></filter></entity></fetch>";
            FetchExpression expression = new FetchExpression(fetchXml);
            pluginContext.InputParameters[VirtualTableProvider.QueryInputParameterKey] = expression;
            pluginContext.MessageName = VirtualTableProvider.RetrieveMultipleMessageName;
            VirtualTableProvider provider = new VirtualTableProvider(string.Empty, string.Empty);
            provider.Execute(serviceProvider.Object);
            var entities = pluginContext.OutputParameters[VirtualTableProvider.CollectionOutputParameterKey] as EntityCollection;
            entities.Should().NotBeNull();
            entities.Entities.Should().NotBeEmpty();
            entities.Entities.Count.Should().Be(10);

        }

        [Fact]
        public void TestGetPassword()
        {
            var organizationService = new Mock<IOrganizationService>();
            var organizationServiceFactory = new Mock<IOrganizationServiceFactory>();
            organizationServiceFactory.Setup(x => x.CreateOrganizationService(It.IsAny<Guid>())).Returns(organizationService.Object);
            var serviceProvider = new Mock<IServiceProvider>();
            var pluginContext = new RemoteExecutionContext();
            var tracingService = new MockTracingService();

            var mockManagedIdentityService = new Mock<IManagedIdentityService>();
            mockManagedIdentityService.Setup(m => m.AcquireToken(It.IsAny<List<string>>())).Returns("123");
            serviceProvider.Setup(x => x.GetService(typeof(IManagedIdentityService))).Returns(mockManagedIdentityService.Object);
            serviceProvider.Setup(x => x.GetService(typeof(IPluginExecutionContext))).Returns(pluginContext);
            serviceProvider.Setup(x => x.GetService(typeof(IOrganizationServiceFactory))).Returns(organizationServiceFactory.Object);
            serviceProvider.Setup(x => x.GetService(typeof(IOrganizationService))).Returns(organizationService.Object);
            serviceProvider.Setup(x => x.GetService(typeof(IExecutionContext))).Returns(pluginContext);
            serviceProvider.Setup(x => x.GetService(typeof(ITracingService))).Returns(tracingService);
            var mockHttpClient = new Mock<HttpClientWrapper>();
            mockHttpClient.Setup(s => s.GetResponse(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpContent>()))
                .Returns((string uri, string credentialType, string authCredentials, HttpContent requestBody) =>
                {
                    return "{\"value\":\"234\"}";
                });


            serviceProvider.Setup(x => x.GetService(typeof(HttpClientWrapper))).Returns(mockHttpClient.Object);
            var localPluginContext = new Mock<LocalPluginContext>(serviceProvider.Object);

            VirtualTableProvider provider = new VirtualTableProvider(string.Empty, string.Empty);
            provider.GetPassword(localPluginContext.Object, "123").Should().Be("123");
            provider.GetPassword(localPluginContext.Object, "https://foo.com/").Should().Be("234");
        }
    }
}