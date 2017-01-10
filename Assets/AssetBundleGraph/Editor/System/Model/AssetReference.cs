using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Security.Cryptography;


namespace AssetBundleGraph {
	[System.Serializable]
	public class AssetReference {

		[SerializeField] private Guid m_guid;
		[SerializeField] private string m_assetDatabaseId;
		[SerializeField] private string m_importFrom;
		[SerializeField] private string m_exportTo;
		[SerializeField] private string m_variantName;
		[SerializeField] private string m_assetTypeString;

		private UnityEngine.Object[] m_data;
		private Type m_assetType;
		private Type m_filterType;

		public string id {
			get {
				return m_guid.ToString();
			}
		}

		public string assetDatabaseId {
			get {
				return m_assetDatabaseId;
			}
		}

		public string importFrom {
			get {
				return m_importFrom;
			}
			set {
				m_importFrom = value;
				AssetReferenceDatabase.SetDBDirty();
			}
		}

		public string exportTo {
			get {
				return m_exportTo;
			}
			set {
				m_exportTo = value;
				AssetReferenceDatabase.SetDBDirty();
			}
		}

		public string variantName {
			get {
				return m_variantName;
			}
			set {
				m_variantName = value;
				AssetReferenceDatabase.SetDBDirty();
			}
		}

		public Type assetType {
			get {
				if(m_assetType == null) {
					m_assetType = Type.GetType(m_assetTypeString);
					if(m_assetType == null) {
						m_assetType = TypeUtility.GetTypeOfAsset(importFrom);
						m_assetTypeString = m_assetType.AssemblyQualifiedName;
					}
				}
				return m_assetType;
			}
		}

		public Type filterType {
			get {
				if(m_filterType == null) {
					m_filterType = TypeUtility.FindTypeOfAsset(m_importFrom);
				}
				return m_filterType;
			}
		}

		public string fileNameAndExtension {
			get {
				if(m_importFrom != null) {
					return Path.GetFileName(m_importFrom);
				}
				if(m_exportTo != null) {
					return Path.GetFileName(m_exportTo);
				}
				return null;
			}
		}

		public string path {
			get {
				if(m_importFrom != null) {
					return m_importFrom;
				}
				if(m_exportTo != null) {
					return m_exportTo;
				}
				return null;
			}
		}

		public string absolutePath {
			get {
				return Application.dataPath + m_importFrom;
			}
		}

		public UnityEngine.Object[] allData {
			get {
				if(m_data == null || m_data.Length == 0) {
					m_data = AssetDatabase.LoadAllAssetsAtPath(importFrom);
				}
				return m_data;
			}
		}

		public void SetDirty() {
			if(m_data != null) {
				foreach(var o in m_data) {
					EditorUtility.SetDirty(o);
				}
			}
		}

		public void ReleaseData() {
			if(m_data != null) {
				foreach(var o in m_data) {
					if(o is UnityEngine.GameObject || o is UnityEngine.Component) {
						// do nothing.
						// NOTE: DestroyImmediate() will destroy persistant GameObject in prefab. Do not call it.
					} else {
						LogUtility.Logger.LogFormat(LogType.Log, "Unloading {0} ({1})", importFrom, o.GetType().ToString());
						Resources.UnloadAsset(o);
					}
				}
				m_data = null;
			}
		}

		public void TouchImportAsset() {
			System.IO.File.SetLastWriteTime(importFrom, DateTime.UtcNow);
		}

		public static AssetReference CreateReference (string importFrom) {
			return new AssetReference(
				guid: Guid.NewGuid(),
				assetDatabaseId:AssetDatabase.AssetPathToGUID(importFrom),
				importFrom:importFrom,
				assetType:TypeUtility.GetTypeOfAsset(importFrom)
			);
		}

		public static AssetReference CreatePrefabReference (string importFrom) {
			return new AssetReference(
				guid: Guid.NewGuid(),
				assetDatabaseId:AssetDatabase.AssetPathToGUID(importFrom),
				importFrom:importFrom,
				assetType:typeof(GameObject)
			);
		}

		public static AssetReference CreateAssetBundleReference (string path) {
			return new AssetReference(
				guid: Guid.NewGuid(),
				assetDatabaseId:AssetDatabase.AssetPathToGUID(path),
				importFrom:path,
				assetType:typeof(AssetBundleReference)
			);
		}

		private AssetReference (
			Guid guid,
			string assetDatabaseId = null,
			string importFrom = null,
			string exportTo = null,
			Type assetType = null,
			string variantName = null
		) {
			if(assetType == null) {
				throw new AssetReferenceException(importFrom, "Invalid type of asset created:" + importFrom);
			}

			this.m_guid = guid;
			this.m_importFrom = importFrom;
			this.m_exportTo = exportTo;
			this.m_assetDatabaseId = assetDatabaseId;
			this.m_assetType = assetType;
			this.m_assetTypeString = assetType.AssemblyQualifiedName;
			this.m_variantName = variantName;
		}
	}
}