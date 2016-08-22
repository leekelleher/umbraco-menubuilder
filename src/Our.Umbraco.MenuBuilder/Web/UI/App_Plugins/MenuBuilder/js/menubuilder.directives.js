angular.module("umbraco.directives").directive('menuBuilderEditor', [

    function () {

        var link = function ($scope, element, attrs, ctrl) {
            $scope.nodeContext = $scope.model = $scope.ngModel;

            var tab = $scope.ngModel.tabs[0];

            if ($scope.tabAlias) {
                angular.forEach($scope.ngModel.tabs, function (value, key) {
                    if (value.alias.toLowerCase() == $scope.tabAlias.toLowerCase()) {
                        tab = value;
                        return;
                    }
                });
            }

            $scope.tab = tab;

            var unsubscribe = $scope.$on("mbSyncVal", function (ev, args) {
                if (args.id === $scope.model.id) {
                    $scope.$broadcast("formSubmitting", { scope: $scope });
                }
            });

            $scope.$on('$destroy', function () {
                unsubscribe();
            });
        }

        return {
            restrict: "E",
            replace: true,
            templateUrl: "/App_Plugins/MenuBuilder/Views/menubuilder.editor.html",
            scope: {
                ngModel: '=',
                tabAlias: '='
            },
            link: link
        };

    }
]);
