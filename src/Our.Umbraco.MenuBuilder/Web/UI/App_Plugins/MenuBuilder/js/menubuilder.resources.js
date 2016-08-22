angular.module('umbraco.resources').factory('Our.Umbraco.MenuBuilder.Resources.MenuBuilderResources',
    function ($q, $http, umbRequestHelper) {
        return {
            // TODO: [LK] Review this, as I believe Umbraco 7.4+ now has a service for this.
            getContentTypes: function () {
                var url = "/umbraco/backoffice/MenuBuilder/MenuBuilderApi/GetContentTypes";
                return umbRequestHelper.resourcePromise(
                    $http.get(url),
                    'Failed to retrieve content types'
                );
            },
        };
    });