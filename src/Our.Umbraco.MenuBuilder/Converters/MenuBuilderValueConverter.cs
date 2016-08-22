using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Our.Umbraco.MenuBuilder.Extensions;
using Our.Umbraco.MenuBuilder.Helpers;
using Our.Umbraco.MenuBuilder.Models;
using Our.Umbraco.MenuBuilder.PropertyEditors;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
using Umbraco.Web;

namespace Our.Umbraco.MenuBuilder.Converters
{
    public class MenuBuilderValueConverter : IPropertyValueConverterMeta
    {
        public object ConvertDataToSource(PublishedPropertyType propertyType, object source, bool preview)
        {
            try
            {
                if (source != null && !source.ToString().IsNullOrWhiteSpace())
                {
                    var rawValue = JsonConvert.DeserializeObject<List<object>>(source.ToString());
                    var processedValue = new List<IPublishedContent>();

                    var preValueCollection = MenuBuilderHelper.GetPreValuesCollectionByDataTypeId(propertyType.DataTypeId);
                    var preValueDictionary = preValueCollection.AsPreValueDictionary();

                    for (var i = 0; i < rawValue.Count; i++)
                    {
                        var item = (JObject)rawValue[i];

                        var contentTypeAlias = MenuBuilderHelper.GetContentTypeAliasFromItem(item);
                        if (string.IsNullOrEmpty(contentTypeAlias))
                        {
                            continue;
                        }

                        var publishedContentType = PublishedContentType.Get(PublishedItemType.Content, contentTypeAlias);
                        if (publishedContentType == null)
                        {
                            continue;
                        }

                        var propValues = item.ToObject<Dictionary<string, object>>();
                        var properties = new List<IPublishedProperty>();

                        foreach (var jProp in propValues)
                        {
                            var propType = publishedContentType.GetPropertyType(jProp.Key);
                            if (propType != null)
                            {
                                properties.Add(new DetachedPublishedProperty(propType, jProp.Value));
                            }
                        }

                        // Parse out the name manually
                        object nameObj = null;
                        if (propValues.TryGetValue("name", out nameObj))
                        {
                            // Do nothing, we just want to parse out the name if we can
                        }

                        // Get the current request node we are embedded in
                        var pcr = UmbracoContext.Current.PublishedContentRequest;
                        var containerNode = pcr != null && pcr.HasPublishedContent ? pcr.PublishedContent : null;

                        processedValue.Add(new DetachedPublishedContent(
                            nameObj == null ? null : nameObj.ToString(),
                            publishedContentType,
                            properties.ToArray(),
                            containerNode,
                            i));
                    }

                    return processedValue;
                }

            }
            catch (Exception e)
            {
                LogHelper.Error<MenuBuilderValueConverter>("Error converting value", e);
            }

            return null;
        }

        public bool IsConverter(PublishedPropertyType propertyType)
        {
            return propertyType.PropertyEditorAlias.InvariantEquals(MenuBuilderPropertyEditor.PropertyEditorAlias);
        }

        public object ConvertSourceToObject(PublishedPropertyType propertyType, object source, bool preview)
        {
            throw new NotImplementedException();
        }

        public object ConvertSourceToXPath(PublishedPropertyType propertyType, object source, bool preview)
        {
            throw new NotImplementedException();
        }

        public Type GetPropertyValueType(PublishedPropertyType propertyType)
        {
            return typeof(IEnumerable<IPublishedContent>);
        }

        public PropertyCacheLevel GetPropertyCacheLevel(PublishedPropertyType propertyType, PropertyCacheValue cacheValue)
        {
            return PropertyCacheLevel.Content;
        }
    }
}