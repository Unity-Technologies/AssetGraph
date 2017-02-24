using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

namespace UnityEngine.AssetBundles.GraphTool {

	public class LogUtility {

//		class MyLogHandler : ILogHandler
//		{
//			public void LogFormat (LogType logType, UnityEngine.Object context, string format, params object[] args)
//			{
//				Debug.logger.logHandler.LogFormat (logType, context, format, args);
//			}
//
//			public void LogException (Exception exception, UnityEngine.Object context)
//			{
//				Debug.logger.LogException (exception, context);
//			}
//		}

		public static readonly string kTag = "AssetBundle";

		private static Logger s_logger;

		public static Logger Logger {
			get {
				if(s_logger == null) {
					#if UNITY_2017_1_OR_NEWER
					s_logger = new Logger(Debug.unityLogger.logHandler);
					#else
					s_logger = new Logger(Debug.logger.logHandler);
					#endif
				}

				return s_logger;
			}
		}
	}
}
