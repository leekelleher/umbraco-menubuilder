using System;
using System.Collections.Generic;
using System.Linq;
using Our.Umbraco.MenuBuilder.Extensions;
using Umbraco.Web.Editors;
using Umbraco.Web.Mvc;
using System.Web.Http.ModelBinding;

namespace Our.Umbraco.MenuBuilder.Web.Controllers
{
    [PluginController("MenuBuilder")]
    public class MenuBuilderApiController : UmbracoAuthorizedJsonController
    {
        // TODO: [LK] Review this, as I believe Umbraco 7.4+ now has a service for this.
        [System.Web.Http.HttpGet]
        public IEnumerable<object> GetContentTypes()
        {
            return Services.ContentTypeService.GetAllContentTypes()
                .OrderBy(x => x.SortOrder)
                .Select(x => new
                {
                    id = x.Id,
                    guid = x.Key,
                    name = x.Name,
                    alias = x.Alias,
                    icon = x.Icon,
                    tabs = x.CompositionPropertyGroups.Select(y => y.Name).Distinct()
                });
        }
    }
}
