angular.module("umbraco").requires.push("ng-nestable");
angular.module("umbraco").controller("Our.Umbraco.MenuBuilder.Controllers.PropertyEditorController", [

    "$scope",
    "$nestable",
    "innerContentService",

    function ($scope, $nestable, innerContentService) {

        var vm = this;

        vm.items = [];

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
                console.log("callback", data);

                innerContentService.populateName(data.model, 0, $scope.model.config.contentTypes);

                if (!($scope.model.value instanceof Array)) {
                    $scope.model.value = [];
                }

                if (data.action === "add") {

                    $scope.model.value.push(data.model);

                    vm.items.push({ item: data.model, children: [] });

                } else if (data.action === "edit") {
                    $scope.model.value[data.idx] = data.model;
                    vm.items[data.idx].item = data.model;
                }
            }
        };

        vm.add = add;
        vm.edit = edit;
        vm.remove = remove;

        function add($event) {
            vm.overlayConfig.event = $event;
            vm.overlayConfig.data = { model: null, action: "add" };
            vm.overlayConfig.show = true;
        };

        function edit(item) {
            vm.overlayConfig.event = $event;
            vm.overlayConfig.data = { model: item, action: "edit" };
            vm.overlayConfig.show = true;
        };

        function remove(item) {
            //$scope.model.value.splice($index, 1);
            // TODO: Find the `item` and remove it
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

                _.each($scope.model.value, function (item) {
                    vm.items.push({ item: item, children: [] });
                });
            }
        }

        initialize();

    }
]);
