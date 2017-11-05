using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.Animations;

using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_2017_1_OR_NEWER
using UnityEngine.U2D;
using UnityEngine.Playables;
using UnityEngine.Timeline;
#endif

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	public static class TypeUtility {

		private static readonly List<Type> IgnoreTypes = new List<Type> {
			typeof(MonoScript),
			typeof(AssetBundleReference),
            typeof(Model.ConfigGraph),
            typeof(Model.ConnectionData),
            typeof(Model.ConnectionPointData),
            typeof(Model.NodeData),
            typeof(AssetReferenceDatabase),
            typeof(AssetBundleBuildMap),
            typeof(AssetProcessEventRecord)
		};

        public static bool IsLoadingAsset (string assetPath) {
            if (assetPath.Contains (AssetGraphBasePath.BasePath)) {
                return false;
            }
            Type t = GetMainAssetTypeAtPath (assetPath);
            if (t == null) {
                return false;
            }
            if (IgnoreTypes.Contains (t)) {
                return false;
            }
            return true;
        }

        public static Type GetAssetImporterTypeAtPath (string assetPath) {
            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer != null) {
                var importerType = importer.GetType();

                if (importerType != null &&
                    importerType  != typeof(UnityEditor.AssetImporter)) 
                {
                    return importerType;
                }
            }
            return null;
        }

		/**
		 * Get type of asset from give path.
		 */
		public static Type GetMainAssetTypeAtPath (string assetPath) {
			if (assetPath.EndsWith(Model.Settings.UNITY_METAFILE_EXTENSION)) {
				return typeof(string);
			}

			Type t = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if(t == typeof(MonoBehaviour)) {
                UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                t = asset.GetType();
            }

			return t;
		}
            
		public static MonoScript LoadMonoScript(string className) {
			if(className == null) {
				return null;
			}

			var t = Type.GetType(className);
			if(t == null) {
				return null;
			}

			string[] guids = AssetDatabase.FindAssets ("t:MonoScript " + className);

			MonoScript s = null;

			if(guids.Length > 0 ) {
				var path = AssetDatabase.GUIDToAssetPath(guids[0]);
				s = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
			}

			return s;
		}
	}
}
