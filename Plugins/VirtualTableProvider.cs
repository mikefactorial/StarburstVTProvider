using MarkMpn.Sql4Cds.Engine;
using MarkMpn.Sql4Cds.Engine.FetchXml;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Starburst.Plugins.StarburstDataProvider;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Xml.Serialization;

namespace Starburst.Plugins
{
    /// <summary>
    /// Plugin development guide: https://docs.microsoft.com/powerapps/developer/common-data-service/plug-ins
    /// Best practices and guidance: https://docs.microsoft.com/powerapps/developer/common-data-service/best-practices/business-logic/
    /// </summary>
    public class VirtualTableProvider : PluginBase
    {
        public const string RetrieveMessageName = "Retrieve";
        public const string RetrieveMultipleMessageName = "RetrieveMultiple";
        public const string CollectionOutputParameterKey = "BusinessEntityCollection";
        public const string EntityOutputParameterKey = "BusinessEntity";
        public const string QueryInputParameterKey = "Query";

        public const string StatementUrlKey = "mf_statementurl";
        public const string UserNameKey = "mf_username";
        public const string PasswordKey = "mf_password";

        public VirtualTableProvider(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(VirtualTableProvider))
        {
        }

        // Entry point for custom business logic execution
        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            var context = localPluginContext.PluginExecutionContext;
            localPluginContext.Trace(localPluginContext.PluginExecutionContext.MessageName);
            switch (localPluginContext.PluginExecutionContext.MessageName)
            {
                case RetrieveMessageName:
                    ExecuteRetrieve(localPluginContext);
                    break;
                case RetrieveMultipleMessageName:
                    ExecuteRetrieveMultiple(localPluginContext);
                    break;
                default:
                    throw new NotImplementedException($"The message: {localPluginContext.PluginExecutionContext.MessageName} is not supported");
            }
        }

        protected virtual void ExecuteRetrieve(ILocalPluginContext context)
        {
            var mapper = new GenericMapper(context);
            Entity entity = new Entity(context.PluginExecutionContext.PrimaryEntityName);

            if (mapper != null)
            {
                string queryText = $"SELECT * FROM {mapper.PrimaryEntityMetadata.LogicalName} WHERE {mapper.PrimaryEntityMetadata.PrimaryIdAttribute} = '{mapper.MapToVirtualEntityValue(mapper.PrimaryEntityMetadata.PrimaryIdAttribute, context.PluginExecutionContext.PrimaryEntityId)}'";
                queryText = mapper.MapVirtualEntityAttributes(queryText);

                context.Trace($"Query: {queryText}");
                var retrieverService = (IEntityDataSourceRetrieverService)context.ServiceProvider.GetService(typeof(IEntityDataSourceRetrieverService));
                var sourceEntity = retrieverService.RetrieveEntityDataSource();
                var statementUrl = sourceEntity[StatementUrlKey].ToString();
                var user = sourceEntity[UserNameKey].ToString();
                var pwd = GetPassword(context, sourceEntity[PasswordKey].ToString());

                DataProvider provider = new DataProvider(statementUrl, user, pwd);
                var entities = provider.ExecuteQuery(context, mapper, queryText);

                if (entities.Entities != null && entities.Entities.Count > 0)
                {
                    entity = entities.Entities[0];
                }
            }

            // Set output parameter
            context.PluginExecutionContext.OutputParameters[EntityOutputParameterKey] = entity;

        }

        protected virtual void ExecuteRetrieveMultiple(ILocalPluginContext context)
        {
            var query = context.PluginExecutionContext.InputParameters[QueryInputParameterKey];
            if (query != null)
            {
                var mapper = new GenericMapper(context);

                EntityCollection collection = new EntityCollection();
                string fetchXml = string.Empty;
                if (query is QueryExpression qe)
                {
                    var convertRequest = new QueryExpressionToFetchXmlRequest();
                    convertRequest.Query = (QueryExpression)qe;
                    var response = (QueryExpressionToFetchXmlResponse)context.PluginUserService.Execute(convertRequest);
                    fetchXml = response.FetchXml;
                }
                else if (query is FetchExpression fe)
                {
                    fetchXml = fe.Query;
                }
                if (!string.IsNullOrEmpty(fetchXml))
                {
                    context.Trace($"Pre FetchXML: {fetchXml}");

                    var metadata = new AttributeMetadataCache(context.PluginUserService);
                    var serializer = new XmlSerializer(typeof(FetchType));
                    FetchType fetch;
                    using (TextReader reader = new StringReader(fetchXml))
                    {
                        fetch = serializer.Deserialize(reader) as FetchType;
                        mapper.MapFetchXml(fetch);
                    }
                    if (fetch != null)
                    {
                        var queryText = FetchXml2Sql.Convert(context.PluginUserService, metadata, fetch, new FetchXml2SqlOptions { PreserveFetchXmlOperatorsAsFunctions = false }, out _);
                        queryText = mapper.MapVirtualEntityAttributes(queryText);
                        context.Trace($"Query: {queryText}");
                        var retrieverService = (IEntityDataSourceRetrieverService)context.ServiceProvider.GetService(typeof(IEntityDataSourceRetrieverService));
                        var sourceEntity = retrieverService.RetrieveEntityDataSource();
                        var statementUrl = sourceEntity[StatementUrlKey].ToString();
                        var user = sourceEntity[UserNameKey].ToString();
                        var pwd = sourceEntity[PasswordKey].ToString();

                        DataProvider provider = new DataProvider(statementUrl, user, pwd);
                        collection = provider.ExecuteQuery(context, mapper, queryText);
                    }
                }
                context.Trace($"Records Returned: {collection.Entities.Count}");
                context.PluginExecutionContext.OutputParameters[CollectionOutputParameterKey] = collection;
            }
        }

        public string GetPassword(ILocalPluginContext context, string configPasswordString)
        {
            if (Uri.IsWellFormedUriString(configPasswordString, UriKind.Absolute))
            {
                var scopes = new List<string> { $"https://vault.azure.net/.default" };
                var token = context.ManagedIdentityService.AcquireToken(scopes);
                context.Trace("Token: " + token);
                context.Trace("Get Password from Key Vault: " + configPasswordString);
                var response = context.HttpClient.GetResponse(configPasswordString, "Bearer", token, null);
                var mySecret = JsonConvert.DeserializeObject<KeyVaultSecret>(response);
                return mySecret.value;
            }
            return configPasswordString;
        }
    }
}
