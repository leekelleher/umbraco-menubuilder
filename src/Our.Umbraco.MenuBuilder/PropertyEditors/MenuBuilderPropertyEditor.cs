using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using ClientDependency.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Our.Umbraco.MenuBuilder.Extensions;
using Our.Umbraco.MenuBuilder.Helpers;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Editors;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;
using Umbraco.Web.PropertyEditors;

namespace Our.Umbraco.MenuBuilder.PropertyEditors
{
    [PropertyEditorAsset(ClientDependencyType.Javascript, "/App_Plugins/MenuBuilder/js/jquery.nestable.js")]
    [PropertyEditorAsset(ClientDependencyType.Javascript, "/App_Plugins/MenuBuilder/js/angular-nestable.js")]
    [PropertyEditorAsset(ClientDependencyType.Javascript, "/App_Plugins/MenuBuilder/js/menubuilder.resources.js")]
    [PropertyEditorAsset(ClientDependencyType.Javascript, "/App_Plugins/MenuBuilder/js/menubuilder.directives.js")]
    [PropertyEditorAsset(ClientDependencyType.Javascript, "/App_Plugins/MenuBuilder/js/menubuilder.controllers.js")]
    [PropertyEditorAsset(ClientDependencyType.Css, "/App_Plugins/MenuBuilder/css/menubuilder.css")]
    [PropertyEditor(PropertyEditorAlias, "Menu Builder", "/App_Plugins/MenuBuilder/html/menubuilder.html", ValueType = "JSON")]
    public class MenuBuilderPropertyEditor : PropertyEditor
    {
        internal const string ContentTypeAliasPropertyKey = "mbContentTypeAlias";

        public const string PropertyEditorAlias = "Our.Umbraco.MenuBuilder";

        private IDictionary<string, object> _defaultPreValues;
        public override IDictionary<string, object> DefaultPreValues
        {
            get { return _defaultPreValues; }
            set { _defaultPreValues = value; }
        }

        public MenuBuilderPropertyEditor()
        {
            // Setup default values
            _defaultPreValues = new Dictionary<string, object>
            {
                { MenuBuilderPreValueEditor.ContentTypesPreValueKey, "" },
                { "maxDepth", 0 },
                { "confirmDeletes", "1" },
                { "showIcons", "1" }
            };
        }

        #region Pre Value Editor

        protected override PreValueEditor CreatePreValueEditor()
        {
            return new MenuBuilderPreValueEditor();
        }

        internal class MenuBuilderPreValueEditor : PreValueEditor
        {
            internal const string ContentTypesPreValueKey = "contentTypes";

            [PreValueField(ContentTypesPreValueKey, "Doc Types", "/App_Plugins/MenuBuilder/views/menubuilder.doctypepicker.html", Description = "Select the doc types to use as the data blueprint.")]
            public string[] ContentTypes { get; set; }

            [PreValueField("maxDepth", "Max Depth", "number", Description = "Set the maximum depth of the menu hieraricy.")]
            public string MaxDepth { get; set; }

            [PreValueField("confirmDeletes", "Confirm Deletes", "boolean", Description = "Set whether item deletions should require confirming.")]
            public string ConfirmDeletes { get; set; }

            [PreValueField("showIcons", "Show Icons", "boolean", Description = "Set whether to show the items doc type icon in the list.")]
            public string ShowIcons { get; set; }

            [PreValueField("hideLabel", "Hide Label", "boolean", Description = "Set whether to hide the editor label and have the list take up the full width of the editor window.")]
            public string HideLabel { get; set; }
        }

        #endregion

        #region Value Editor

        protected override PropertyValueEditor CreateValueEditor()
        {
            return new MenuBuilderPropertyValueEditor(base.CreateValueEditor());
        }

        internal class MenuBuilderPropertyValueEditor : PropertyValueEditorWrapper
        {
            public MenuBuilderPropertyValueEditor(PropertyValueEditor wrapped)
                : base(wrapped)
            {
                Validators.Add(new MenuBuilderValidator());
            }

            internal ServiceContext Services
            {
                get { return ApplicationContext.Current.Services; }
            }

            public override void ConfigureForDisplay(PreValueCollection preValues)
            {
                base.ConfigureForDisplay(preValues);

                var asDictionary = preValues.AsPreValueDictionary();
                if (asDictionary.ContainsKey("hideLabel"))
                {
                    var boolAttempt = asDictionary["hideLabel"].TryConvertTo<bool>();
                    if (boolAttempt.Success)
                    {
                        HideLabel = boolAttempt.Result;
                    }
                }
            }

