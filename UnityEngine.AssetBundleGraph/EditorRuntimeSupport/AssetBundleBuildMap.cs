using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.AssetBundles.GraphTool {
	public class AssetBundleBuildMap : ScriptableObject {

		[SerializeField] private List<AssetBundleEntry> m_assetBundles;
        #if UNITY_EDITOR
		[SerializeField] private int m_version;
		private const int VERSION = 1;
        #endif

		private static AssetBundleBuildMap s_map;

        #if UNITY_EDITOR
        class Config {
            private static string s_basePath;

            public static string BasePath {
                get {
                    //if (string.IsNullOrEmpty (s_basePath)) {
                    var obj = ScriptableObject.CreateInstance<AssetBundleBuildMap> ();
                    MonoScript s = MonoScript.FromScriptableObject (obj);
                    var configGuiPath = AssetDatabase.GetAssetPath( s );
                    UnityEngine.Object.DestroyImmediate (obj);

                    var fileInfo = new FileInfo(configGuiPath);
                    var baseDir = fileInfo.Directory.Parent;

                    string baseDirPath = baseDir.ToString ().Replace( '\\', '/');

                    int index = baseDirPath.LastIndexOf (ASSETS_PATH);
                    Assertions.Assert.IsTrue ( index >= 0 );

                    baseDirPath = baseDirPath.Substring (index);

					s_basePath = baseDirPath;
                    //}
                    return s_basePath;
                }
            }
            public const string ASSETS_PATH = "Assets/";
            public static string SettingFilePath        { get { return BasePath + "/SettingFiles/"; } }
            public static string BuildMapPath           { get { return SettingFilePath + "AssetBundleBuildMap.asset"; } }
        }
        #endif

		public static AssetBundleBuildMap GetBuildMap() {
			if(s_map == null) {
				if(!Load()) {
					// Create vanilla db
					s_map = ScriptableObject.CreateInstance<AssetBundleBuildMap>();
					s_map.m_assetBundles = new List<AssetBundleEntry>();
                    #if UNITY_EDITOR
					s_map.m_version = VERSION;

                    var DBDir = Config.SettingFilePath;

					if (!Directory.Exists(DBDir)) {
						Directory.CreateDirectory(DBDir);
					}

                    AssetDatabase.CreateAsset(s_map, Config.BuildMapPath);
					#endif
				}
			}

			return s_map;
		}

		private static bool Load() {
			bool loaded = false;

			#if UNITY_EDITOR
			try {
                var dbPath = Config.BuildMapPath;

				if(File.Exists(dbPath)) 
				{
					AssetBundleBuildMap m = AssetDatabase.LoadAssetAtPath<AssetBundleBuildMap>(dbPath);

					if(m != null && m.m_version == VERSION) {
						s_map = m;
						loaded = true;
					}
				}
			} catch(Exception e) {
				Debug.LogException (e);
			}
			#endif

			return loaded;
		}

		public static void SetMapDirty() {
			#if UNITY_EDITOR
			EditorUtility.SetDirty(s_map);
			#endif
		}

		internal static string MakeFullName(string assetBundleName, string variantName) {
			if (string.IsNullOrEmpty (assetBundleName)) {
				return "";
			}
			if (string.IsNullOrEmpty (variantName)) {
				return assetBundleName.ToLower();
			}
			return string.Format("{0}.{1}", assetBundleName.ToLower(), variantName.ToLower());
		}

		internal static string[] FullNameToNameAndVariant(string assetBundleFullName) {
			assetBundleFullName = assetBundleFullName.ToLower ();
			var vIndex = assetBundleFullName.LastIndexOf ('.');
			if (vIndex > 0 && vIndex+1 < assetBundleFullName.Length) {
				var bundleName = assetBundleFullName.Substring (0, vIndex);
				var bundleVariant = assetBundleFullName.Substring (vIndex+1);
				return new string[] { bundleName, bundleVariant };
			}
			return new string[] { assetBundleFullName, "" };
		}

		[Serializable]
		public class AssetBundleEntry {

			[Serializable]
			internal struct AssetPathString {
				[SerializeField] public string original;
				[SerializeField] public string lower;

				internal AssetPathString(string s) {
					original = s;
					if(!string.IsNullOrEmpty(s)) {
						lower = s.ToLower();
					} else {
						lower = s;
					}
				}
			}

			[SerializeField] internal string m_assetBundleName;
			[SerializeField] internal string m_assetBundleVariantName;
			[SerializeField] internal string m_fullName;
			[SerializeField] internal List<AssetPathString> m_assets;
			[SerializeField] public string m_registererId;

			public string Name {
				get { return m_assetBundleName; }
			}

			public string Variant {
				get { return m_assetBundleVariantName; }
			}

			public string FullName {
				get {
					return m_fullName;
				}
			}

			public AssetBundleEntry(string registererId, string assetBundleName, string variantName) {
				m_registererId = registererId;
				m_assetBundleName = assetBundleName.ToLower();
				m_assetBundleVariantName = variantName.ToLower();
				m_fullName = AssetBundleBuildMap.MakeFullName(assetBundleName, variantName);
				m_assets = new List<AssetPathString>();
			}

			public void Clear() {
				m_assets.Clear ();
				AssetBundleBuildMap.SetMapDirty ();
			}


			public void AddAssets(string id, IEnumerable<string> assets) {
				foreach (var a in assets) {
					m_assets.Add (new AssetPathString(a));
				}
				AssetBundleBuildMap.SetMapDirty ();
			}

			public IEnumerable<string> GetAssetFromAssetName(string assetName) {
				assetName = assetName.ToLower ();
				return m_assets.Where (a => Path.GetFileNameWithoutExtension(a.lower) == assetName).Select(s => s.original);
			}
		}

		public AssetBundleEntry GetAssetBundle(string registererId, string assetBundleFullName) {
			var entry = m_assetBundles.Find (v => v.m_fullName == assetBundleFullName);
			if (entry == null) {
				string[] names = AssetBundleBuildMap.FullNameToNameAndVariant (assetBundleFullName);
				entry = new AssetBundleEntry (registererId, names[0], names[1]);
				m_assetBundles.Add (entry);
				SetMapDirty ();
			}
			return entry;
		}

		public void Clear() {
			m_assetBundles.Clear ();
			SetMapDirty ();
		}

		public void ClearFromId(string id) {
			m_assetBundles.RemoveAll (e => e.m_registererId == id);
		}

		public AssetBundleEntry GetAssetBundleWithNameAndVariant(string registererId, string assetBundleName, string variantName) {
			return GetAssetBundle(registererId, AssetBundleBuildMap.MakeFullName(assetBundleName, variantName));
		}

		public string[] GetAssetPathsFromAssetBundleAndAssetName(string assetbundleName, string assetName) {
			assetName = assetName.ToLower ();
			return m_assetBundles.Where (ab => ab.m_fullName == assetbundleName)
				.SelectMany (ab => ab.GetAssetFromAssetName (assetName))
				.ToArray();
		}

		public string[] GetAssetPathsFromAssetBundle (string assetBundleName) {
			assetBundleName = assetBundleName.ToLower ();
			return m_assetBundles.Where(e => e.m_fullName == assetBundleName).SelectMany(e => e.m_assets).Select(s => s.original).ToArray();
		}

		public string GetAssetBundleName(string assetPath) {
			var entry = m_assetBundles.Find(e => e.m_assets.Contains(new AssetBundleEntry.AssetPathString(assetPath)));
			if (entry != null) {
				return entry.m_fullName;
			}
			return string.Empty;
		}

		public string GetImplicitAssetBundleName(string assetPath) {
			return GetAssetBundleName (assetPath);
		}

		public string[] GetAllAssetBundleNames() {
			return m_assetBundles.Select (e => e.m_fullName).ToArray ();
		}
	}
}

