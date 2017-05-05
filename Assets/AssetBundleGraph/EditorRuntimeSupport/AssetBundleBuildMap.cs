using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

namespace UnityEngine.AssetBundles.GraphTool {
	public class AssetBundleBuildMap : ScriptableObject {

		[SerializeField] private List<AssetBundleEntry> m_assetBundles;
		[SerializeField] private int m_version;

		private const int VERSION = 1;

		private static AssetBundleBuildMap s_map;

		class Config {
			public const string ASSETNBUNDLEGRAPH_DATA_PATH    = "Assets/AssetBundleGraph/SettingFiles";
			public const string ASSETBUNDLEGRAPH_BUILDMAP_NAME = ASSETNBUNDLEGRAPH_DATA_PATH + "/AssetBundleBuildMap.asset";
		}

		public static AssetBundleBuildMap GetBuildMap() {
			if(s_map == null) {
				if(!Load()) {
					// Create vanilla db
					s_map = ScriptableObject.CreateInstance<AssetBundleBuildMap>();
					s_map.m_assetBundles = new List<AssetBundleEntry>();
					s_map.m_version = VERSION;

					var DBDir = Config.ASSETNBUNDLEGRAPH_DATA_PATH;

					if (!Directory.Exists(DBDir)) {
						Directory.CreateDirectory(DBDir);
					}

					AssetDatabase.CreateAsset(s_map, DBPath);
				}
			}

			return s_map;
		}

		private static string DBPath {
			get {
				return Config.ASSETBUNDLEGRAPH_BUILDMAP_NAME;
			}
		}

		private static bool Load() {

			bool loaded = false;

			try {
				var dbPath = DBPath;

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

			return loaded;
		}

		public static void SetMapDirty() {
			EditorUtility.SetDirty(s_map);
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

			[SerializeField] internal string m_assetBundleName;
			[SerializeField] internal string m_assetBundleVariantName;
			[SerializeField] internal string m_fullName;
			[SerializeField] private List<string> m_assets;
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
				m_assets = new List<string>();
			}

			public void Clear() {
				m_assets.Clear ();
				AssetBundleBuildMap.SetMapDirty ();
			}


			public void AddAssets(string id, IEnumerable<string> assets) {
				foreach (var a in assets) {
					m_assets.Add (a.ToLower ());
				}
				AssetBundleBuildMap.SetMapDirty ();
			}

			public IEnumerable<string> GetAssetFromAssetName(string assetName) {
				assetName = assetName.ToLower ();
				return m_assets.Where (a => Path.GetFileNameWithoutExtension(a) == assetName);
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
	}
}

