using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

using Model = UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{
    [Serializable] 
    [CustomAssetImporterConfigurator(typeof(TrueTypeFontImporter), "Font", "setting.ttf")]
    public class TrueTypeFontImportSettingsConfigurator : IAssetImporterConfigurator
    {
        public void Initialize (ConfigurationOption option)
        {
        }

        public bool IsModified (AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group)
        {
            var r = referenceImporter as TrueTypeFontImporter;
            var t = importer as TrueTypeFontImporter;
            if (r == null || t == null) {
                throw new AssetGraphException (string.Format ("Invalid AssetImporter assigned for {0}", importer.assetPath));
            }
            return !IsEqual (t, r);
        }

        public void Configure (AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group)
        {
            var r = referenceImporter as TrueTypeFontImporter;
            var t = importer as TrueTypeFontImporter;
            if (r == null || t == null) {
                throw new AssetGraphException (string.Format ("Invalid AssetImporter assigned for {0}", importer.assetPath));
            }
            OverwriteImportSettings (t, r);
        }

        public void OnInspectorGUI (AssetImporter referenceImporter, BuildTargetGroup target, Action onValueChanged)
        {
        }

        private void OverwriteImportSettings (TrueTypeFontImporter target, TrueTypeFontImporter reference)
        {
        }

        private bool IsEqual (TrueTypeFontImporter target, TrueTypeFontImporter reference)
        {
            return true;
        }
    }
}
