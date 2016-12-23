using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

namespace AssetBundleGraph {

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
					s_logger = new Logger(Debug.logger.logHandler);
				}

				return s_logger;
			}
		}
	}
}
