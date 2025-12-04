// Knockout Mapping plugin v2.1.2
// (c) 2012 Steven Sanderson, Roy Jacobs - http://knockoutjs.com/
// License: MIT (http://www.opensource.org/licenses/mit-license.php)

(function (factory) {
	// Module systems magic dance.

	if (typeof require === "function" && typeof exports === "object" && typeof module === "object") {
		// CommonJS or Node: hard-coded dependency on "knockout"
		factory(require("knockout"), exports);
	} else if (typeof define === "function" && define["amd"]) {
		// AMD anonymous module with hard-coded dependency on "knockout"
		define(["knockout", "exports"], factory);
	} else {
		// <script> tag: use the global `ko` object, attaching a `mapping` property
		factory(ko, ko.mapping = {});
	}
}(function (ko, exports) {
	var DEBUG=true;
	var mappingProperty = "__ko_mapping__";
	var realKoDependentObservable = ko.dependentObservable;
	var mappingNesting = 0;
	var dependentObservables;
	var visitedObjects;

	var _defaultOptions = {
		include: ["_destroy"],
		ignore: [],
		copy: []
	};
	var defaultOptions = _defaultOptions;

	exports.isMapped = function (viewModel) {
		var unwrapped = ko.utils.unwrapObservable(viewModel);
		return unwrapped && unwrapped[mappingProperty];
	}

	exports.fromJS = function (jsObject /*, inputOptions, target*/ ) {
		if (arguments.length == 0) throw new Error("When calling ko.fromJS, pass the object you want to convert.");

		// When mapping is completed, even with an exception, reset the nesting level
		window.setTimeout(function () {
			mappingNesting = 0;
		}, 0);

		if (!mappingNesting++) {
			dependentObservables = [];
			visitedObjects = new objectLookup();
		}

		var options;
		var target;

		if (arguments.length == 2) {
			if (arguments[1][mappingProperty]) {
				target = arguments[1];
			} else {
				options = arguments[1];
			}
		}
		if (arguments.length == 3) {
			options = arguments[1];
			target = arguments[2];
		}

		if (target) {
			options = mergeOptions(target[mappingProperty], options);
		} else {
			options = mergeOptions(options);
		}
		options.mappedProperties = options.mappedProperties || {};

		var result = updateViewModel(target, jsObject, options);
		if (target) {
			result = target;
		}

		// Evaluate any dependent observables that were proxied.
		// Do this in a timeout to defer execution. Basically, any user code that explicitly looks up the DO will perform the first evaluation. Otherwise,
		// it will be done by this code.
		if (!--mappingNesting) {
			window.setTimeout(function () {
				while (dependentObservables.length) {
					var DO = dependentObservables.pop();
					if (DO) DO();
				}
			}, 0);
		}

		// Save any new mapping options in the view model, so that updateFromJS can use them later.
		result[mappingProperty] = mergeOptions(result[mappingProperty], options);

		return result;
	};

	exports.fromJSON = function (jsonString /*, options, target*/ ) {
		var parsed = ko.utils.parseJson(jsonString);
		arguments[0] = parsed;
		return exports.fromJS.apply(this, arguments);
	};

	exports.updateFromJS = function (viewModel) {
		throw new Error("ko.mapping.updateFromJS, use ko.mapping.fromJS instead. Please note that the order of parameters is different!");
	};

	exports.updateFromJSON = function (viewModel) {
		throw new Error("ko.mapping.updateFromJSON, use ko.mapping.fromJSON instead. Please note that the order of parameters is different!");
	};

	exports.toJS = function (rootObject, options) {
		if (arguments.length == 0) throw new Error("When calling ko.mapping.toJS, pass the object you want to convert.");
		// Merge in the options used in fromJS
		options = mergeOptions(rootObject[mappingProperty], options);

		// We just unwrap everything at every level in the object graph
		return visitModel(rootObject, function (x) {
			return ko.utils.unwrapObservable(x)
		}, options);
	};

	exports.toJSON = function (rootObject, options) {
		var plainJavaScriptObject = exports.toJS(rootObject, options);
		return ko.utils.stringifyJson(plainJavaScriptObject);
	};

	exports.visitModel = function (rootObject, callback, options) {
		if (arguments.length == 0) throw new Error("When calling ko.mapping.visitModel, pass the object you want to visit.");
		// Merge in the options used in fromJS
		options = mergeOptions(rootObject[mappingProperty], options);

		return visitModel(rootObject, callback, options);
	};

	exports.defaultOptions = function () {
		if (arguments.length > 0) {
			defaultOptions = arguments[0];
		} else {
			return defaultOptions;
		}
	};

	exports.resetDefaultOptions = function () {
		defaultOptions = {
			include: _defaultOptions.include.slice(0),
			ignore: _defaultOptions.ignore.slice(0),
			copy: _defaultOptions.copy.slice(0)
		};
	};

	exports.getType = function(x) {
		if ((x) && (typeof (x) === "object")) {
			if (x.constructor == (new Date).constructor) return "date";
			if (x.constructor == (new Array).constructor) return "array";
		}
		return typeof x;
	}

	function extendOptionsArray(distArray, sourceArray) {
		return ko.utils.arrayGetDistinctValues(
			ko.utils.arrayPushAll(distArray, sourceArray)
		);
	}

	function extendOptionsObject(target, options) {
		var type = exports.getType,
			name, special = { "include": true, "ignore": true, "copy": true },
			t, o, i = 1, l = arguments.length;
		if (type(target) !== "object") {
			target = {};
		}
		for (; i < l; i++) {
			options = arguments[i];
			if (type(options) !== "object") {
				options = {};
			}
			for (name in options) {
				t = target[name]; o = options[name];
				if (name !== "constructor" && special[name] && type(o) !== "array") {
					if (type(o) !== "string") {
						throw new Error("ko.mapping.defaultOptions()." + name + " should be an array or string.");
					}
					o = [o];
				}
				switch (type(o)) {
				case "object": // Recurse
					t = type(t) === "object" ? t : {};
					target[name] = extendOptionsObject(t, o);
					break;
				case "array":
					t = type(t) === "array" ? t : [];
					target[name] = extendOptionsArray(t, o);
					break;
				default:
					target[name] = o;
				}
			}
		}
		return target;
	}

	function mergeOptions() {
		var options = ko.utils.arrayPushAll([{}, defaultOptions], arguments); // Always use empty object as target to avoid changing default options
		options = extendOptionsObject.apply(this, options);
		return options;
	}

	// When using a 'create' callback, we proxy the dependent observable so that it doesn't immediately evaluate on creation.
	// The reason is that the dependent observables in the user-specified callback may contain references to properties that have not been mapped yet.
	function withProxyDependentObservable(dependentObservables, callback) {
		var localDO = ko.dependentObservable;
		ko.dependentObservable = function (read, owner, options) {
			options = options || {};

			if (read && typeof read == "object") { // mirrors condition in knockout implementation of DO's
				options = read;
			}

			var realDeferEvaluation = options.deferEvaluation;

			var isRemoved = false;

			// We wrap the original dependent observable so that we can remove it from the 'dependentObservables' list we need to evaluate after mapping has
			// completed if the user already evaluated the DO themselves in the meantime.
			var wrap = function (DO) {
				var wrapped = realKoDependentObservable({
					read: function () {
						if (!isRemoved) {
							ko.utils.arrayRemoveItem(dependentObservables, DO);
							isRemoved = true;
						}
						return DO.apply(DO, arguments);
					},
					write: function (val) {
						return DO(val);
					},
					deferEvaluation: true
				});
				if(DEBUG) wrapped._wrapper = true;
				return wrapped;
			};
			
			options.deferEvaluation = true; // will either set for just options, or both read/options.
			var realDependentObservable = new realKoDependentObservable(read, owner, options);

			if (!realDeferEvaluation) {
				realDependentObservable = wrap(realDependentObservable);
				dependentObservables.push(realDependentObservable);
			}

			return realDependentObservable;
		}
		ko.dependentObservable.fn = realKoDependentObservable.fn;
		ko.computed = ko.dependentObservable;
		var result = callback();
		ko.dependentObservable = localDO;
		ko.computed = ko.dependentObservable;
		return result;
	}

	function updateViewModel(mappedRootObject, rootObject, options, parentName, parent, parentPropertyName) {
		var isArray = ko.utils.unwrapObservable(rootObject) instanceof Array;

		// If nested object was already mapped previously, take the options from it
		if (parentName !== undefined && exports.isMapped(mappedRootObject)) {
			options = ko.utils.unwrapObservable(mappedRootObject)[mappingProperty];
			parentName = "";
			parentPropertyName = "";
		}

		parentName = parentName || "";
		parentPropertyName = parentPropertyName || "";

		var callbackParams = {
			data: rootObject,
			parent: parent
		};

		var getCallback = function (name) {
			var callback;
			if (parentName === "") {
				callback = options[name];
			} else if (callback = options[parentName]) {
				callback = callback[name]
			}
			return callback;
		};

		var hasCreateCallback = function () {
			return getCallback("create") instanceof Function;
		};

		var createCallback = function (data) {
			return withProxyDependentObservable(dependentObservables, function () {
				return getCallback("create")({
					data: data || callbackParams.data,
					parent: callbackParams.parent
				});
			});
		};

		var hasUpdateCallback = function () {
			return getCallback("update") instanceof Function;
		};

		var updateCallback = function (obj, data) {
			var params = {
				data: data || callbackParams.data,
				parent: callbackParams.parent,
				target: ko.utils.unwrapObservable(obj)
			};

			if (ko.isWriteableObservable(obj)) {
				params.observable = obj;
			}

			return getCallback("update")(params);
		}

		var alreadyMapped = visitedObjects.get(rootObject);
		if (alreadyMapped) {
			return alreadyMapped;
		}

		if (!isArray) {
			// For atomic types, do a direct update on the observable
			if (!canHaveProperties(rootObject)) {
				switch (exports.getType(rootObject)) {
				case "function":
					if (hasUpdateCallback()) {
						if (ko.isWriteableObservable(rootObject)) {
							rootObject(updateCallback(rootObject));
							mappedRootObject = rootObject;
						} else {
							mappedRootObject = updateCallback(rootObject);
						}
					} else {
						mappedRootObject = rootObject;
					}
					break;
				default:
					if (ko.isWriteableObservable(mappedRootObject)) {
						if (hasUpdateCallback()) {
							mappedRootObject(updateCallback(mappedRootObject));
						} else {
							mappedRootObject(ko.utils.unwrapObservable(rootObject));
						}
					} else {
						if (hasCreateCallback()) {
							mappedRootObject = createCallback();
						} else {
							mappedRootObject = ko.observable(ko.utils.unwrapObservable(rootObject));
						}

						if (hasUpdateCallback()) {
							mappedRootObject(updateCallback(mappedRootObject));
						}
					}
					break;
				}

			} else {
				mappedRootObject = ko.utils.unwrapObservable(mappedRootObject);
				if (!mappedRootObject) {
					if (hasCreateCallback()) {
						var result = createCallback();

						if (hasUpdateCallback()) {
							result = updateCallback(result);
						}

						return result;
					} else {
						if (hasUpdateCallback()) {
							return updateCallback(result);
						}

						mappedRootObject = {};
					}
				}

				if (hasUpdateCallback()) {
					mappedRootObject = updateCallback(mappedRootObject);
				}

				visitedObjects.save(rootObject, mappedRootObject);

				// For non-atomic types, visit all properties and update recursively
				visitPropertiesOrArrayEntries(rootObject, function (indexer) {
					var fullPropertyName = getPropertyName(parentPropertyName, rootObject, indexer);

					if (ko.utils.arrayIndexOf(options.ignore, fullPropertyName) != -1) {
						return;
					}

					if (ko.utils.arrayIndexOf(options.copy, fullPropertyName) != -1) {
						mappedRootObject[indexer] = rootObject[indexer];
						return;
					}

					// In case we are adding an already mapped property, fill it with the previously mapped property value to prevent recursion.
					// If this is a property that was generated by fromJS, we should use the options specified there
					var prevMappedProperty = visitedObjects.get(rootObject[indexer]);
					var value = prevMappedProperty || updateViewModel(mappedRootObject[indexer], rootObject[indexer], options, indexer, mappedRootObject, fullPropertyName);

					if (ko.isWriteableObservable(mappedRootObject[indexer])) {
						mappedRootObject[indexer](ko.utils.unwrapObservable(value));
					} else {
						mappedRootObject[indexer] = value;
					}

					options.mappedProperties[fullPropertyName] = true;
				});
			}
		} else {
			var changes = [];

			var hasKeyCallback = getCallback("key") instanceof Function;
			var keyCallback = hasKeyCallback ? getCallback("key") : function (x) {
				return x;
			};
			if (!ko.isObservable(mappedRootObject)) {
				// When creating the new observable array, also add a bunch of utility functions that take the 'key' of the array items into account.
				mappedRootObject = ko.observableArray([]);

				mappedRootObject.mappedRemove = function (valueOrPredicate) {
					var predicate = typeof valueOrPredicate == "function" ? valueOrPredicate : function (value) {
							return value === keyCallback(valueOrPredicate);
						};
					return mappedRootObject.remove(function (item) {
						return predicate(keyCallback(item));
					});
				}

				mappedRootObject.mappedRemoveAll = function (arrayOfValues) {
					var arrayOfKeys = filterArrayByKey(arrayOfValues, keyCallback);
					return mappedRootObject.remove(function (item) {
						return ko.utils.arrayIndexOf(arrayOfKeys, keyCallback(item)) != -1;
					});
				}

				mappedRootObject.mappedDestroy = function (valueOrPredicate) {
					var predicate = typeof valueOrPredicate == "function" ? valueOrPredicate : function (value) {
							return value === keyCallback(valueOrPredicate);
						};
					return mappedRootObject.destroy(function (item) {
						return predicate(keyCallback(item));
					});
				}

				mappedRootObject.mappedDestroyAll = function (arrayOfValues) {
					var arrayOfKeys = filterArrayByKey(arrayOfValues, keyCallback);
					return mappedRootObject.destroy(function (item) {
						return ko.utils.arrayIndexOf(arrayOfKeys, keyCallback(item)) != -1;
					});
				}

				mappedRootObject.mappedIndexOf = function (item) {
					var keys = filterArrayByKey(mappedRootObject(), keyCallback);
					var key = keyCallback(item);
					return ko.utils.arrayIndexOf(keys, key);
				}

				mappedRootObject.mappedCreate = function (value) {
					if (mappedRootObject.mappedIndexOf(value) !== -1) {
						throw new Error("There already is an object with the key that you specified.");
					}

					var item = hasCreateCallback() ? createCallback(value) : value;
					if (hasUpdateCallback()) {
						var newValue = updateCallback(item, value);
						if (ko.isWriteableObservable(item)) {
							item(newValue);
						} else {
							item = newValue;
						}
					}
					mappedRootObject.push(item);
					return item;
				}
			}

			var currentArrayKeys = filterArrayByKey(ko.utils.unwrapObservable(mappedRootObject), keyCallback).sort();
			var newArrayKeys = filterArrayByKey(rootObject, keyCallback);
			if (hasKeyCallback) newArrayKeys.sort();
			var editScript = ko.utils.compareArrays(currentArrayKeys, newArrayKeys);

			var ignoreIndexOf = {};

			var newContents = [];
			for (var i = 0, j = editScript.length; i < j; i++) {
				var key = editScript[i];
				var mappedItem;
				var fullPropertyName = getPropertyName(parentPropertyName, rootObject, i);
				switch (key.status) {
				case "added":
					var item = getItemByKey(ko.utils.unwrapObservable(rootObject), key.value, keyCallback);
					mappedItem = updateViewModel(undefined, item, options, parentName, mappedRootObject, fullPropertyName);
					if(!hasCreateCallback()) {
						mappedItem = ko.utils.unwrapObservable(mappedItem);
					}

					var index = ignorableIndexOf(ko.utils.unwrapObservable(rootObject), item, ignoreIndexOf);
					newContents[index] = mappedItem;
					ignoreIndexOf[index] = true;
					break;
				case "retained":
					var item = getItemByKey(ko.utils.unwrapObservable(rootObject), key.value, keyCallback);
					mappedItem = getItemByKey(mappedRootObject, key.value, keyCallback);
					updateViewModel(mappedItem, item, options, parentName, mappedRootObject, fullPropertyName);

					var index = ignorableIndexOf(ko.utils.unwrapObservable(rootObject), item, ignoreIndexOf);
					newContents[index] = mappedItem;
					ignoreIndexOf[index] = true;
					break;
				case "deleted":
					mappedItem = getItemByKey(mappedRootObject, key.value, keyCallback);
					break;
				}

				changes.push({
					event: key.status,
					item: mappedItem
				});
			}

			mappedRootObject(newContents);

			var arrayChangedCallback = getCallback("arrayChanged");
			if (arrayChangedCallback instanceof Function) {
				ko.utils.arrayForEach(changes, function (change) {
					arrayChangedCallback(change.event, change.item);
				});
			}
		}

		return mappedRootObject;
	}

	function ignorableIndexOf(array, item, ignoreIndices) {
		for (var i = 0, j = array.length; i < j; i++) {
			if (ignoreIndices[i] === true) continue;
			if (array[i] === item) return i;
		}
		return null;
	}

	function mapKey(item, callback) {
		var mappedItem;
		if (callback) mappedItem = callback(item);
		if (exports.getType(mappedItem) === "undefined") mappedItem = item;

		return ko.utils.unwrapObservable(mappedItem);
	}

	function getItemByKey(array, key, callback) {
		var filtered = ko.utils.arrayFilter(ko.utils.unwrapObservable(array), function (item) {
			return mapKey(item, callback) === key;
		});

		if (filtered.length == 0) throw new Error("When calling ko.update*, the key '" + key + "' was not found!");
		if ((filtered.length > 1) && (canHaveProperties(filtered[0]))) throw new Error("When calling ko.update*, the key '" + key + "' was not unique!");

		return filtered[0];
	}

	function filterArrayByKey(array, callback) {
		return ko.utils.arrayMap(ko.utils.unwrapObservable(array), function (item) {
			if (callback) {
				return mapKey(item, callback);
			} else {
				return item;
			}
		});
	}

	function visitPropertiesOrArrayEntries(rootObject, visitorCallback) {
		if (rootObject instanceof Array) {
			for (var i = 0; i < rootObject.length; i++)
			visitorCallback(i);
		} else {
			for (var propertyName in rootObject)
			visitorCallback(propertyName);
		}
	};

	function canHaveProperties(object) {
		var type = exports.getType(object);
		return (type === "object" || type === "array") && (object !== null) && (type !== "undefined");
	}

	// Based on the parentName, this creates a fully classified name of a property

	function getPropertyName(parentName, parent, indexer) {
		var propertyName = parentName || "";
		if (parent instanceof Array) {
			if (parentName) {
				propertyName += "[" + indexer + "]";
			}
		} else {
			if (parentName) {
				propertyName += ".";
			}
			propertyName += indexer;
		}
		return propertyName;
	}

	function visitModel(rootObject, callback, options, parentName, fullParentName) {
		// If nested object was already mapped previously, take the options from it
		if (parentName !== undefined && exports.isMapped(rootObject)) {
			//options = ko.utils.unwrapObservable(rootObject)[mappingProperty];
			options = mergeOptions(ko.utils.unwrapObservable(rootObject)[mappingProperty], options);
			parentName = "";
		}

		if (parentName === undefined) { // the first call
			visitedObjects = new objectLookup();
		}

		parentName = parentName || "";

		var mappedRootObject;
		var unwrappedRootObject = ko.utils.unwrapObservable(rootObject);
		if (!canHaveProperties(unwrappedRootObject)) {
			return callback(rootObject, fullParentName);
		} else {
			// Only do a callback, but ignore the results
			callback(rootObject, fullParentName);
			mappedRootObject = unwrappedRootObject instanceof Array ? [] : {};
		}

		visitedObjects.save(rootObject, mappedRootObject);

		var origFullParentName = fullParentName;
		visitPropertiesOrArrayEntries(unwrappedRootObject, function (indexer) {
			if (options.ignore && ko.utils.arrayIndexOf(options.ignore, indexer) != -1) return;

			var propertyValue = unwrappedRootObject[indexer];
			var fullPropertyName = getPropertyName(parentName, unwrappedRootObject, indexer);
			
			// If we don't want to explicitly copy the unmapped property...
			if (ko.utils.arrayIndexOf(options.copy, indexer) === -1) {
				// ...find out if it's a property we want to explicitly include
				if (ko.utils.arrayIndexOf(options.include, indexer) === -1) {
					// Options contains all the properties that were part of the original object.
					// If a property does not exist, and it is not because it is part of an array (e.g. "myProp[3]"), then it should not be unmapped.
					if (options.mappedProperties && !options.mappedProperties[fullPropertyName] && !(unwrappedRootObject instanceof Array)) {
						return;
					}
				}
			}

			fullParentName = getPropertyName(origFullParentName, unwrappedRootObject, indexer);
			
			var propertyType = exports.getType(ko.utils.unwrapObservable(propertyValue));
			switch (propertyType) {
			case "object":
			case "array":
			case "undefined":
				var previouslyMappedValue = visitedObjects.get(propertyValue);
				mappedRootObject[indexer] = (exports.getType(previouslyMappedValue) !== "undefined") ? previouslyMappedValue : visitModel(propertyValue, callback, options, fullPropertyName, fullParentName);
				break;
			default:
				mappedRootObject[indexer] = callback(propertyValue, fullParentName);
			}
		});

		return mappedRootObject;
	}

	function objectLookup() {
		var keys = [];
		var values = [];
		this.save = function (key, value) {
			var existingIndex = ko.utils.arrayIndexOf(keys, key);
			if (existingIndex >= 0) values[existingIndex] = value;
			else {
				keys.push(key);
				values.push(value);
			}
		};
		this.get = function (key) {
			var existingIndex = ko.utils.arrayIndexOf(keys, key);
			return (existingIndex >= 0) ? values[existingIndex] : undefined;
		};
	};
}));