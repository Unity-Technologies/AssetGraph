using UnityEditor;

[InitializeOnLoad]
public class TestIgniter {

	static TestIgniter () {
		var testContext = new Test();

		// remove comment -> run tests.
		// testContext.RunTests();
	}

	[MenuItem("AssetGraphTest/Run...")]
	public static void RunTests () {
		var testContext = new Test();
		testContext.RunTests();
	}


}