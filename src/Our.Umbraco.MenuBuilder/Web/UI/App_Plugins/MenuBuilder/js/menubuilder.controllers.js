angular.module("umbraco").controller("Our.Umbraco.MenuBuilder.Controllers.DocTypePickerController", [

    "$scope",
    "Our.Umbraco.MenuBuilder.Resources.MenuBuilderResources",

    function ($scope, mbResources) {

        $scope.add = function () {
            $scope.model.value.push({
                // All stored content type aliases must be prefixed "mb" for easier recognition.
                // For good measure we'll also prefix the tab alias "mb" 
                mbAlias: "",
                mbTabAlias: "",
                nameTemplate: ""
            }
            );
        }

        $scope.selectedDocTypeTabs = function (cfg) {
            var dt = _.find($scope.model.docTypes, function (itm) {
                return itm.alias.toLowerCase() == cfg.mbAlias.toLowerCase();
            });
            var tabs = dt ? dt.tabs : [];
            if (!_.contains(tabs, cfg.mbTabAlias)) {
                cfg.mbTabAlias = tabs[0];
            }
            return tabs;
        }

        $scope.remove = function (index) {
            $scope.model.value.splice(index, 1);
        }

        $scope.sortableOptions = {
            axis: 'y',
            cursor: "move",
            handle: ".icon-navigation"
        };

        mbResources.getContentTypes().then(function (docTypes) {
            $scope.model.docTypes = docTypes;
        });

        if (!$scope.model.value) {
            $scope.model.value = [];
            $scope.add();
        }
    }
]);

angular.module("umbraco").requires.push('ng-nestable');

angular.module("umbraco").controller("Our.Umbraco.MenuBuilder.Controllers.MenuBuilderPropertyEditorController", [

    "$scope",
    "$nestable",
    "$interpolate",
    "$filter",
    "contentResource",
    "localizationService",
    "Our.Umbraco.MenuBuilder.Resources.MenuBuilderResources",

    function ($scope, $nestable, $interpolate, $filter, contentResource, localizationService, mbResources) {

        $nestable.enableDraggableHandle = true;

        if (!$scope.model.value) {
            $scope.model.value = [];
        }

        $scope.add = function () {

            var idx = $scope.model.value.length;

            $scope.model.value.push({ item: { name: "Item " + idx } });

            console.log("added item");
        };

        $scope.remove = function () {

            console.log("removed item", arguments);

        }

    }

]);