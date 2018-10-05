using ClientDependency.Core;
using Our.Umbraco.InnerContent.PropertyEditors;
using Umbraco.Core.PropertyEditors;
using Umbraco.Web.PropertyEditors;

namespace Our.Umbraco.MenuBuilder.PropertyEditors
{
    [PropertyEditorAsset(ClientDependencyType.Javascript, "~/App_Plugins/MenuBuilder/js/jquery.nestable.js")]
    [PropertyEditorAsset(ClientDependencyType.Javascript, "~/App_Plugins/MenuBuilder/js/angular-nestable.js")]
    [PropertyEditorAsset(ClientDependencyType.Javascript, "~/App_Plugins/MenuBuilder/js/menubuilder.js")]
    [PropertyEditorAsset(ClientDependencyType.Css, "~/App_Plugins/MenuBuilder/css/menubuilder.css")]
    [PropertyEditor(PropertyEditorAlias, PropertyEditorName, PropertyEditorValueTypes.Json, PropertyEditorViewPath, Group = "lists", Icon = "icon-umb-contour")]
    public class MenuBuilderPropertyEditor : SimpleInnerContentPropertyEditor
    {
        public const string PropertyEditorAlias = "Our.Umbraco.MenuBuilder2";
        public const string PropertyEditorName = "Menu Builder";
        public const string PropertyEditorViewPath = "~/App_Plugins/MenuBuilder/views/menubuilder.html";

        public MenuBuilderPropertyEditor()
            : base()
        {
            DefaultPreValues.Add("maxDepth", 0);
            DefaultPreValues.Add("hideLabel", "0");
        }

        protected override PreValueEditor CreatePreValueEditor()
        {
            return new MenuBuilderPreValueEditor();
        }

        internal class MenuBuilderPreValueEditor : SimpleInnerContentPreValueEditor
        {
            public MenuBuilderPreValueEditor()
                : base()
            {
                Fields.Add("maxDepth", "Max Depth", "number", "Set the maximum depth of the menu hieraricy.");
                Fields.AddHideLabel();
            }
        }
    }
}