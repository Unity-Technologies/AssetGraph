using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;


namespace AssetGraph {
	public class FileController {
		public static void RemakeDirectory (string localFolderPath) {
			if (Directory.Exists(localFolderPath)) Directory.Delete(localFolderPath, true);
			Directory.CreateDirectory(localFolderPath);
		}
		
		public static void CopyFileFromGlobalToLocal (string absoluteSourceFilePath, string localTargetFilePath) {
			var parentDirectoryPath = Path.GetDirectoryName(localTargetFilePath);
			Directory.CreateDirectory(parentDirectoryPath);
			FileUtil.CopyFileOrDirectory(absoluteSourceFilePath, localTargetFilePath);
		}


		public static List<string> FilePathsInFolderWithoutMeta (string localFolderPath) {
			var filePaths = new List<string>();
			GetFilePathsRecursive(localFolderPath, filePaths);
			return filePaths.Where(path => !path.EndsWith(AssetGraphSettings.UNITY_METAFILE_EXTENSION)).ToList();
		}

		private static void GetFilePathsRecursive (string localFolderPath, List<string> filePaths) {
			var folders = Directory.GetDirectories(localFolderPath);
			foreach (var folder in folders) {
				GetFilePathsRecursive(folder, filePaths);
			}

			var files = Directory.GetFiles(localFolderPath);
			filePaths.AddRange(files);
		}
	}
}