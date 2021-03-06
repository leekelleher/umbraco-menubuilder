﻿using Newtonsoft.Json.Linq;
using Our.Umbraco.MenuBuilder.PropertyEditors;
using Umbraco.Core;
using Umbraco.Core.Models;

namespace Our.Umbraco.MenuBuilder.Helpers
{
    internal static class MenuBuilderHelper
    {
        public static PreValueCollection GetPreValuesCollectionByDataTypeId(int dtdId)
        {
            var preValueCollection = (PreValueCollection)ApplicationContext.Current.ApplicationCache.RuntimeCache.GetCacheItem(
                string.Concat("Our.Umbraco.MenuBuilder.GetPreValuesCollectionByDataTypeId_", dtdId),
                () => ApplicationContext.Current.Services.DataTypeService.GetPreValuesCollectionByDataTypeId(dtdId));

            return preValueCollection;
        }

        public static string GetContentTypeAliasFromItem(JObject item)
        {
            if (item == null) return null;

            var contentTypeAliasProperty = item[MenuBuilderPropertyEditor.ContentTypeAliasPropertyKey];
            if (contentTypeAliasProperty == null)
            {
                return null;
            }

            return contentTypeAliasProperty.ToObject<string>();
        }

        public static IContentType GetContentTypeFromItem(JObject item)
        {
            var contentTypeAlias = GetContentTypeAliasFromItem(item);
            if (string.IsNullOrEmpty(contentTypeAlias))
            {
                return null;
            }

            return ApplicationContext.Current.Services.ContentTypeService.GetContentType(contentTypeAlias);
        }
    }
}