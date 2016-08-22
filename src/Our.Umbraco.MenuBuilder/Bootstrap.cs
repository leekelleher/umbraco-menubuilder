using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Our.Umbraco.MenuBuilder
{
    public class Bootstrap : ApplicationEventHandler
    {
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            DataTypeService.Saved += ExpireCache;
        }

        private void ExpireCache(IDataTypeService sender, SaveEventArgs<IDataTypeDefinition> e)
        {
            foreach (var dataType in e.SavedEntities)
            {
                ApplicationContext.Current.ApplicationCache.RuntimeCache.ClearCacheItem(
                    string.Concat("Our.Umbraco.MenuBuilder.GetPreValuesCollectionByDataTypeId_", dataType.Id));
            }
        }
    }
}