            #region DB to String

            public override string ConvertDbToString(Property property, PropertyType propertyType, IDataTypeService dataTypeService)
            {
                // Convert / validate value
                if (property.Value == null || string.IsNullOrWhiteSpace(property.Value.ToString()))
                    return string.Empty;

                var value = JsonConvert.DeserializeObject<List<object>>(property.Value.ToString());
                if (value == null)
                    return string.Empty;

                // Process value
                PreValueCollection preValues = null;
                for (var i = 0; i < value.Count; i++)
                {
                    var o = value[i];
                    var propValues = ((JObject)o);

                    var contentType = MenuBuilderHelper.GetContentTypeFromItem(propValues);
                    if (contentType == null)
                    {
                        continue;
                    }

                    var propValueKeys = propValues.Properties().Select(x => x.Name).ToArray();

                    foreach (var propKey in propValueKeys)
                    {
                        var propType = contentType.CompositionPropertyTypes.FirstOrDefault(x => x.Alias == propKey);
                        if (propType == null)
                        {
                            if (IsSystemPropertyKey(propKey) == false)
                            {
                                // Property missing so just delete the value
                                propValues[propKey] = null;
                            }
                        }
                        else
                        {
                            // Create a fake property using the property abd stored value
                            var prop = new Property(propType, propValues[propKey] == null ? null : propValues[propKey].ToString());

                            // Lookup the property editor
                            var propEditor = PropertyEditorResolver.Current.GetByAlias(propType.PropertyEditorAlias);

                            // Get the editor to do it's conversion, and store it back
                            propValues[propKey] = propEditor.ValueEditor.ConvertDbToString(prop, propType,
                                ApplicationContext.Current.Services.DataTypeService);
                        }

                    }
                }

                // Update the value on the property
                property.Value = JsonConvert.SerializeObject(value);

                // Pass the call down
                return base.ConvertDbToString(property, propertyType, dataTypeService);
            }

            #endregion

            #region DB to Editor

            public override object ConvertDbToEditor(Property property, PropertyType propertyType, IDataTypeService dataTypeService)
            {
                if (property.Value == null || string.IsNullOrWhiteSpace(property.Value.ToString()))
                    return string.Empty;

                var value = JsonConvert.DeserializeObject<List<object>>(property.Value.ToString());
                if (value == null)
                    return string.Empty;

                // Process value
                PreValueCollection preValues = null;
                for (var i = 0; i < value.Count; i++)
                {
                    var o = value[i];
                    var propValues = ((JObject)o);

                    var contentType = MenuBuilderHelper.GetContentTypeFromItem(propValues);
                    if (contentType == null)
                    {
                        continue;
                    }

                    var propValueKeys = propValues.Properties().Select(x => x.Name).ToArray();

                    foreach (var propKey in propValueKeys)
                    {
                        var propType = contentType.CompositionPropertyTypes.FirstOrDefault(x => x.Alias == propKey);
                        if (propType == null)
                        {
                            if (IsSystemPropertyKey(propKey) == false)
                            {
                                // Property missing so just delete the value
                                propValues[propKey] = null;
                            }
                        }
                        else
                        {
                            // Create a fake property using the property abd stored value
                            var prop = new Property(propType, propValues[propKey] == null ? null : propValues[propKey].ToString());

                            // Lookup the property editor
                            var propEditor = PropertyEditorResolver.Current.GetByAlias(propType.PropertyEditorAlias);

                            // Get the editor to do it's conversion
                            var newValue = propEditor.ValueEditor.ConvertDbToEditor(prop, propType,
                                ApplicationContext.Current.Services.DataTypeService);

                            // Store the value back
                            propValues[propKey] = (newValue == null) ? null : JToken.FromObject(newValue);
                        }

                    }
                }

                // Update the value on the property
                property.Value = JsonConvert.SerializeObject(value);

                // Pass the call down
                return base.ConvertDbToEditor(property, propertyType, dataTypeService);
            }

            #endregion

            #region Editor to DB

