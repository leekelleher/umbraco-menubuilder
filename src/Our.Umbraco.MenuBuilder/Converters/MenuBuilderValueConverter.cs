using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    [PropertyValueCache(PropertyCacheValue.All, PropertyCacheLevel.Content)]
    public class MenuBuilderValueConverter : PropertyValueConverterBase
    {
        protected IEnumerable<IPublishedContent> ConvertDataToSourceRecrusive(JArray items, IPublishedContent parentNode = null, int level = 0, bool preview = false)
        {
            var nodes = new List<IPublishedContent>();

            for (var i = 0; i < items.Count; i++)
            {
                var item = (JObject)items[i];

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
                        properties.Add(new DetachedPublishedProperty(propType, jProp.Value, preview));
                    }
                }

                // Parse out the name manually
                object nameObj = null;
                if (propValues.TryGetValue("name", out nameObj))
                {
                    // Do nothing, we just want to parse out the name if we can
                }

                // Parse out key manually
                object keyObj = null;
                if (propValues.TryGetValue("key", out keyObj))
                {
                    // Do nothing, we just want to parse out the key if we can
                }

                // Get the current request node we are embedded in
                var pcr = UmbracoContext.Current.PublishedContentRequest;
                var containerNode = pcr != null && pcr.HasPublishedContent ? pcr.PublishedContent : null;

                var node = new DetachedPublishedContent(
                    keyObj == null ? Guid.Empty : Guid.Parse(keyObj.ToString()),
                    nameObj == null ? null : nameObj.ToString(),
                    publishedContentType,
                    properties.ToArray(),
                    containerNode,
                    parentNode,
                    i,
                    level,
                    preview);

                // Process children
                if (propValues.ContainsKey("children"))
                {
                    var children = ConvertDataToSourceRecrusive((JArray)propValues["children"], node, level + 1, preview);
                    node.SetChildren(children);
                }

                nodes.Add(node);
            }

            return nodes;
        }

        public override object ConvertDataToSource(PublishedPropertyType propertyType, object source, bool preview)
        {
            try
            {
                if (source != null && !source.ToString().IsNullOrWhiteSpace())
                {
                    var rawValue = JsonConvert.DeserializeObject<JArray>(source.ToString());
                    return ConvertDataToSourceRecrusive(rawValue, null, 1, preview);
                }

            }
            catch (Exception e)
            {
                LogHelper.Error<MenuBuilderValueConverter>("Error converting value", e);
            }

            return null;
        }

        public override bool IsConverter(PublishedPropertyType propertyType)
        {
            return propertyType.PropertyEditorAlias.InvariantEquals(MenuBuilderPropertyEditor.PropertyEditorAlias);
        }
    }
}