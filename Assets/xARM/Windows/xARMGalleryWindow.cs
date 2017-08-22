#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using xARM;


public class xARMGalleryWindow : EditorWindow {
	
	#region Fields
	// Ref to itself
	private static xARMGalleryWindow _myWindow;
	private static string myWindowTitle = "xARM Gallery";
	
	// cache of all active ScreenCaps
	private static List<xARMScreenCap> activeScreenCaps = new List<xARMScreenCap>();
	// which ScreenCap is next to update (update only one ScreenCap at a time/Update())
	private static int currScreenCapIndex = 0;
	// count of screenCaps to update once (used by the update1x-button)
	private static int screenCapsToUpdateOnce = 0;
	private static bool updateOnce = false;
	private static int postUpdateFramesToGo = 0;
	// limit update interval
	private double lastUpdateTime = 0.0f;
	private static float updateInterval;
	
	// GUI help (error, warning, info) to display
	public static string WarningBoxText = "";
	public static string InfoBoxText = "";
	
	// GUI look
	public static bool RepaintNextUpdate = false;
	private static Vector2 scrollPos;
	private static Rect scaleRatioRectLayout;
	private static GUIStyle labelStyle = new GUIStyle();
	
	private static int screenCapMinWidth = 160;
	private static int screenCapMinHeight = 160;
	
	private static char charInch = '\u2033';
	private static char charLandscape = '\u25AD';
	private static char charPortrait = '\u25AF';
	#endregion
	
	#region Properties
	private static xARMGalleryWindow myWindow{
		get {
			// refresh Ref if it's lost
			if(!_myWindow) _myWindow = GetWindow<xARMGalleryWindow>();
			return _myWindow;
		}
	}
	#endregion
		
	#region Functions
	// open GalleryWindow
	[MenuItem ("Window/xARM/xARM Gallery", false, 120)]
    public static void ShowxARMGalleryWindow() {
		// undock msg
		 if(EditorUtility.DisplayDialog ("xARM will now undock the GameView", 
			"Please keep the GameView undocked while working with xARM.", 
			"OK", "Cancel")){
			_myWindow = GetWindow<xARMGalleryWindow>();
#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3|| UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0
			_myWindow.title = myWindowTitle;
#else
			_myWindow.titleContent = new GUIContent(myWindowTitle);
#endif
			_myWindow.minSize = new Vector2(300, 150);
			
			// undock GameView only on window open (not while switching between Editor and Play mode)
			xARMManager.FloatingGameView (xARMManager.DefaultGameViewResolution);
		}
		
    }
	
	#region OnMessage
	void OnEnable (){
		// Init everything
		xARMManager.CreateProxyGO ();

		// Init GUI look
		labelStyle.alignment = TextAnchor.UpperCenter;
		
		// ensure correct display after Edit->Pause/Play mode switch
		RepaintNextUpdate = true;
	}
	
