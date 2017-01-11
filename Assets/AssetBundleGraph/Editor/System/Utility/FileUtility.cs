using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;


namespace AssetBundleGraph {
	public class FileUtility {
		public static void RemakeDirectory (string localFolderPath) {
			if (Directory.Exists(localFolderPath)) Directory.Delete(localFolderPath, true);
			Directory.CreateDirectory(localFolderPath);
		}

		public static void CopyFile (string sourceFilePath, string targetFilePath) {
			var parentDirectoryPath = Path.GetDirectoryName(targetFilePath);
			Directory.CreateDirectory(parentDirectoryPath);
			File.Copy(sourceFilePath, targetFilePath, true);
		}

		public static void CopyTemplateFile (string sourceFilePath, string targetFilePath, string srcName, string dstName) {
			var parentDirectoryPath = Path.GetDirectoryName(targetFilePath);
			Directory.CreateDirectory(parentDirectoryPath);

			StreamReader r = File.OpenText(sourceFilePath);

			string contents = r.ReadToEnd();
			contents = contents.Replace(srcName, dstName);

			File.WriteAllText(targetFilePath, contents);
		}

		public static void DeleteFileThenDeleteFolderIfEmpty (string localTargetFilePath) {			
			File.Delete(localTargetFilePath);
			File.Delete(localTargetFilePath + AssetBundleGraphSettings.UNITY_METAFILE_EXTENSION);
			var directoryPath = Directory.GetParent(localTargetFilePath).FullName;
			var restFiles = GetFilePathsInFolder(directoryPath);
			if (!restFiles.Any()) {
				Directory.Delete(directoryPath, true);
				File.Delete(directoryPath + AssetBundleGraphSettings.UNITY_METAFILE_EXTENSION);
			}
		}

		// Get all files under given path, including files in child folders
		public static List<string> GetAllFilePathsInFolder (string localFolderPath, bool includeHidden=false, bool includeMeta=!AssetBundleGraphSettings.IGNORE_META) 
		{
			var filePaths = new List<string>();
			
			if (string.IsNullOrEmpty(localFolderPath)) {
				return filePaths;
			}
			if (!Directory.Exists(localFolderPath)) {
				return filePaths;
			}

			GetFilePathsRecursively(localFolderPath, filePaths, includeHidden, includeMeta);
			
			return filePaths;
		}

		// Get files under given path
		public static List<string> GetFilePathsInFolder (string folderPath, bool includeHidden=false, bool includeMeta=!AssetBundleGraphSettings.IGNORE_META) {
			var filePaths = Directory.GetFiles(folderPath).Select(p=>p);

			if(!includeHidden) {
				filePaths = filePaths.Where(path => !(Path.GetFileName(path).StartsWith(AssetBundleGraphSettings.DOTSTART_HIDDEN_FILE_HEADSTRING)));
			}
			if (!includeMeta) {
				filePaths = filePaths.Where(path => !FileUtility.IsMetaFile(path));
			}

			// Directory.GetFiles() returns platform dependent delimiter, so make sure replace with "/"
			if( Path.DirectorySeparatorChar != AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR ) {
				filePaths =	filePaths.Select(filePath => filePath.Replace(Path.DirectorySeparatorChar.ToString(), AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString()));
			}

			return filePaths.ToList();
		}

		private static void GetFilePathsRecursively (string localFolderPath, List<string> filePaths, bool includeHidden=false, bool includeMeta=!AssetBundleGraphSettings.IGNORE_META) {
			var folders = Directory.GetDirectories(localFolderPath);

			foreach (var folder in folders) {
				GetFilePathsRecursively(folder, filePaths, includeHidden, includeMeta);
			}

			var files = GetFilePathsInFolder(localFolderPath, includeHidden, includeMeta);
			filePaths.AddRange(files);
		}

		/**
			create combination of path.

			delimiter is always '/'.
		*/
		public static string PathCombine (params string[] paths) {
			if (paths.Length < 2) {
				throw new ArgumentException("Argument must contain at least 2 strings to combine.");
			}

			var combinedPath = _PathCombine(paths[0], paths[1]);
			var restPaths = new string[paths.Length-2];

			Array.Copy(paths, 2, restPaths, 0, restPaths.Length);
			foreach (var path in restPaths) combinedPath = _PathCombine(combinedPath, path);

			return combinedPath;
		}

		private static string _PathCombine (string head, string tail) {
			if (!head.EndsWith(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString())) {
				head = head + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
			}
			
			if (string.IsNullOrEmpty(tail)) {
				return head;
			}

			if (tail.StartsWith(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString())) {
				tail = tail.Substring(1);
			}

			return Path.Combine(head, tail);
		}

		public static string GetPathWithProjectPath (string pathUnderProjectFolder) {
			var assetPath = Application.dataPath;
			var projectPath = Directory.GetParent(assetPath).ToString();
			return FileUtility.PathCombine(projectPath, pathUnderProjectFolder);
		}

		public static string GetPathWithAssetsPath (string pathUnderAssetsFolder) {
			var assetPath = Application.dataPath;
			return FileUtility.PathCombine(assetPath, pathUnderAssetsFolder);
		}

		public static string ProjectPathWithSlash () {
			var assetPath = Application.dataPath;
			return Directory.GetParent(assetPath).ToString() + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
		}

		public static bool IsMetaFile (string filePath) {
			if (filePath.EndsWith(AssetBundleGraphSettings.UNITY_METAFILE_EXTENSION)) return true;
			return false;
		}

		public static bool ContainsHiddenFiles (string filePath) {
			var pathComponents = filePath.Split(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR);
			foreach (var path in pathComponents) {
				if (path.StartsWith(AssetBundleGraphSettings.DOTSTART_HIDDEN_FILE_HEADSTRING)) return true;
			}
			return false;
		}

		public static string EnsurePrefabBuilderCacheDirExists(BuildTarget t, NodeData node) {
			var cacheDir = FileUtility.PathCombine(AssetBundleGraphSettings.PREFABBUILDER_CACHE_PLACE, node.Id, SystemDataUtility.GetPathSafeTargetName(t));

			if (!Directory.Exists(cacheDir)) {
				Directory.CreateDirectory(cacheDir);
			}
			if (!cacheDir.EndsWith(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString())) {
				cacheDir = cacheDir + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString();
			}
			return cacheDir;
		}


		public static string EnsureAssetBundleCacheDirExists(BuildTarget t, NodeData node, bool remake = false) {
			var cacheDir = FileUtility.PathCombine(AssetBundleGraphSettings.BUNDLEBUILDER_CACHE_PLACE, node.Id, BuildTargetUtility.TargetToAssetBundlePlatformName(t));

			if (!Directory.Exists(cacheDir)) {
				Directory.CreateDirectory(cacheDir);
			} else {
				if(remake) {
					RemakeDirectory(cacheDir);
				}
			}
			return cacheDir;
		}


		public static string GetImportSettingTemplateFilePath(AssetReference a) {
			if(a.filterType == typeof(ModelImporter)) {
				return AssetBundleGraphSettings.SETTINGTEMPLATE_FILE_MODEL;
			}
			if(a.filterType == typeof(AudioImporter)) {
				return AssetBundleGraphSettings.SETTINGTEMPLATE_FILE_AUDIO;
			}
			if(a.filterType == typeof(TextureImporter)) {
				return AssetBundleGraphSettings.SETTINGTEMPLATE_FILE_TEXTURE;
			}
			return null;
		}
	}
}