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
using Starburst.Plugins.StarburstDataProvider;
using MarkMpn.Sql4Cds.Engine;
using System.Text.RegularExpressions;

namespace Starburst.Plugins.Tests
{
    public class FetchToSqlTests
    {
        [Fact]
        public void TestTopQuery()
        {
            var organizationService = new Mock<IOrganizationService>();
            string fetchXml = @"<fetch top='5'><entity name='account'><attribute name='name'/></entity></fetch>";
            var metadata = new AttributeMetadataCache(organizationService.Object);

            var queryText = FetchXml2Sql.Convert(organizationService.Object, metadata, fetchXml, new FetchXml2SqlOptions { PreserveFetchXmlOperatorsAsFunctions = false }, out _);
            queryText.EndsWith("LIMIT 5").Should().BeTrue("Top query should end with LIMIT 5");
        }

        [Fact]
        public void TestTopQueryWithOrderBy()
        {
            var organizationService = new Mock<IOrganizationService>();
            string fetchXml = @"<fetch top='5'><entity name='account'><attribute name='name'/><order attribute='name' descending='true'/></entity></fetch>";
            var metadata = new AttributeMetadataCache(organizationService.Object);

            var queryText = FetchXml2Sql.Convert(organizationService.Object, metadata, fetchXml, new FetchXml2SqlOptions { PreserveFetchXmlOperatorsAsFunctions = false }, out _);
            queryText.EndsWith("LIMIT 5").Should().BeTrue("Top query should end with LIMIT 5");
        }

        [Fact]
        public void TestQueryWithFilter()
        {
            var organizationService = new Mock<IOrganizationService>();
            this.setupOrganizationService(organizationService);
            string fetchXml = @"<fetch><entity name='account'><attribute name='name'/><filter><condition attribute='name' operator='eq' value='Test'/></filter></entity></fetch>";
            var metadata = new AttributeMetadataCache(organizationService.Object);

            var queryText = FetchXml2Sql.Convert(organizationService.Object, metadata, fetchXml, new FetchXml2SqlOptions { PreserveFetchXmlOperatorsAsFunctions = false }, out _);
            queryText.Should().Contain("name = 'Test'");
        }

        [Fact]
        public void TestQueryWithFilterAndOrderBy()
        {
            var organizationService = new Mock<IOrganizationService>();
            this.setupOrganizationService(organizationService);
            string fetchXml = @"<fetch><entity name='account'><attribute name='name'/><filter><condition attribute='name' operator='eq' value='Test'/></filter><order attribute='name' descending='true'/></entity></fetch>";
            var metadata = new AttributeMetadataCache(organizationService.Object);

            var queryText = FetchXml2Sql.Convert(organizationService.Object, metadata, fetchXml, new FetchXml2SqlOptions { PreserveFetchXmlOperatorsAsFunctions = false }, out _);
            queryText.Should().Contain("name = 'Test'");
            queryText.Should().Contain("ORDER BY name DESC");
        }

        [Fact]
        public void TestQueryWithJoin()
        {
            var organizationService = new Mock<IOrganizationService>();
            this.setupOrganizationService(organizationService);
            string fetchXml = @"<fetch><entity name='account'><attribute name='name'/><link-entity name='contact' from='accountid' to='accountid' alias='contact'><attribute name='fullname'/></link-entity></entity></fetch>";
            var metadata = new AttributeMetadataCache(organizationService.Object);

            var queryText = FetchXml2Sql.Convert(organizationService.Object, metadata, fetchXml, new FetchXml2SqlOptions { PreserveFetchXmlOperatorsAsFunctions = false }, out _);
            queryText.Should().Contain("ON account.accountid = contact.accountid");
        }


        protected void setupOrganizationService(Mock<IOrganizationService> organizationService)
        {
            organizationService.Setup(s => s.Execute(It.IsAny<RetrieveEntityRequest>()))
                .Returns((RetrieveEntityRequest request) =>
                {
                    var entityMetadata = new EntityMetadata()
                    {
                        LogicalName = "account",
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
                            LogicalName = "name", ExternalName = "name"
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
        }
    }
}