	// update one ScreenCap per Update()
	void Update(){
		// send heartbeat
		xARMManager.SendHeartbeatGallery();

		// ensures a xARM Proxy exists
		xARMManager.CreateProxyGO ();

		// finalize SC update (set default GV size etc.)
		if(xARMManager.AllScreenCapsUpdatedRecently() && !xARMManager.FinalizeScreenCapInProgress){
			// reset 1xUpdate
			if(updateOnce) {
				screenCapsToUpdateOnce = 0;
				updateOnce = false;
			}

			xARMManager.GalleryIsUpdating = false;
			xARMManager.FinalizeScreenCapInProgress = true;
			xARMManager.FinalizeScreenCapUpdate ();
		}
		// ensure some frames after default resolution is set
		else if(xARMManager.AllScreenCapsUpdatedRecently() && xARMManager.FinalizeScreenCapInProgress){
			// still frames to go?
			if(postUpdateFramesToGo <= 0){
				xARMManager.SetAllScreenCapsUpdated();
				
#if UNITY_EDITOR_WIN
				// move GV back to hidden position (Win)
				xARMManager.UpdateGameView ();
#endif

				xARMManager.FinalizeScreenCapInProgress = false;
			} else {
				xARMManager.EnsureNextFrame ();
				postUpdateFramesToGo--;
			}
		}
		// auto update or 1xUpdate
		else if(((xARMManager.Config.GalleryAutoUpdateInEditorMode && xARMManager.CurrEditorMode == EditorMode.Edit) || // Editor mode
				(xARMManager.Config.GalleryAutoUpdateInPauseMode && xARMManager.CurrEditorMode == EditorMode.Pause) || // Pause mode
		    	updateOnce) && // 1x Update
			!xARMManager.ScreenCapUpdateInProgress)
		{ // update ScreenCap

			// limit SC-block updates
			if(!updateOnce && !xARMManager.GalleryIsUpdating && xARMManager.CurrEditorMode == EditorMode.Edit){ // limit auto update in Edit mode (only for SC-blocks)
				updateInterval = 1f / xARMManager.Config.GalleryUpdateIntervalLimitEdit;
				
			}
			else {
				updateInterval = 0f; // do not limit update interval
			}
			
			if(EditorApplication.timeSinceStartup > lastUpdateTime + updateInterval){ // limit updates per sec
				lastUpdateTime = EditorApplication.timeSinceStartup;

				// cache list
				activeScreenCaps = xARMManager.ActiveScreenCaps;

				// execute update?
				if(activeScreenCaps.Count != 0 && xARMManager.ProxyGO && !xARMManager.PreviewIsUpdating && !xARMManager.GameViewHasFocus && !xARMManager.OtherUpdateInProgress){
					// find next screenCap to update
					if(currScreenCapIndex >= activeScreenCaps.Count) currScreenCapIndex = 0;
					xARMScreenCap currScreenCap = activeScreenCaps[currScreenCapIndex];
					
					// update screencap only if it's outdated (try one time) OR update1x is clicked
					if(xARMManager.IsToUpdate(currScreenCap) || updateOnce){

						if(xARMManager.GalleryIsUpdating == false && xARMManager.OnStartScreenCapUpdate != null) xARMManager.OnStartScreenCapUpdate ();

						xARMManager.GalleryIsUpdating = true;
						xARMManager.UpdateScreenCap (currScreenCap);
						
						currScreenCapIndex++;
						
						// decrease Update1x-to-go count
						if(updateOnce && screenCapsToUpdateOnce > 0){
							screenCapsToUpdateOnce--;
						}

						// number os frames to step after all SCs are updated
						postUpdateFramesToGo = xARMManager.Config.FramesToWait;
						
					} else { // skip SC
						currScreenCapIndex++;
					}
				}
			}
		}
		
		// ensure a next frame while waiting for one
		if(xARMManager.ScreenCapUpdateInProgress && xARMManager.GalleryIsUpdating && xARMManager.GalleryWindowIsAlive) xARMManager.EnsureNextFrame ();

	 	if(activeScreenCaps.Count == 0){
			InfoBoxText = "No ScreenCaps active. Open xARM Options to activate target Aspect Ratios and Resolutions.";
		} else {
			InfoBoxText = "";
		}

		// repaint window
		if(RepaintNextUpdate){
			myWindow.Repaint ();
			RepaintNextUpdate = false;
		}
	}

	void OnGUI (){ // is only triggered by Repaint after SC update
		// cache list
		activeScreenCaps = xARMManager.ActiveScreenCaps;
		
		DrawControls ();
		scrollPos = EditorGUILayout.BeginScrollView (scrollPos);
		DrawGallery ();
		EditorGUILayout.EndScrollView ();
	}
	
	
	void OnDestroy (){ // isn't executed on Editor close
		// cleanup
		xARMManager.RemoveProxyGO ();
	}
	#endregion
	
