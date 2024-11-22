using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Remoting.Contexts;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Starburst.Plugins.StarburstDataProvider
{
    public class DataProvider
    {
        private string authCredentials;
        private string statementApiUrl;
        public DataProvider(string statementUri, string user, string password)
        {
            authCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}"));
            statementApiUrl = statementUri;
        }

        public EntityCollection ExecuteQuery(ILocalPluginContext context, GenericMapper mapper, string queryText)
        {
            List<Entity> entities = new List<Entity>();
            try
            {
                // Send the Query
                var queryResult = context.HttpClient.GetResponse(statementApiUrl, "Basic", authCredentials, new StringContent(queryText));

                // parse the response
                var parsedQueryResult = JsonConvert.DeserializeObject<QueryResult>(queryResult);
                // loop until data starts to flow
                bool columnSet = false;
                var nextUri = parsedQueryResult.nextUri;

                // This dictionary holds the ordinal position of columns for each row
                var columnNameLookup = new Dictionary<int, Column>();

                while (nextUri != null)
                {
                    var nextUriResult = context.HttpClient.GetResponse(nextUri, "Basic", authCredentials, null);

                    var parsedGet = JsonConvert.DeserializeObject<QueryResult>(nextUriResult);
                    nextUri = parsedGet.nextUri;
                    if (parsedGet.columns != null && parsedGet.data != null)
                    {
                        foreach (var dataRow in parsedGet.data)
                        {
                            Entity entity = new Entity(context.PluginExecutionContext.PrimaryEntityName);
                            for(var i = 0; i < dataRow.Count; i++)
                            {
                                var column = parsedGet.columns[i];
                                if (column != null && !string.IsNullOrEmpty(column.name) && dataRow[i] != null && !string.IsNullOrEmpty(dataRow[i]))
                                {
                                    context.Trace($"{column.name}:{dataRow[i]}");
                                    var entityAttribute = mapper.PrimaryEntityMetadata.Attributes.FirstOrDefault(a => a.ExternalName == column.name);
                                    if (entityAttribute != null)
                                    {
                                        entity[entityAttribute.LogicalName] = mapper.MapToVirtualEntityValue(entityAttribute, dataRow[i]);
                                    }
                                }
                            }
                            entities.Add(entity);
                        }
                    }
                    else
                    {
                        // need a brief pause between pulses
                        System.Threading.Thread.Sleep(200);
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message.Replace("{", "|").Replace("}", "|").Replace("\"", "");
            }
            return new EntityCollection(entities);
        }
    }
}
