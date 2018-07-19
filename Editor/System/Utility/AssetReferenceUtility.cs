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
	public static class AssetReferenceUtility {

        public static AssetReference FindFirstIncomingAssetReference(List<AssetReference> assets) {

            if(assets != null && assets.Any()) {
                return assets.First();
            }

            return null;
        }

        public static AssetReference FindFirstIncomingAssetReference(AssetReferenceStreamManager mgr, Model.ConnectionPointData inputPoint) {
            var assetGroupEnum = mgr.EnumurateIncomingAssetGroups(inputPoint);
            if(assetGroupEnum == null) {
                return null;
            }

            if(assetGroupEnum.Any()) {
                var ag = assetGroupEnum.First();
                if(ag.Values.Any()) {
                    var assets = ag.Values.First();
                    if(assets.Count > 0) {
                        return assets[0];
                    }
                }
            }

            return null;
        }

        public static AssetReference FindFirstIncomingAssetReference(IEnumerable<PerformGraph.AssetGroups> incoming) {

            if( incoming == null ) {
                return null;
            }

            foreach(var ag in incoming) {
                foreach(var v in ag.assetGroups.Values) {
                    if(v.Count > 0) {
                        return v[0];
                    }
                }
            }

            return null;
        }
	}
}