	#region Draw
	private static void DrawControls (){
		
		// draw toolbar filling the whole window width
		EditorGUILayout.BeginHorizontal (EditorStyles.toolbar, GUILayout.MaxWidth(myWindow.position.width));
		
		GUILayout.Label ("Update:", EditorStyles.toolbarButton);
		if(GUILayout.Button ("1x", EditorStyles.toolbarButton)) {
			updateOnce = true;
			screenCapsToUpdateOnce = activeScreenCaps.Count;
			xARMManager.SceneChanged (); // fake scene change
		}
		xARMManager.Config.GalleryAutoUpdateInEditorMode = GUILayout.Toggle (xARMManager.Config.GalleryAutoUpdateInEditorMode, "Edit", EditorStyles.toolbarButton);
		xARMManager.Config.GalleryAutoUpdateInPauseMode = GUILayout.Toggle (xARMManager.Config.GalleryAutoUpdateInPauseMode, "Pause", EditorStyles.toolbarButton);
		EditorGUILayout.Space ();
		GUILayout.FlexibleSpace ();
		// screenCaps per row silder
		xARMManager.Config.GalleryScreenCapsPerRow = Mathf.RoundToInt (GUILayout.HorizontalSlider (xARMManager.Config.GalleryScreenCapsPerRow, 1, Mathf.Max (activeScreenCaps.Count, 1), 
			GUILayout.ExpandWidth (true), GUILayout.MinWidth (40), GUILayout.MaxWidth (100)));

		// Tools foldout
		GUILayout.Label ("Tools", EditorStyles.toolbarButton);
		switch (EditorGUILayout.Popup (-1, new string[3] {"Export all ScreenCaps as PNGs", "Options", "(Un)Hide Game View"}, EditorStyles.toolbarDropDown, GUILayout.Width(15))){
		case 0: // save all SCs
			xARMManager.SaveAllScreenCapFiles ();
			break;
			
		case 1: // open Options window
			xARMOptionsWindow.DisplayWizard ();
			break;
			
		case 2: // (un)hide GV
			xARMManager.SwitchHideGameView ();
			break;

		}
		EditorGUILayout.EndHorizontal ();
		
#if UNITY_3_3 || UNITY_3_4
		if(WarningBoxText != "") GUILayout.Label (WarningBoxText);
		if(InfoBoxText != "") GUILayout.Label (InfoBoxText);
#else
		if(WarningBoxText != "") EditorGUILayout.HelpBox (WarningBoxText, MessageType.Warning, true); // Unity 3.3 Error
		if(InfoBoxText != "") EditorGUILayout.HelpBox (InfoBoxText, MessageType.Info, true); // Unity 3.3 Error
#endif
	}
	
	
	private static void DrawGallery (){
		int entriesPerRow = xARMManager.Config.GalleryScreenCapsPerRow;

		EditorGUILayout.BeginVertical ();
		EditorGUILayout.BeginHorizontal ();
		
		for(int x=0; x < activeScreenCaps.Count; x++){
			xARMScreenCap currScreenCap = activeScreenCaps[x];
			
			// begin new row?
			if(x % entriesPerRow == 0){
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
			}
				
			DrawScreenCap (currScreenCap, x);

			// last entry?
			if(x == activeScreenCaps.Count - 1){

				// fill last row to ensure same/correct size of last row entries
				int entriesInLastRow = activeScreenCaps.Count % entriesPerRow;

				if(entriesInLastRow < entriesPerRow && entriesInLastRow != 0 && !xARMManager.Config.UseFixedScreenCapSize){
					int entriesToFill = entriesPerRow - entriesInLastRow;

					// add invisible dummy entries
					for(int y=0; y < entriesToFill; y++){
						// use same layout cammands as below for similar spacing

						EditorGUILayout.BeginVertical ();
						GUILayoutUtility.GetAspectRect (currScreenCap.Aspect, GUILayout.MinWidth (screenCapMinWidth), GUILayout.MinHeight (screenCapMinHeight));

						EditorGUILayout.BeginHorizontal ();
						EditorGUILayout.EndHorizontal ();

						EditorGUILayout.EndVertical ();
					}

				}
			}

		}

		EditorGUILayout.EndHorizontal ();
		EditorGUILayout.EndVertical ();
		
	}
	
