using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.AssetGraph {
    #if UNITY_EDITOR
    public class AssetGraphBasePath : ScriptableObject {
        private static string s_basePath;

        public static string BasePath {
            get {
                if (string.IsNullOrEmpty (s_basePath)) {
                    var obj = ScriptableObject.CreateInstance<AssetGraphBasePath> ();
                    MonoScript s = MonoScript.FromScriptableObject (obj);
                    var configGuiPath = AssetDatabase.GetAssetPath( s );
                    UnityEngine.Object.DestroyImmediate (obj);

                    var fileInfo = new FileInfo(configGuiPath);
                    var baseDir = fileInfo.Directory.Parent;

                    Assertions.Assert.AreEqual (ToolDirName, baseDir.Name);

                    string baseDirPath = baseDir.ToString ().Replace( '\\', '/');

                    int index = baseDirPath.LastIndexOf (ASSETS_PATH);
                    Assertions.Assert.IsTrue ( index >= 0 );

                    baseDirPath = baseDirPath.Substring (index);

                    s_basePath = baseDirPath;
                }
                return s_basePath;
            }
        }

        public static void ResetBasePath() {
            s_basePath = string.Empty;
        }

        /// <summary>
        /// Name of the base directory containing the asset graph tool files.
        /// Customize this to match your project's setup if you need to change.
        /// </summary>
        /// <value>The name of the base directory.</value>
        public static string ToolDirName            { get { return "UnityEngine.AssetGraph"; } }

        public const string ASSETS_PATH = "Assets/";
        public static string CachePath                  { get { return System.IO.Path.Combine(BasePath, "Cache"); } }
        public static string SettingFilePath            { get { return System.IO.Path.Combine(BasePath, "SettingFiles"); } }
        public static string TemporalSettingFilePath    { get { return System.IO.Path.Combine(CachePath, "TemporalSettingFiles"); } }
    }
    #endif
}

