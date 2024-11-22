using Newtonsoft.Json;
using System.Text;

namespace ConsoleApp1
{
    internal class PrestoQuery
    {
        static string PrestoResponse;
        private static byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes($"michael.ochs@asurion.com:M!&N7Lm5XI!fXnk4");
        private static string val = System.Convert.ToBase64String(plainTextBytes);
        static StringBuilder sb = new StringBuilder();
        // This dictionary holds the ordinal position of columns for each row
        static Dictionary<int, string> ColumnNameLookup = new Dictionary<int, string>();
        // this dicitonary uses the same ordinal to allow you to return the datatype of the datacolumn
        static Dictionary<int, string> DataTypeLookup = new Dictionary<int, string>();

        public async Task<string> Execute(string queryText)
        {
            try
            {
                //reset variables
                sb = new StringBuilder();

                //Build the json structure
                sb.Append("{\"" + "Status" + "\":" + "\"" + "Success" + "\"," + Environment.NewLine
                                                 + "\"" + "ErrorMessage" + "\":" + "\"" + "none" + "\"," + Environment.NewLine
                                                 + "\"" + "Data" + "\":");

                sb.Append("[");

                ColumnNameLookup = new Dictionary<int, string>();
                DataTypeLookup = new Dictionary<int, string>();

                string responseMessage = "I have nothing to say to you";

                if (queryText.Length == 0
                   || queryText.ToUpper().Contains("SELECT") == false)
                {
                    responseMessage = "Query not valid or does not contain a Select Statement. The Query you Passed in: [" + queryText + "]";
                }
                else
                {
                    responseMessage = "The Query you Passed in: [" + queryText + "]";
                }

                // Send the Query
                await PostQuery("post", queryText);
                //log.LogInformation("Presto Respoded with: " + PrestoResponse);
                responseMessage = PrestoResponse;

                // parse the response
                dynamic ParsedPost = JsonConvert.DeserializeObject<dynamic>(PrestoResponse);
                string NextURI = Convert.ToString(ParsedPost.nextUri);
                //log.LogInformation(NextURI);

                // loop until data starts to flow
                bool columnSet = false;
                dynamic ParsedGet;
                string columns = null;

                while (NextURI != null)
                {

                    await GetData(NextURI);

                    // parse the response
                    responseMessage = PrestoResponse;
                    // log.LogInformation("Presto Respoded with: " + PrestoResponse);
                    ParsedGet = JsonConvert.DeserializeObject<dynamic>(PrestoResponse);


                    // Store the data
                    string data = Convert.ToString(ParsedGet.data);
                    columns = Convert.ToString(ParsedGet.columns);
                    NextURI = Convert.ToString(ParsedGet.nextUri);
                    // log.LogInformation(NextURI);

                    int ordinal = 1;

                    if (columns != null && columnSet == false)
                    {
                        ordinal = 1;
                        foreach (Newtonsoft.Json.Linq.JObject item in ParsedGet.columns) // <-- Note that here we used JObject instead of usual JProperty
                        {
                            // add the columns to an ordinal look up
                            string name = item.GetValue("name").ToString();
                            string type = item.GetValue("type").ToString();
                            ColumnNameLookup.Add(ordinal, name);
                            DataTypeLookup.Add(ordinal, type);
                            ordinal = ordinal + 1;

                        }

                        columnSet = true;
                    }

                    ordinal = 1;
                    int rowcounter = 0;

                    // this will parse a json response from the data
                    if (data != null)
                    {
                        int RowCountofData = ParsedGet.data.Count;
                        foreach (Newtonsoft.Json.Linq.JArray DataRow in ParsedGet.data)
                        {

                            sb.Append("{");
                            foreach (Newtonsoft.Json.Linq.JValue F in DataRow)
                            {
                                string datatype = DataTypeLookup[ordinal];
                                string cname = ColumnNameLookup[ordinal];


                                if (F.ToString().Length == 0)
                                {
                                    sb.Append("\"" + cname + "\":" + "null" + "," + Environment.NewLine);
                                }
                                else if (datatype.StartsWith("decimal") || datatype.StartsWith("bool"))
                                {
                                    sb.Append("\"" + cname + "\":" + F.ToString() + "," + Environment.NewLine);
                                }
                                else
                                {
                                    sb.Append("\"" + cname + "\":" + "\"" + System.Web.HttpUtility.JavaScriptStringEncode(F.ToString()) + "\"" + "," + Environment.NewLine);
                                }


                                ordinal = ordinal + 1;
                            }

                            //reset the ordinal counter for the next row
                            ordinal = 1;
                            rowcounter = rowcounter + 1;


                            sb.Append("},");



                        }
                    }
                    else
                    {
                        // need a brief pause between pulses
                        System.Threading.Thread.Sleep(1000);
                    }
                }

                sb.Length--;

                /* close the final Json element */

                sb.Replace("," + Environment.NewLine + "}", Environment.NewLine + "}");
                //Parsing the Json to make it pretty

                /*
                dynamic FinalJson = JsonConvert.DeserializeObject<dynamic>(sb.ToString());
                string JsonData = FinalJson.ToString();
                */

                sb.Append("]}");
            }
            catch (Exception ex)
            {
                string msg = ex.Message.Replace("{", "|").Replace("}", "|").Replace("\"", "");
            }
            return sb.ToString();
        }

        // gets a basic auth token using the service account 
        static async Task PostQuery(string RequestType = "get", string QueryText = "select 'No Query Sent'")
        {
            // PResto API Endpoint
            string url = "https://mikefactorial-free-cluster.trino.galaxy.starburst.io/v1/statement";

            HttpMessageHandler handler = new HttpClientHandler();

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(url),
                //1 hour timeout
                Timeout = new TimeSpan(1, 0, 0)
            };

            httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);

            //make the call
            HttpContent Body = new StringContent(QueryText);
            HttpResponseMessage response = await httpClient.PostAsync(url, Body);

            //record the response
            PrestoResponse = await response.Content.ReadAsStringAsync();


        }

        // gets a basic auth token using the service account 
        static async Task GetData(string URI)
        {



            HttpMessageHandler handler = new HttpClientHandler();
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(URI),
                //1 hour timeout
                Timeout = new TimeSpan(1, 0, 0)
            };

            // set up the call
            httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + val);



            // make the call
            HttpResponseMessage response = await httpClient.GetAsync(URI);

            //record the response
            PrestoResponse = await response.Content.ReadAsStringAsync();

        }


    }
}
