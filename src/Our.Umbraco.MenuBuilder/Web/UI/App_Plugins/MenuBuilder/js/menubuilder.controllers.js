// Prevalue Editors
angular.module("umbraco").controller("Our.Umbraco.MenuBuilder.Controllers.DocTypePickerController", [

    "$scope",
    "Our.Umbraco.MenuBuilder.Resources.MenuBuilderResources",

    function ($scope, mbResources) {

        $scope.add = function () {
            $scope.model.value.push({
                // All stored content type aliases must be prefixed "mb" for easier recognition.
                // For good measure we'll also prefix the tab alias "mb" 
                mbContentTypeAlias: "",
                mbTabAlias: "",
                nameTemplate: ""
            }
            );
        }

        $scope.selectedDocTypeTabs = function (cfg) {
            var dt = _.find($scope.model.docTypes, function (itm) {
                return itm.alias.toLowerCase() == cfg.mbContentTypeAlias.toLowerCase();
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

// Property Editors
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

        var inited = false;

        // Interpolate the name templates
        _.each($scope.model.config.contentTypes, function (contentType) {
            contentType.nameExp = !!contentType.nameTemplate
                ? $interpolate(contentType.nameTemplate)
                : undefined;
        });

        // Declare variables
        $scope.items = [];
        $scope.scaffolds = [];
        $scope.maxDepth = $scope.model.config.maxDepth || 10;
        $scope.wideMode = $scope.model.config.hideLabel == "1";

        // Declare scope functions
        $scope.openContentTypePickerOverlay = function (event) {

            // this could be used for future limiting on node types
            var scaffolds = [];
            _.each($scope.scaffolds, function (scaffold) {
                var icon = scaffold.icon;
                // workaround for when no icon is chosen for a doctype
                if (icon === ".sprTreeFolder") {
                    icon = "icon-folder";
                }
                scaffolds.push({
                    alias: scaffold.contentTypeAlias,
                    name: scaffold.contentTypeName,
                    icon: icon
                });
            });

            if (scaffolds.length == 0) {
                return;
            }

            if (scaffolds.length == 1) {
                // only one scaffold type - no need to display the picker
                addItem(scaffolds[0].alias);
                $scope.closeContentTypePickerOverlay();
                return;
            }

            $scope.contentTypePickerOverlay = {
                view: "itempicker",
                filter: false,
                title: localizationService.localize("grid_insertControl"), // Should probably use a NC specific string, but for now re-using the grid title
                availableItems: scaffolds,
                event: event,
                show: true,
                submit: function (model) {
                    addItem(model.selectedItem.alias);
                    $scope.closeContentTypePickerOverlay();
                }
            };
        };

        $scope.closeContentTypePickerOverlay = function () {
            if ($scope.contentTypePickerOverlay) {
                $scope.contentTypePickerOverlay.show = false;
                $scope.contentTypePickerOverlay = null;
            }
        };

        $scope.openContentEditorOverlay = function(dialogData, callback) {
            
            $scope.contentEditorOverlay = {
                view: Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + "/MenuBuilder/views/menubuilder.dialog.html",
                title: dialogData.isNew ? "Create item" : "Edit item '"+ dialogData.item.name +"'",  
                show: true,
                dialogData: dialogData,
                submit: function (model) {
                    if (callback) {
                        callback(model);
                    }
                    $scope.closeContentEditorOverlay();
                }
            };

        }

        $scope.closeContentEditorOverlay = function() {
            if ($scope.contentEditorOverlay) {
                $scope.contentEditorOverlay.show = false;
                $scope.contentEditorOverlay = null;
            }
        }

        $scope.editItem = function(item) {
            
            var dialogData = {
                item: item,
                isNew: false
            }

            $scope.openContentEditorOverlay(dialogData, function (model) {
                var item = model.dialogData.item;
                generateName(item);
            });

        }

        // Helpers
        var UUID = (function () {
            var self = {};
            var lut = []; for (var i = 0; i < 256; i++) { lut[i] = (i < 16 ? '0' : '') + (i).toString(16); }
            self.generate = function () {
                var d0 = Math.random() * 0xffffffff | 0;
                var d1 = Math.random() * 0xffffffff | 0;
                var d2 = Math.random() * 0xffffffff | 0;
                var d3 = Math.random() * 0xffffffff | 0;
                return lut[d0 & 0xff] + lut[d0 >> 8 & 0xff] + lut[d0 >> 16 & 0xff] + lut[d0 >> 24 & 0xff] + '-' +
                  lut[d1 & 0xff] + lut[d1 >> 8 & 0xff] + '-' + lut[d1 >> 16 & 0x0f | 0x40] + lut[d1 >> 24 & 0xff] + '-' +
                  lut[d2 & 0x3f | 0x80] + lut[d2 >> 8 & 0xff] + '-' + lut[d2 >> 16 & 0xff] + lut[d2 >> 24 & 0xff] +
                  lut[d3 & 0xff] + lut[d3 >> 8 & 0xff] + lut[d3 >> 16 & 0xff] + lut[d3 >> 24 & 0xff];
            }
            return self;
        })();

        var getScaffold = function (alias) {
            return _.find($scope.scaffolds, function (scaffold) {
                return scaffold.contentTypeAlias == alias;
            });
        }

        var getContentTypeConfig = function (alias) {
            return _.find($scope.model.config.contentTypes, function (cfg) {
                return cfg.mbContentTypeAlias == alias;
            });
        }

        var countItems = function(itms) {
            var count = 0;
            _.each(itms, function(itm) {
                count++;
                if (itm.children) {
                    count += countItems(itm.children);
                }
            });
            return count;
        }

        var generateName = function (item) {

            var cfg = getContentTypeConfig(item.mbContentTypeAlias);
            if (cfg) {

                var nameExp = !!cfg.nameTemplate
                    ? $interpolate(cfg.nameTemplate)
                    : undefined;

                if (nameExp) {

                    // Copy property values
                    var value = {};

                    for (var t = 0; t < item.tabs.length; t++) {
                        var tab = item.tabs[t];
                        for (var p = 0; p < tab.properties.length; p++) {
                            var prop = tab.properties[p];
                            if (typeof prop.value !== "function") {
                                value[prop.propertyAlias] = prop.value;
                            }
                        }
                    }

                    // Pass data through name template
                    var newName = nameExp(value); // Run it against the stored dictionary value, NOT the node object
                    if (newName && (newName = $.trim(newName)) && item.name != newName) {
                        item.name = newName;
                    }

                } 
            }
        }

        var initItem = function (scaffold, existingItem) {
            var item = angular.copy(scaffold);

            item.key = existingItem && existingItem.key ? existingItem.key : UUID.generate();
            item.mbContentTypeAlias = scaffold.contentTypeAlias;

            // Give the node a default name, we'll override it later if a name template exists
            item.name = "Item " + (countItems($scope.items) + 1);

            for (var t = 0; t < item.tabs.length; t++) {
                var tab = item.tabs[t];
                for (var p = 0; p < tab.properties.length; p++) {

                    var prop = tab.properties[p];
                    prop.propertyAlias = prop.alias;
                    prop.alias = $scope.model.alias + "___" + prop.alias;

                    // Copy existing property values
                    if (existingItem) {
                        item.name = existingItem.name;
                        if (existingItem[prop.propertyAlias]) {
                            prop.value = existingItem[prop.propertyAlias];
                        }
                    }
                }
            }

            return item;
        }

        var addItem = function (alias) {
            
            var scaffold = getScaffold(alias);
            var newNode = initItem(scaffold, null);

            var dialogData = {
                item: newNode,
                isNew: true
            }

            $scope.openContentEditorOverlay(dialogData, function (model) {
                var item = model.dialogData.item;
                generateName(item);
                $scope.items.push({ item: item });
            });

        }

        var updateModelRecursive = function(items) {
            var newValues = [];
            for (var i = 0; i < items.length; i++) {

                var item = items[i].item;
                var newValue = {
                    key: item.key,
                    name: item.name,
                    mbContentTypeAlias: item.mbContentTypeAlias
                };

                for (var t = 0; t < item.tabs.length; t++) {
                    var tab = item.tabs[t];
                    for (var p = 0; p < tab.properties.length; p++) {
                        var prop = tab.properties[p];
                        if (typeof prop.value !== "function") {
                            newValue[prop.propertyAlias] = prop.value;
                        }
                    }
                }

                if (items[i].children) {
                    newValue.children = updateModelRecursive(items[i].children);
                }

                newValues.push(newValue);
            }
            return newValues;
        };

        var updateModel = function () {
            if (inited) {
                var newValues = updateModelRecursive($scope.items);
                console.log(newValues);
                $scope.model.value = newValues;
            }
        }

        // Handle destruction
        var unsubscribe = $scope.$on("formSubmitting", function (ev, args) {
            updateModel();
        });

        $scope.$on('$destroy', function () {
            unsubscribe();
        });

        // Initialize
        var scaffoldsLoaded = 0;
        _.each($scope.model.config.contentTypes, function (contentType) {
            contentResource.getScaffold(-20, contentType.mbContentTypeAlias).then(function (scaffold) {

                // remove all tabs except the specified tab
                var tab = _.find(scaffold.tabs, function (tab) {
                    return tab.id != 0 && (tab.alias.toLowerCase() == contentType.mbTabAlias.toLowerCase() || contentType.mbTabAlias == "");
                });
                scaffold.tabs = [];
                if (tab) {
                    scaffold.tabs.push(tab);
                }

                // Store the scaffold object
                $scope.scaffolds.push(scaffold);

                scaffoldsLoaded++;
                initIfAllScaffoldsHaveLoaded();
            }, function (error) {
                scaffoldsLoaded++;
                initIfAllScaffoldsHaveLoaded();
            });
        });

        var initViewModelRecursive = function (items) {
            var models = [];
            for (var i = 0; i < items.length; i++) {
                var item = items[i];
                var scaffold = getScaffold(item.mbContentTypeAlias);
                if (scaffold == null) {
                    // No such scaffold - the content type might have been deleted. We need to skip it.
                    continue;
                }
                var model = { item: initItem(scaffold, item) };

                if (item.children) {
                    model.children = initViewModelRecursive(item.children);
                }

                models.push(model);
            }
            return models;
        }

        var initIfAllScaffoldsHaveLoaded = function () {
            // Initialize when all scaffolds have loaded
            if ($scope.model.config.contentTypes.length === scaffoldsLoaded) {
                // Because we're loading the scaffolds async one at a time, we need to 
                // sort them explicitly according to the sort order defined by the data type.
                var contentTypeAliases = [];
                _.each($scope.model.config.contentTypes, function (contentType) {
                    contentTypeAliases.push(contentType.ncAlias);
                });
                $scope.scaffolds = $filter('orderBy')($scope.scaffolds, function (s) {
                    return contentTypeAliases.indexOf(s.contentTypeAlias);
                });

                console.log($scope.model.value);

                // Convert stored value
                if ($scope.model.value) {
                    $scope.items = initViewModelRecursive($scope.model.value);
                }

                inited = true;
            }
        }

    }

]);

angular.module("umbraco").controller("Our.Umbraco.MenuBuilder.MenuBuilderDialogController",
    [
        "$scope",
        "$interpolate",
        "formHelper",
        "contentResource",

        function ($scope, $interpolate, formHelper, contentResource) {
            $scope.item = $scope.model.dialogData.item;
        }

    ]);