            public override object ConvertEditorToDb(ContentPropertyData editorValue, object currentValue)
            {
                if (editorValue.Value == null || string.IsNullOrWhiteSpace(editorValue.Value.ToString()))
                    return null;

                var value = JsonConvert.DeserializeObject<List<object>>(editorValue.Value.ToString());
                if (value == null)
                    return null;

                // Issue #38 - Keep recursive property lookups working
                if (!value.Any())
                    return null;

                // Process value
                for (var i = 0; i < value.Count; i++)
                {
                    var o = value[i];
                    var propValues = ((JObject)o);

                    var contentType = MenuBuilderHelper.GetContentTypeFromItem(propValues);
                    if (contentType == null)
                    {
                        continue;
                    }

                    var propValueKeys = propValues.Properties().Select(x => x.Name).ToArray();

                    foreach (var propKey in propValueKeys)
                    {
                        var propType = contentType.CompositionPropertyTypes.FirstOrDefault(x => x.Alias == propKey);
                        if (propType == null)
                        {
                            if (IsSystemPropertyKey(propKey) == false)
                            {
                                // Property missing so just delete the value
                                propValues[propKey] = null;
                            }
                        }
                        else
                        {
                            // Fetch the property types prevalue
                            var propPreValues = Services.DataTypeService.GetPreValuesCollectionByDataTypeId(
                                    propType.DataTypeDefinitionId);

                            // Lookup the property editor
                            var propEditor = PropertyEditorResolver.Current.GetByAlias(propType.PropertyEditorAlias);

                            // Create a fake content property data object
                            var contentPropData = new ContentPropertyData(
                                propValues[propKey], propPreValues,
                                new Dictionary<string, object>());

                            // Get the property editor to do it's conversion
                            var newValue = propEditor.ValueEditor.ConvertEditorToDb(contentPropData, propValues[propKey]);

                            // Store the value back
                            propValues[propKey] = (newValue == null) ? null : JToken.FromObject(newValue);
                        }

                    }
                }

                return JsonConvert.SerializeObject(value);
            }

            #endregion
        }

        internal class MenuBuilderValidator : IPropertyValidator
        {
            public IEnumerable<ValidationResult> Validate(object rawValue, PreValueCollection preValues, PropertyEditor editor)
            {
                var value = JsonConvert.DeserializeObject<List<object>>(rawValue.ToString());
                if (value == null)
                    yield break;

                IDataTypeService dataTypeService = ApplicationContext.Current.Services.DataTypeService;
                for (var i = 0; i < value.Count; i++)
                {
                    var o = value[i];
                    var propValues = ((JObject)o);

                    var contentType = MenuBuilderHelper.GetContentTypeFromItem(propValues);
                    if (contentType == null)
                    {
                        continue;
                    }

                    var propValueKeys = propValues.Properties().Select(x => x.Name).ToArray();

                    foreach (var propKey in propValueKeys)
                    {
                        var propType = contentType.CompositionPropertyTypes.FirstOrDefault(x => x.Alias == propKey);
                        if (propType != null)
                        {
                            PreValueCollection propPrevalues = dataTypeService.GetPreValuesCollectionByDataTypeId(propType.DataTypeDefinitionId);
                            PropertyEditor propertyEditor = PropertyEditorResolver.Current.GetByAlias(propType.PropertyEditorAlias);

                            foreach (IPropertyValidator validator in propertyEditor.ValueEditor.Validators)
                            {
                                foreach (ValidationResult result in validator.Validate(propValues[propKey], propPrevalues, propertyEditor))
                                {
                                    result.ErrorMessage = "Item " + (i + 1) + " '" + propType.Name + "' " + result.ErrorMessage;
                                    yield return result;
                                }
                            }

                            // Check mandatory
                            if (propType.Mandatory)
                            {
                                if (propValues[propKey] == null)
                                    yield return new ValidationResult("Item " + (i + 1) + " '" + propType.Name + "' cannot be null", new[] { propKey });
                                else if (propValues[propKey].ToString().IsNullOrWhiteSpace())
                                    yield return new ValidationResult("Item " + (i + 1) + " '" + propType.Name + "' cannot be empty", new[] { propKey });
                            }

                            // Check regex
                            if (!propType.ValidationRegExp.IsNullOrWhiteSpace()
                                && propValues[propKey] != null && !propValues[propKey].ToString().IsNullOrWhiteSpace())
                            {
                                var regex = new Regex(propType.ValidationRegExp);
                                if (!regex.IsMatch(propValues[propKey].ToString()))
                                {
                                    yield return new ValidationResult("Item " + (i + 1) + " '" + propType.Name + "' is invalid, it does not match the correct pattern", new[] { propKey });
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        private static bool IsSystemPropertyKey(string propKey)
        {
            return propKey == "name" || propKey == ContentTypeAliasPropertyKey;
        }
    }
}