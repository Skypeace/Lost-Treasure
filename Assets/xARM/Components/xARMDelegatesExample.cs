#if UNITY_EDITOR
using UnityEngine;

[ExecuteInEditMode]
public class xARMDelegatesExample : MonoBehaviour {
	/* This is an example of how you can use the xARM delegates to hook you own code. 
	 * Drop this script on a GameObject in your scene to see the different Screen 
	 * resolutions while updating the ScreenCaps.
	 * 
	 * Note: Do NOT put your custom code into the xARM folder to enable easy updating of xARM.
	 *  
	 * xARMManager.OnStartScreenCapUpdate
	 * 	Use this to run your code directly before update of all ScreenCaps starts. 
	 * 	(e.g. get/set variables used during all updates, store values for later restore).
	 * 	The GameView's default resolution (set in Options) is still active when this is called.
	 *	Called once per batch.
	 * 
	 * xARMManager.OnPreScreenCapUpdate
	 * 	Use this to run your code directly before a ScreenCap is updated.
	 * 	(e.g. recreate your GUI if it doesn't automatically).
	 * 	The ScreenCap's resolution is set before this is called.
	 *	Called once per ScreenCap.
	 * 
	 * xARMManager.OnPostScreenCapUpdate
	 * 	Use this to run your code directly after a ScreenCap is updated.
	 * 	(e.g. reset changes made in OnPreScreenCapUpdate). 
	 * 	The ScreenCap's resolution is still active when this is called.
	 *	Called once per ScreenCap.
	 * 
	 * xARMManager.OnFinalizeScreenCapUpdate
	 * 	Use this to run your code directly after all ScreenCaps are updated.
	 * 	(e.g. reset GUI or changes made in OnStartScreenCapUpdate).
	 * 	The GameView's default resolution (set in Options) is set before this is called.
	 *	Called once per batch.
	 * 
	 */
	
	// hook delegates
	void OnEnable () {
		xARMManager.OnStartScreenCapUpdate += StartUpdate;
		xARMManager.OnPreScreenCapUpdate += PreUpdate;
		xARMManager.OnPostScreenCapUpdate += PostUpdate;
		xARMManager.OnFinalizeScreenCapUpdate += FinUpdate;
	}
	
	// unhook delegates
	void OnDisable () {
		xARMManager.OnStartScreenCapUpdate -= StartUpdate;
		xARMManager.OnPreScreenCapUpdate -= PreUpdate;
		xARMManager.OnPostScreenCapUpdate -= PostUpdate;
		xARMManager.OnFinalizeScreenCapUpdate -= FinUpdate;
	}
	
	
	// your custom functions
	private void StartUpdate (){
		Debug.Log ("StartUpdate: " + Screen.width + "x" + Screen.height);
	}

	private void PreUpdate (){
		Debug.Log ("PreUpdate: " + Screen.width + "x" + Screen.height + "@" + xARMManager.UpdatingScreenCap.DPI + "dpi");
	}
	
	private void PostUpdate (){
		Debug.Log ("PostUpdate: " + Screen.width + "x" + Screen.height + "@" + xARMManager.UpdatingScreenCap.DPI + "dpi");
	}
	
	private void FinUpdate (){
		Debug.Log ("FinUpdate: " + Screen.width + "x" + Screen.height);
	}
	
}
#endif