	private static void DrawScreenCap (xARMScreenCap screenCap, int screenCapIndex){
		Rect screenCapRect, screenCapBox;
		float screenCapPXOnScreen; // SC px width 
		
		// create Box and Rect
		if(xARMManager.Config.UseFixedScreenCapSize){
			screenCapBox = EditorGUILayout.BeginVertical ("box", GUILayout.Width (xARMManager.Config.FixedScreenCapSize.x), GUILayout.Height (xARMManager.Config.FixedScreenCapSize.y));
			screenCapRect = GUILayoutUtility.GetRect (xARMManager.Config.FixedScreenCapSize.x, xARMManager.Config.FixedScreenCapSize.y);
			screenCapPXOnScreen = xARMManager.Config.FixedScreenCapSize.x;

		} else {
			screenCapBox =  EditorGUILayout.BeginVertical ("box");
			screenCapRect = GUILayoutUtility.GetAspectRect (screenCap.Aspect, GUILayout.MinWidth (screenCapMinWidth), GUILayout.MinHeight (screenCapMinHeight));
			screenCapPXOnScreen = myWindow.position.width / xARMManager.Config.GalleryScreenCapsPerRow; // estimate SC width
		}

		// draw SC
		if(screenCap.UpdatedSuccessful){
#if UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1
			// standard draw (with Play mode tint)
			GUI.DrawTexture (screenCapRect, screenCap.Texture, ScaleMode.ScaleToFit);
#else
			// draw ScreenCap in prepared rect (use EditorGUI.DrawTextureTransparent to draw it without Play mode tint)
			EditorGUI.DrawTextureTransparent (screenCapRect, screenCap.Texture, ScaleMode.ScaleToFit);
#endif

			// select SC in xARM Preview on click
			if(screenCapBox.Contains (Event.current.mousePosition)){
				if(Event.current.type == EventType.MouseDown && Event.current.button == 0){
					xARMPreviewWindow.ChangeActiveScreenCap (screenCapIndex);
				}
			}

		} else {
#if UNITY_3_3 || UNITY_3_4
			GUILayout.Label ("ScreenCap could not been updated");
#else
			EditorGUILayout.HelpBox ("ScreenCap could not been updated", MessageType.Warning, true); // Unity 3.3 Error
#endif
		}
		
		GUILayout.Space (2);
		
		// description
		EditorGUILayout.BeginHorizontal ();
		if(screenCap.IsLandscape){
			GUILayout.Label (screenCap.Name + " " + screenCap.Diagonal + charInch + " " + screenCap.DPILabel + " (" + screenCap.AspectLabel + ")" + "\n" + 
				screenCap.Resolution.x + "x" + screenCap.Resolution.y + "px (" + screenCap.ResolutionLabel + " " + charLandscape + ")", labelStyle);
		} else {
			GUILayout.Label (screenCap.Name + " " + screenCap.Diagonal + charInch + " " + screenCap.DPILabel + " (" + screenCap.AspectLabel + ")" + "\n" + 
				screenCap.Resolution.x + "x" + screenCap.Resolution.y + "px (" + screenCap.ResolutionLabel + " " + charPortrait + ")", labelStyle);
		}

		// Scale Ratio size
		float scaleRatioPhySizeInch = xARMManager.Config.ScaleRatioInch; // phy size of Scale Ratio
		float screenCapPhySizeInch = screenCap.Resolution.x / screenCap.DPI; // phy size of SC

		// get SC on screen size
		if(Event.current.type.Equals(EventType.Repaint)){
			screenCapPXOnScreen = screenCapRect.width; // get precise SC width (only possible while Repaint)
		}

		// scale Scale Ratio the same as the SC was scaled
		float scaleRatioPX = scaleRatioPhySizeInch * screenCapPXOnScreen / screenCapPhySizeInch; 
		Rect scaleRatioRect = GUILayoutUtility.GetRect (scaleRatioPX, scaleRatioPX, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));


		// Draw Scale Ratio and mark selected SC with a colored one
		Color prevGUIColor = GUI.color;
		if(xARMPreviewWindow.SelectedScreenCap is xARMScreenCap && xARMPreviewWindow.SelectedScreenCap == screenCap){
			GUI.color = new Color(0.0f, 0.9f, 0.0f);
		}
		GUI.DrawTexture (scaleRatioRect, EditorGUIUtility.whiteTexture, ScaleMode.ScaleToFit, false);
		GUI.color = prevGUIColor;

		EditorGUILayout.EndHorizontal ();
		EditorGUILayout.EndVertical ();
	}
	#endregion
	
	#endregion
}
#endif