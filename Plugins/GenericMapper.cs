using MarkMpn.Sql4Cds.Engine.FetchXml;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starburst.Plugins
{
    public class GenericMapper
    {
        protected ILocalPluginContext context { get; set; }
        private EntityMetadata primaryEntityMetadata = null;

        public GenericMapper(ILocalPluginContext context)
        {
            this.context = context;
        }
        public EntityMetadata PrimaryEntityMetadata
        {
            get
            {
                if (primaryEntityMetadata == null)
                {
                    //Create RetrieveEntityRequest
                    RetrieveEntityRequest retrievesEntityRequest = new RetrieveEntityRequest
                    {
                        EntityFilters = EntityFilters.Entity | EntityFilters.Attributes,
                        LogicalName = context.PluginExecutionContext.PrimaryEntityName
                    };

                    //Execute Request
                    RetrieveEntityResponse retrieveEntityResponse = (RetrieveEntityResponse)context.PluginUserService.Execute(retrievesEntityRequest);
                    primaryEntityMetadata = retrieveEntityResponse.EntityMetadata;
                }

                return primaryEntityMetadata;
            }
        }

        public virtual void MapFetchXml(object fetch)
        {
            if (fetch is condition cond)
            {
                if (!string.IsNullOrEmpty(cond.value))
                {
                    cond.value = MapToVirtualEntityValue(cond.attribute, cond.value).ToString();
                }
                else if (cond.Items.Length > 0)
                {
                    for (int i = 0; i < cond.Items.Length; i++)
                    {
                        context?.Trace($"PreConvert: {cond.Items[i].Value}");
                        cond.Items[i].Value = MapToVirtualEntityValue(cond.attribute, cond.Items[i].Value).ToString();
                        context?.Trace($"PostConvert: {cond.Items[i].Value}");
                    }
                }
            }

            if (fetch is FetchType ft)
            {
                for (int i = 0; i < ft.Items.Length; i++)
                {
                    object item = ft.Items[i];
                    MapFetchXml(item);
                }
            }
            else if (fetch is FetchEntityType fet)
            {
                for (int i = 0; i < fet.Items.Length; i++)
                {
                    object item = fet.Items[i];
                    MapFetchXml(item);
                }
            }
            else if (fetch is FetchLinkEntityType felt)
            {
                for (int i = 0; i < felt.Items.Length; i++)
                {
                    object item = felt.Items[i];
                    MapFetchXml(item);
                }
            }
            else if (fetch is filter filt)
            {
                for (int i = 0; i < filt.Items.Length; i++)
                {
                    object item = filt.Items[i];
                    MapFetchXml(item);
                }
            }

        }

        public virtual string MapVirtualEntityAttributes(string sql)
        {
            var iEnum = GetCustomMappings().GetEnumerator();
            while (iEnum.MoveNext())
            {
                sql = sql.Replace(iEnum.Current.Key, iEnum.Current.Value);
            }

            return sql;
        }


        public virtual Dictionary<string, string> GetCustomMappings()
        {
            Dictionary<string, string> mappings = new Dictionary<string, string>();

            foreach (var att in PrimaryEntityMetadata.Attributes)
            {
                if (!string.IsNullOrEmpty(att.ExternalName))
                {
                    mappings.Add(att.LogicalName, att.ExternalName);
                }
            }
            mappings.Add(PrimaryEntityMetadata.LogicalName, PrimaryEntityMetadata.ExternalName);

            return mappings;
        }

        public virtual object MapToVirtualEntityValue(string attributeName, object value)
        {
            var att = PrimaryEntityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == attributeName);
            return MapToVirtualEntityValue(att, value);
        }

        public virtual object MapToVirtualEntityValue(AttributeMetadata entityAttribute, object value)
        {
            if (value == null)
            {
                return null;
            }
            else if (entityAttribute.LogicalName == PrimaryEntityMetadata.PrimaryIdAttribute && int.TryParse(value.ToString(), out int keyInt))
            {
                //This is a generic method of creating a guid from an int value if no guid is available in the database
                return new Guid(keyInt.ToString().PadLeft(32, 'a'));
            }
            else if (entityAttribute.LogicalName == PrimaryEntityMetadata.PrimaryIdAttribute && !Guid.TryParse(value.ToString(), out Guid keyGuid))
            {
                //This is a generic method of creating a guid from a string if no guid is available in the database
                return ConvertStringToGuid(value.ToString());
            }
            else if (entityAttribute is LookupAttributeMetadata lookupAttr1 && int.TryParse(value.ToString(), out int lookupInt))
            {
                var lookup = new EntityReference(lookupAttr1.Targets[0], new Guid(lookupInt.ToString().PadLeft(32, 'a')));
                return lookup;
            }
            else if (entityAttribute is LookupAttributeMetadata lookupAttr2 && !Guid.TryParse(value.ToString(), out Guid lookupGuid))
            {
                var lookup = new EntityReference(lookupAttr2.Targets[0], new Guid(value.GetHashCode().ToString().PadLeft(32, 'a')));
                return lookup;
            }
            else if (entityAttribute is LookupAttributeMetadata lookupAttr3 && Guid.TryParse(value.ToString(), out Guid lookupGuid1))
            {
                var lookup = new EntityReference(lookupAttr3.Targets[0], lookupGuid1);
                return lookup;
            }
            else if (entityAttribute is UniqueIdentifierAttributeMetadata uniqueIdAttr && Guid.TryParse(value.ToString(), out Guid uniqueId))
            {
                return uniqueId;
            }
            else if (entityAttribute is StatusAttributeMetadata || entityAttribute is StateAttributeMetadata || entityAttribute is PicklistAttributeMetadata)
            {
                if (int.TryParse(value.ToString(), out int picklistInt))
                {
                    return new OptionSetValue(picklistInt);
                }
                else
                {
                    //Lookup the value by name if it's not a number
                    var option = ((EnumAttributeMetadata)entityAttribute).OptionSet.Options.FirstOrDefault(o => o.Label.UserLocalizedLabel.Label == value.ToString());
                    if (option != null && option.Value != null)
                    {
                        return new OptionSetValue(option.Value.Value);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else if (entityAttribute is BooleanAttributeMetadata && bool.TryParse(value.ToString(), out bool boolValue))
            {
                return boolValue;
            }
            else if (entityAttribute is DateTimeAttributeMetadata && DateTime.TryParse(value.ToString(), out DateTime dateTime))
            {
                return dateTime;
            }
            else if (entityAttribute is BigIntAttributeMetadata && long.TryParse(value.ToString(), out long longValue))
            {
                return longValue;
            }
            else if (entityAttribute is IntegerAttributeMetadata && int.TryParse(value.ToString(), out int integerValue))
            {
                return integerValue;
            }
            else if (entityAttribute is DecimalAttributeMetadata && decimal.TryParse(value.ToString(), out decimal decimalValue))
            {
                return decimalValue;
            }
            else if (entityAttribute is DoubleAttributeMetadata && double.TryParse(value.ToString(), out double doubleValue))
            {
                return doubleValue;
            }
            else if (entityAttribute is MoneyAttributeMetadata && decimal.TryParse(value.ToString(), out decimal moneyValue))
            {
                return new Money(moneyValue);
            }
            else if (int.TryParse(value.ToString().Replace("{", string.Empty).Replace("}", string.Empty).Replace("a", string.Empty).Replace("A", string.Empty).Replace("-", string.Empty), out int intValue))
            {
                //This converts the generated guid back to an int. 
                return intValue.ToString();
            }
            else if (Guid.TryParse(value.ToString(), out var outGuid))
            {
                //We don't have any guids in our source data so this needs to convert back to string
                return ConvertGuidToString(outGuid);
            }
            else
            {
                return value;
            }
        }

        public Guid ConvertStringToGuid(string value)
        {
            //This is a generic method of creating a guid from a string if no guid is available in the database
            //If the passed in string is too long this will fail due to the translation from ASCII to Hex string
            var strValue = value.ToString();
            if (string.IsNullOrEmpty(strValue))
            {
                return Guid.NewGuid();
            }
            StringBuilder hex = new StringBuilder(strValue.Length * 2);
            foreach (char c in strValue)
            {
                hex.AppendFormat("{0:x2}", (int)c);
            }
            strValue = hex.ToString();
            if (!Guid.TryParse(strValue.PadLeft(32, 'a'), out Guid outGuid))
            {
                return Guid.NewGuid();
            }
            return outGuid;
        }

        public string ConvertGuidToString(Guid value)
        {
            //Obvious edge case where the guid already starts with an 'a' will fail
            var str = value.ToString().Replace("{", "").Replace("-", "").ToLower();
            for (var i = 0; i < str.Length; i++)
            {
                if (str[i] != 'a')
                {
                    str = str.Substring(i);
                    break;
                }
            }
            int numberChars = str.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(str.Substring(i, 2), 16);
            }
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
