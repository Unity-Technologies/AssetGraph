using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace Unity.AssetGraph {

    public static class VersionCompatibilityUtility {

        public static string UpdateClassName(string className) {
            if (string.IsNullOrEmpty(className))
            {
                return className;
            }
            
            if (!className.StartsWith("Unity.AssetGraph."))
            {
                className = className
                    .Replace("UnityEngine.AssetBundles.GraphTool.", "Unity.AssetGraph.") // v1.3 -> 1.5
                    .Replace("UnityEngine.AssetGraph.", "Unity.AssetGraph."); // v1.4 -> 1.5
            }

            // test remapped type class.
            var typeGetTest = Type.GetType(className);
            if (null == typeGetTest)
            {
                var fullname = className.Split(',')[0];
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    var matchingType = assembly.GetTypes().FirstOrDefault(t => t.FullName == fullname);
                    if (matchingType != null)
                    {
                        return matchingType.AssemblyQualifiedName;
                    }
                }
                
                LogUtility.Logger.LogFormat(LogType.Error, "[VersionCompatibilityUtility] Type not found for class: {0}.", className );						
            }

            return className;
        }
    }
}
