using UnityEditor;

public class TestIgniter {

	[MenuItem("AssetGraphTest/Run...")]
	public static void RunTests () {
		var testContext = new Test();
		testContext.RunTests();
	}

}