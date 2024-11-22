using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starburst.Plugins.StarburstDataProvider
{
    internal class QueryResult
    {
        public string id { get; set; }
        public string infoUri { get; set; }
        public string nextUri { get; set; }
        public List<object> warnings { get; set; }
        public List<Column> columns { get; set; }
        public List<List<string>> data { get; set; }
    }
    internal class Column
    {
        public string name { get; set; }
        public string type { get; set; }
        public TypeSignature typeSignature { get; set; }
    }
    public class TypeSignature
    {
        public string rawType { get; set; }
        public List<Argument> arguments { get; set; }
    }
    public class Argument
    {
        public string kind { get; set; }
        public object value { get; set; }
    }
}
