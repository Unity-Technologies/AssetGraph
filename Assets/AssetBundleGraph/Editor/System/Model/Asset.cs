using UnityEngine;
using UnityEditor;

using System;
using System.IO;

namespace AssetBundleGraph {
	public class Asset {
		public readonly Guid guid;
		public readonly string assetDatabaseId;
		public readonly string absoluteAssetPath;
//		public readonly string sourceBasePath;
//		public readonly string pathUnderSourceBase;
		public readonly string importFrom;
		public readonly string exportTo;
		public readonly Type assetType;
		public readonly bool isNew;		
		public readonly bool isBundled;
		public readonly string variantName;

		public string id {
			get {
				return guid.ToString();
			}
		}

		public string fileNameAndExtension {
			get {
				if(absoluteAssetPath != null) {
					return Path.GetFileName(absoluteAssetPath);
				}
				if(importFrom != null) {
					return Path.GetFileName(importFrom);
				}
				if(exportTo != null) {
					return Path.GetFileName(exportTo);
				}
				return null;
			}
		}

		/**
			Create Asset info from Loader
		*/
		public static Asset CreateNewAssetFromLoader (string absoluteAssetPath, string importFrom) {
			return new Asset(
				guid: Guid.NewGuid(),
				assetDatabaseId:AssetDatabase.AssetPathToGUID(importFrom),
				absoluteAssetPath:absoluteAssetPath,
				importFrom:importFrom,
				assetType:TypeUtility.GetTypeOfAsset(importFrom)
			);
		}

		/**
			new assets which is generated on Imported or Prefabricated.
		*/
		public static Asset CreateNewAssetWithImportPathAndStatus (string importFrom, bool isNew, bool isBundled) {
			return new Asset(
				guid:Guid.NewGuid(),
				assetDatabaseId:AssetDatabase.AssetPathToGUID(importFrom),
				importFrom:importFrom,
				assetType:TypeUtility.GetTypeOfAsset(importFrom),
				isNew:isNew,
				isBundled:isBundled
			);
		}

		/**
		 * used by BundleBuilder
		*/
		public static Asset CreateAssetWithImportPath (string importFrom) {
			return new Asset(
				guid: Guid.NewGuid(),
				importFrom:importFrom);
		}

		/**
		 * used by Exporter
		*/
		public static Asset CreateAssetWithExportPath (string exportTo) {
			return new Asset(
				guid: Guid.NewGuid(),
				exportTo:exportTo
			);
		}

		/**
			Create Asset with new assetType configured
		*/
		public static Asset DuplicateAsset (Asset asset) {
			return new Asset(
				guid:asset.guid,
				assetDatabaseId:asset.assetDatabaseId,
				absoluteAssetPath:asset.absoluteAssetPath,
				importFrom:asset.importFrom,
				exportTo:asset.exportTo,
				assetType:asset.assetType,
				isNew:asset.isNew,
				isBundled:asset.isBundled
			);
		}

		/**
			Create Asset with new assetType configured
		*/
		public static Asset DuplicateAssetWithNewType (Asset asset, Type newAssetType) {
			return new Asset(
				guid:asset.guid,
				assetDatabaseId:asset.assetDatabaseId,
				absoluteAssetPath:asset.absoluteAssetPath,
				importFrom:asset.importFrom,
				exportTo:asset.exportTo,
				assetType:newAssetType,
				isNew:asset.isNew,
				isBundled:asset.isBundled
			);
		}

		public static Asset DuplicateAssetWithVariant (Asset asset, string variantName) {
			return new Asset(
				guid:asset.guid,
				assetDatabaseId:asset.assetDatabaseId,
				absoluteAssetPath:asset.absoluteAssetPath,
				importFrom:asset.importFrom,
				exportTo:asset.exportTo,
				assetType:asset.assetType,
				isNew:asset.isNew,
				isBundled:asset.isBundled,
				variantName:asset.variantName
			);
		}

		/**
			Create Asset with new status (isNew, isBundled) configured
		*/
		public static Asset DuplicateAssetWithNewStatus (Asset asset, bool isNew, bool isBundled) {
			return new Asset(
				guid:asset.guid,
				assetDatabaseId:asset.assetDatabaseId,
				absoluteAssetPath:asset.absoluteAssetPath,
				importFrom:asset.importFrom,
				exportTo:asset.exportTo,
				assetType:asset.assetType,
				isNew:isNew,
				isBundled:isBundled
			);
		}

		private Asset (
			Guid guid,
			string assetDatabaseId = null,
			string absoluteAssetPath = null,
//			string sourceBasePath = null,
//			string fileNameAndExtension = null,
//			string pathUnderSourceBase = null,
			string importFrom = null,
			string exportTo = null,
			Type assetType = null,
			bool isNew = false,
			bool isBundled = false,
			string variantName = null
		) {
			if(assetType == typeof(object)) {
				throw new AssetBundleGraphException("Unknown type asset is created:" + absoluteAssetPath);
			}

			this.guid = guid;
			this.absoluteAssetPath = absoluteAssetPath;
//			this.sourceBasePath = sourceBasePath;
//			this.fileNameAndExtension = fileNameAndExtension;
//			this.pathUnderSourceBase = pathUnderSourceBase;
			this.importFrom = importFrom;
			this.exportTo = exportTo;
			this.assetDatabaseId = assetDatabaseId;
			this.assetType = assetType;
			this.isNew = isNew;
			this.isBundled = isBundled;
			this.variantName = variantName;
		}
/*		
		public static string GetPathWithoutBasePath (string localPathWithBasePath, string basePath) {
			var replaced = localPathWithBasePath.Replace(basePath, string.Empty);
			if (replaced.StartsWith(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString())) return replaced.Substring(1);
			return replaced;
		}
*/
		public string GetAbsolutePathOrImportedPath () {
			if (absoluteAssetPath != null) {
				return absoluteAssetPath;
			}
			return importFrom;		
		}
	}
}