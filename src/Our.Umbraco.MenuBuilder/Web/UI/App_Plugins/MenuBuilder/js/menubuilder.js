angular.module("umbraco").requires.push("ng-nestable");
angular.module("umbraco").controller("Our.Umbraco.MenuBuilder.Controllers.PropertyEditorController", [

    "$scope",
    "$nestable",
    "innerContentService",

    function ($scope, $nestable, innerContentService) {

        var vm = this;

        vm.items = [];

        vm.showIcons = $scope.model.config.showIcons || true;

        vm.nestableOptions = {
            maxDepth: $scope.model.config.maxDepth || 10
        };

        vm.overlayConfig = {
            propertyAlias: $scope.model.alias,
            contentTypes: $scope.model.config.contentTypes,
            show: false,
            data: {
                idx: 0,
                model: null
            },
            callback: function (data) {
                innerContentService.populateName(data.model, data.idx, $scope.model.config.contentTypes);

                if (!($scope.model.value instanceof Array)) {
                    $scope.model.value = [];
                }

                if (data.action === "add") {
                    $scope.model.value.push(data.model);
                } else if (data.action === "edit") {
                    $scope.model.value[data.idx] = data.model;
                }
            }
        };

        vm.add = add;
        vm.edit = edit;
        vm.remove = remove;

        function add($event) {
            vm.overlayConfig.event = $event;
            vm.overlayConfig.data = { model: null, idx: $scope.model.value.length, action: "add" };
            vm.overlayConfig.show = true;
        };

        function edit($event, $index, item) {
            vm.overlayConfig.event = $event;
            vm.overlayConfig.data = { model: item, idx: $index, action: "edit" };
            vm.overlayConfig.show = true;
        };

        function remove($index) {
            $scope.model.value.splice($index, 1);
            setDirty();
        };

        function setDirty() {
            if ($scope.propertyForm) {
                $scope.propertyForm.$setDirty();
            }
        };

        function initialize() {

            $nestable.enableDraggableHandle = true;
            $scope.model.value = $scope.model.value || [];

            if ($scope.model.value.length > 0) {

                // Sync icons, as as it may have changed on the doctype
                var guids = _.uniq($scope.model.value.map(function (itm) {
                    return itm.icContentTypeGuid;
                }));

                innerContentService.getContentTypeIconsByGuid(guids).then(function (data) {
                    _.each($scope.model.value, function (itm) {
                        if (data.hasOwnProperty(itm.icContentTypeGuid)) {
                            itm.icon = data[itm.icContentTypeGuid];
                        }
                    });
                });

            }
        }

        initialize();

    }
]);
