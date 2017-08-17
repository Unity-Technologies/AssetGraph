using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

using System;
using System.IO;
using System.Security.Cryptography;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
    /// <summary>
    /// Asset reference.
    /// </summary>
	[System.Serializable]
	public class AssetReference {

		[SerializeField] private Guid m_guid;
		[SerializeField] private string m_assetDatabaseId;
		[SerializeField] private string m_importFrom;
		[SerializeField] private string m_exportTo;
		[SerializeField] private string m_variantName;
		[SerializeField] private string m_assetTypeString;

		private UnityEngine.Object[] m_data;
		private SceneManagement.Scene m_scene;
		private Type m_assetType;
		private Type m_filterType;

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
		public string id {
			get {
				return m_guid.ToString();
			}
		}

        /// <summary>
        /// Gets the asset database identifier.
        /// </summary>
        /// <value>The asset database identifier.</value>
		public string assetDatabaseId {
			get {
				return m_assetDatabaseId;
			}
		}

        /// <summary>
        /// Gets or sets the import from.
        /// </summary>
        /// <value>The import from.</value>
		public string importFrom {
			get {
				return m_importFrom;
			}
			set {
				m_importFrom = value;
				AssetReferenceDatabase.SetDBDirty();
			}
		}

        /// <summary>
        /// Gets or sets the export to.
        /// </summary>
        /// <value>The export to.</value>
		public string exportTo {
			get {
				return m_exportTo;
			}
			set {
				m_exportTo = value;
				AssetReferenceDatabase.SetDBDirty();
			}
		}

        /// <summary>
        /// Gets or sets the name of the variant.
        /// </summary>
        /// <value>The name of the variant.</value>
		public string variantName {
			get {
				return m_variantName;
			}
			set {
				m_variantName = value;
				AssetReferenceDatabase.SetDBDirty();
			}
		}

        /// <summary>
        /// Gets the type of the asset.
        /// </summary>
        /// <value>The type of the asset.</value>
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

        /// <summary>
        /// Gets the type of the filter.
        /// </summary>
        /// <value>The type of the filter.</value>
		public Type filterType {
			get {
				if(m_filterType == null) {
                    m_filterType = TypeUtility.FindAssetFilterType(m_importFrom);
				}
				return m_filterType;
			}
		}

        /// <summary>
        /// Gets the file name and extension.
        /// </summary>
        /// <value>The file name and extension.</value>
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

        /// <summary>
        /// Gets extension.
        /// </summary>
        /// <value>The extension of the file name.</value>
        public string extension {
            get {
                if(m_importFrom != null) {
                    return Path.GetExtension(m_importFrom);
                }
                if(m_exportTo != null) {
                    return Path.GetExtension(m_exportTo);
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <value>The name of the file.</value>
		public string fileName {
			get {
				if(m_importFrom != null) {
					return Path.GetFileNameWithoutExtension(m_importFrom);
				}
				if(m_exportTo != null) {
					return Path.GetFileNameWithoutExtension(m_exportTo);
				}
				return null;
			}
		}

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>The path.</value>
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

        /// <summary>
        /// Gets the absolute path.
        /// </summary>
        /// <value>The absolute path.</value>
		public string absolutePath {
			get {
                return m_importFrom.Replace("Assets", Application.dataPath);
			}
		}

        /// <summary>
        /// File size (byte)
        /// </summary>
        /// <value>File size (byte)</value>
        public long runtimeMemorySize {
            get {
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(absolutePath);
                if (fileInfo.Exists) {
                    return fileInfo.Length;
                }
                return 0L;
            }
        }

        /// <summary>
        /// Gets all data.
        /// </summary>
        /// <value>All data.</value>
		public UnityEngine.Object[] allData {
			get {
				if(m_data == null || m_data.Length == 0) {
					if (isSceneAsset) {
						if(!m_scene.isLoaded) {
							m_scene = EditorSceneManager.OpenScene (importFrom);
						}
						m_data = m_scene.GetRootGameObjects ();
					} else {
						m_data = AssetDatabase.LoadAllAssetsAtPath (importFrom);
					}
				}
				return m_data;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:UnityEngine.AssetBundles.GraphTool.AssetReference"/> is referencing a scene asset.
		/// </summary>
		/// <value><c>true</c> if is scene asset; otherwise, <c>false</c>.</value>
		public bool isSceneAsset {
			get {
				return filterType == typeof (SceneManagement.Scene);
			}
		}

		/// <summary>
		/// Gets the scene.
		/// </summary>
		/// <value>The loaded Scene object.</value>
		public SceneManagement.Scene scene {
			get {
				return m_scene;
			}
		}

        public long GetFileSize() {
            if (string.IsNullOrEmpty (m_importFrom)) {
                return 0L;
            }
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(absolutePath);
            if (fileInfo.Exists) {
                return fileInfo.Length;
            }
            return 0L;
        }

        public long GetRuntimeMemorySize() {
            var releaseAfterLoad = (m_data == null || m_data.Length == 0);
            var objects = allData;
            long size = 0L;
            foreach (var o in objects) {
                #if UNITY_5_6_OR_NEWER
                size += Profiler.GetRuntimeMemorySizeLong (o);
                #else
                size += Profiler.GetRuntimeMemorySize(o);
                #endif
            }

            if (releaseAfterLoad) {
                ReleaseData ();
            }
            return size;
        }

        /// <summary>
        /// Sets the dirty.
        /// </summary>
		public void SetDirty() {
			if(isSceneAsset) {
				if(m_scene.isLoaded) {
					EditorSceneManager.MarkSceneDirty (m_scene);
				}
			}
			else if(m_data != null) {
				foreach(var o in m_data) {
					if(o == null) {
						continue;
					}
					EditorUtility.SetDirty(o);
				}
			}
		}

        /// <summary>
        /// Releases the data.
        /// </summary>
		public void ReleaseData() {
			if (isSceneAsset) {
				if(m_scene.isLoaded) {
					// unloading last scene is not supported. omit closing if this is the last scene.
					if(EditorSceneManager.sceneCount > 1) {
						EditorSceneManager.CloseScene (m_scene, true);
					}
				}
				m_data = null;
			}
			else if(m_data != null) {
				foreach(var o in m_data) {
					if (o == null) {
						continue;
					}
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

        /// <summary>
        /// Touchs the import asset.
        /// </summary>
		public void TouchImportAsset() {
			System.IO.File.SetLastWriteTime(importFrom, DateTime.UtcNow);
		}

        /// <summary>
        /// Creates the reference.
        /// </summary>
        /// <returns>The reference.</returns>
        /// <param name="importFrom">Import from.</param>
		public static AssetReference CreateReference (string importFrom) {
			return new AssetReference(
				guid: Guid.NewGuid(),
				assetDatabaseId:AssetDatabase.AssetPathToGUID(importFrom),
				importFrom:importFrom,
				assetType:TypeUtility.GetTypeOfAsset(importFrom)
			);
		}

        /// <summary>
        /// Creates the reference.
        /// </summary>
        /// <returns>The reference.</returns>
        /// <param name="importFrom">Import from.</param>
        /// <param name="assetType">Asset type.</param>
        public static AssetReference CreateReference (string importFrom, Type assetType) {
            return new AssetReference(
                guid: Guid.NewGuid(),
                assetDatabaseId:AssetDatabase.AssetPathToGUID(importFrom),
                importFrom:importFrom,
                assetType:assetType
            );
        }

        /// <summary>
        /// Creates the prefab reference.
        /// </summary>
        /// <returns>The prefab reference.</returns>
        /// <param name="importFrom">Import from.</param>
		public static AssetReference CreatePrefabReference (string importFrom) {
			return new AssetReference(
				guid: Guid.NewGuid(),
				assetDatabaseId:AssetDatabase.AssetPathToGUID(importFrom),
				importFrom:importFrom,
				assetType:typeof(GameObject)
			);
		}

        /// <summary>
        /// Creates the asset bundle reference.
        /// </summary>
        /// <returns>The asset bundle reference.</returns>
        /// <param name="path">Path.</param>
		public static AssetReference CreateAssetBundleReference (string path) {
			return new AssetReference(
				guid: Guid.NewGuid(),
				assetDatabaseId:AssetDatabase.AssetPathToGUID(path),
				importFrom:path,
				assetType:typeof(AssetBundleReference)
			);
		}

        /// <summary>
        /// Creates the asset bundle manifest reference.
        /// </summary>
        /// <returns>The asset bundle manifest reference.</returns>
        /// <param name="path">Path.</param>
        public static AssetReference CreateAssetBundleManifestReference (string path) {
            return new AssetReference(
                guid: Guid.NewGuid(),
                assetDatabaseId:AssetDatabase.AssetPathToGUID(path),
                importFrom:path,
                assetType:typeof(AssetBundleManifestReference)
            );
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityEngine.AssetBundles.GraphTool.AssetReference"/> class.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="assetDatabaseId">Asset database identifier.</param>
        /// <param name="importFrom">Import from.</param>
        /// <param name="exportTo">Export to.</param>
        /// <param name="assetType">Asset type.</param>
        /// <param name="variantName">Variant name.</param>
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