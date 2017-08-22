#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using xARM;

public class xARMOptionsWindow : ScriptableWizard {
	
	#region Fields
	// Ref to itself
	private static xARMOptionsWindow _myWindow;
	
	// GUI look
	private static Vector2 scrollPos;
	private static bool showCheckDPI = false;
	private static bool showCheckScaleRatio = false;
	private static bool prevToggleState;
	
	// sizes
	private static int spacing = 10;
	private static int labelWidthEnabled = 36;
	private static int labelWidthGroup = 68;
	private static int labelWidthName = 68;
	private static int labelWidthDiagonal = 60;
	private static int labelWidthDPILabel = 65;
	private static int labelWidthDPI = 40;
	private static int labelWidthAspectLabel = 48;
	private static int labelWidthResolution = 88;
	private static int labelWidthResolutionLabel = 68;
	private static int labelWidthFormat = 68;
	private static int labelWidthStatsPos = 66;
	private static int labelWidthStatsPerc = 54;
	private static int labelMinWidthDescription = 100;
	
	// unicode chars
	private static char charAsc = '\u25B2';
	private static char charDesc = '\u25BC';
	private static char charInch = '\u2033';
	private static char charApprox = '\u2248';
	private static char charLandscape = '\u25AD';
	private static char charPortrait = '\u25AF';
	
	// last sort
	private static Func<xARMScreenCap, object> lastSortedColumn;
	private static bool lastSortWasAsc = false;
	#endregion
	
	#region Properties
	#endregion
	
	#region Functions
	[MenuItem ("Window/xARM/xARM Options", false, 150)]
	public static void DisplayWizard (){
		_myWindow = EditorWindow.GetWindow<xARMOptionsWindow>(true, "xARM Options", true);
		_myWindow.minSize = new Vector2(350, 150);
    }

	
	void OnGUI (){
		scrollPos = EditorGUILayout.BeginScrollView (scrollPos);

		DrawOptions ();
		EditorGUILayout.Space ();
		DrawScreenCapGroups ();
		EditorGUILayout.Space ();
		DrawFooter ();
		
		EditorGUILayout.EndScrollView ();
		
	}
	
	#region Draw
	private static void DrawOptions (){
		int blockWidth = 400;

		EditorGUILayout.BeginVertical ("box");
		EditorGUILayout.BeginVertical ("box", GUILayout.Width(blockWidth));

		xARMManager.Config.FoldoutContact = Foldout (xARMManager.Config.FoldoutContact, "Contact", true, EditorStyles.foldout);
		if(xARMManager.Config.FoldoutContact){

			GUIStyle style = new GUIStyle();
			style.fontStyle = FontStyle.Italic;

			GUILayout.Label ("Thanks for purchasing xARM.", style, GUILayout.MaxWidth(blockWidth));
			GUILayout.Label ("Feedback and feature requests are highly appreciated.", style, GUILayout.MaxWidth(blockWidth));
			GUILayout.Label ("Please take a moment to give xARM a review at the Asset Store.", style, GUILayout.MaxWidth(blockWidth));
			GUILayout.Label ("Thanks!", style, GUILayout.MaxWidth(blockWidth));

			EditorGUILayout.Space();
			GUILayout.Label ("Support & Feedback:");
			if(GUILayout.Button("Unity Forum: xARM Thread", GUILayout.Width(blockWidth / 2))) Application.OpenURL("http://forum.unity3d.com/threads/196174/");
			if(GUILayout.Button("Email: support@flyingwhale.de", GUILayout.Width(blockWidth / 2))) Application.OpenURL("mailto:support@flyingwhale.de");
			EditorGUILayout.Space();

			GUILayout.Label ("Infos & Updates:");
			if(GUILayout.Button("Twitter: @ThavronFW", GUILayout.Width(blockWidth / 2))) Application.OpenURL("https://twitter.com/ThavronFW");
			if(GUILayout.Button("YouTube: Flying Whale", GUILayout.Width(blockWidth / 2))) Application.OpenURL("https://www.youtube.com/channel/UC2CU8aCaWclJ5C6dOQFzdVg");
			EditorGUILayout.Space ();

			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button("All assets by Flying Whale", GUILayout.Width(blockWidth / 2))) Application.OpenURL("http://u3d.as/5aJ");
			EditorGUILayout.BeginVertical();
			GUILayout.Space(8);
			GUILayout.Label ("more comming soon...", style, GUILayout.MaxWidth(blockWidth));
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(4);
			GUILayout.Label ("NEW: xCBM: Color Blindness Master", style);
			GUILayout.Space(4);
		}

		EditorGUILayout.EndVertical();
		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical ("box");
		GUILayout.Label ("Options", EditorStyles.boldLabel, GUILayout.MaxWidth(blockWidth));

		// Global
		EditorGUILayout.BeginVertical ("box", GUILayout.Width(blockWidth));
		xARMManager.Config.FoldoutOptions = Foldout (xARMManager.Config.FoldoutOptions, "Global Options", true, EditorStyles.foldout);
		if(xARMManager.Config.FoldoutOptions){
			GUILayout.Label ("Editor DPI", EditorStyles.boldLabel);
			GUILayout.Label ("Used by 1:1phy (physical size) mode");
			xARMManager.Config.EditorDPI = EditorGUILayout.IntField ("DPI", xARMManager.Config.EditorDPI);
			// DPI check
			showCheckDPI = GUILayout.Toggle (showCheckDPI, "Verify");
			if(showCheckDPI){
				GUILayout.Label ("1x1" + charInch + " " + charApprox + " 2.5x2.5cm");
				Rect checkDPIRect = GUILayoutUtility.GetRect (xARMManager.Config.EditorDPI, xARMManager.Config.EditorDPI,
					GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
				GUI.DrawTexture (checkDPIRect, EditorGUIUtility.whiteTexture, ScaleMode.ScaleToFit, false);
			}
			EditorGUILayout.Space ();

#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3|| UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3
#else	// 5.4+
			GUILayout.Label ("Editor Mode", EditorStyles.boldLabel);
			xARMManager.Config.EditorUsesHIDPI = GUILayout.Toggle (xARMManager.Config.EditorUsesHIDPI, "Editor is using Retina/HiDPI mode.");
			EditorGUILayout.Space ();
#endif

			GUILayout.Label ("Game View Position", EditorStyles.boldLabel);
			GUILayout.Label ("Position of the Game View while it is not hidden");
			xARMManager.Config.AutoTraceGameViewPosition = GUILayout.Toggle (xARMManager.Config.AutoTraceGameViewPosition, "Trace Game View position automatically");
			if(!xARMManager.Config.AutoTraceGameViewPosition){
				xARMManager.Config.FixedGameViewPosition = EditorGUILayout.Vector2Field ("Fixed position", xARMManager.Config.FixedGameViewPosition);
				GUILayout.BeginHorizontal ();
				if(GUILayout.Button ("Test", GUILayout.MaxWidth (blockWidth / 2))){
					GUIUtility.keyboardControl = 0; // remove focus from vec2 fields
					xARMManager.Config.HideGameView = false;
					xARMManager.UpdateGameView();
				}
				if(GUILayout.Button ("Get current Game View position", GUILayout.MaxWidth (blockWidth / 2))){
					GUIUtility.keyboardControl = 0; // remove focus from vec2 fields
					// store current GV position
					xARMManager.Config.FixedGameViewPosition = xARMManager.CurrentGameViewPosition - new Vector2(0, xARMManager.YScrollOffset);
				}
				GUILayout.EndHorizontal ();
			}
			EditorGUILayout.Space ();
			
			GUILayout.Label ("Hidden Game View Position", EditorStyles.boldLabel);
			GUILayout.Label ("Position of the Game View while it is hidden/offscreen");
			xARMManager.Config.HiddenGameViewPosition = EditorGUILayout.Vector2Field ("Hidden position", xARMManager.Config.HiddenGameViewPosition);
			GUILayout.BeginHorizontal ();
			if(GUILayout.Button ("Test", GUILayout.MaxWidth (blockWidth / 2))){
				GUIUtility.keyboardControl = 0; // remove focus from vec2 fields
				xARMManager.Config.HideGameView = true;
				xARMManager.UpdateGameView();
			}
			if(GUILayout.Button ("Get current Game View position", GUILayout.MaxWidth (blockWidth / 2))){
				GUIUtility.keyboardControl = 0; // remove focus from vec2 fields
				// store current GV position
				xARMManager.Config.HiddenGameViewPosition = xARMManager.CurrentGameViewPosition - new Vector2(0, xARMManager.YScrollOffset);
			}
			GUILayout.EndHorizontal ();
			EditorGUILayout.Space ();

			GUILayout.Label ("Update Delay", EditorStyles.boldLabel);
			GUILayout.Label ("Frames to wait between resolution change and ScreenCap update");
			xARMManager.Config.FramesToWait = EditorGUILayout.IntField ("Frames (default: 2)", xARMManager.Config.FramesToWait);
			EditorGUILayout.Space ();

			GUILayout.Label ("Integration", EditorStyles.boldLabel);
			prevToggleState = xARMManager.Config.IntegrationUGUIPhySize;
			xARMManager.Config.IntegrationUGUIPhySize = GUILayout.Toggle (xARMManager.Config.IntegrationUGUIPhySize, "uGUI Canvas Scaler - Constant Physical Size support");
			// on change recreate Proxy with new settings
			if(xARMManager.Config.IntegrationUGUIPhySize != prevToggleState){
				xARMManager.RecreateProxyGO();
			}
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(20);
			if(xARMManager.Config.IntegrationUGUIPhySize) xARMManager.Config.IntegrationUGUIPhySizeKeepDPI = GUILayout.Toggle (xARMManager.Config.IntegrationUGUIPhySizeKeepDPI, "Keep ScreenCap's DPI in uGUI after update");
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space ();

			GUILayout.Label ("Export", EditorStyles.boldLabel);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Default export folder:");
			// select folder via dialog
			if(GUILayout.Button ("Select")){
				string path = EditorUtility.SaveFolderPanel ("Select default export folder", xARMManager.Config.ExportPath, "");
				if(path.Length > 0) xARMManager.Config.ExportPath = path;
			}
			GUILayout.EndHorizontal ();
			GUILayout.Label (xARMManager.Config.ExportPath);
			GUILayout.Space (4);
		}
		EditorGUILayout.EndVertical ();

		// Preview
		EditorGUILayout.BeginVertical ("box", GUILayout.Width(blockWidth));
		xARMManager.Config.FoldoutOptionsPreview= Foldout (xARMManager.Config.FoldoutOptionsPreview, "xARM Preview Options", true, EditorStyles.foldout);
		if(xARMManager.Config.FoldoutOptionsPreview){
			GUILayout.Label ("Update Limit", EditorStyles.boldLabel);
			GUILayout.Label ("Maximum updates per second (FPS)");
			xARMManager.Config.PreviewUpdateIntervalLimitEdit = EditorGUILayout.IntField ("Edit mode", xARMManager.Config.PreviewUpdateIntervalLimitEdit);
			xARMManager.Config.PreviewUpdateIntervalLimitPlay = EditorGUILayout.IntField ("Play mode", xARMManager.Config.PreviewUpdateIntervalLimitPlay);
			EditorGUILayout.Space ();
			GUILayout.Label ("Update Control", EditorStyles.boldLabel);
			xARMManager.Config.UpdatePreviewWhileGameViewHasFocus = GUILayout.Toggle (xARMManager.Config.UpdatePreviewWhileGameViewHasFocus, "Update while Game View has focus");
			EditorGUILayout.Space ();
			GUILayout.Label ("Resolution Sync", EditorStyles.boldLabel);
			xARMManager.Config.GameViewInheritsPreviewSize = GUILayout.Toggle (xARMManager.Config.GameViewInheritsPreviewSize, "Automatically sync selected ScreenCap's resolution to Game View");
			// display warning
			if(!xARMManager.Config.GameViewInheritsPreviewSize){
#if UNITY_3_3 || UNITY_3_4
				GUILayout.Label ("Warning: Deactivating this option causes additional Game View size changes!");
#else
				EditorGUILayout.HelpBox ("Deactivating this option causes additional Game View size changes!", MessageType.Warning, true);
#endif
			}

			xARMManager.Config.FallbackGameViewSize = EditorGUILayout.Vector2Field ("Fallback Game View resolution", xARMManager.Config.FallbackGameViewSize);
			// 2do: currently not very usefull (improve or remove)
			//			xARMManager.Config.CloseGameViewAfterUpdate = GUILayout.Toggle (xARMManager.Config.CloseGameViewAfterUpdate, "Close GameView after update (Editor mode only)");
			EditorGUILayout.Space ();
		}
		EditorGUILayout.EndVertical ();

		// Gallery
		EditorGUILayout.BeginVertical ("box", GUILayout.Width(blockWidth));
		xARMManager.Config.FoldoutOptionsGallery = Foldout (xARMManager.Config.FoldoutOptionsGallery, "xARM Gallery Options", true, EditorStyles.foldout);
		if(xARMManager.Config.FoldoutOptionsGallery){
			GUILayout.Label ("Update Limit", EditorStyles.boldLabel);
			GUILayout.Label ("Maximum updates per second (FPS)");
			xARMManager.Config.GalleryUpdateIntervalLimitEdit = EditorGUILayout.IntField ("Edit mode", xARMManager.Config.GalleryUpdateIntervalLimitEdit);
			EditorGUILayout.Space ();
			GUILayout.Label ("ScreenCap Size", EditorStyles.boldLabel);
			xARMManager.Config.UseFixedScreenCapSize = GUILayout.Toggle (xARMManager.Config.UseFixedScreenCapSize, "Display all ScreenCaps at the same fixed size");
			if(xARMManager.Config.UseFixedScreenCapSize){
				xARMManager.Config.FixedScreenCapSize = EditorGUILayout.Vector2Field ("Size", xARMManager.Config.FixedScreenCapSize);
			}
			EditorGUILayout.Space ();
			
			GUILayout.Label ("Scale Ratio", EditorStyles.boldLabel);
			GUILayout.Label ("Recommended touch target size: 0.9cm");
			xARMManager.Config.ScaleRatioCM = EditorGUILayout.FloatField ("Scale Ratio size (cm)", xARMManager.Config.ScaleRatioCM);
			if(xARMManager.Config.EditorDPI == 0){
#if UNITY_3_3 || UNITY_3_4
				GUILayout.Label ("Info: 'Editor DPI' not set.");
#else
				EditorGUILayout.HelpBox ("'Editor DPI' not set.", MessageType.Info, true);
#endif
			}
			// DPI check
			showCheckScaleRatio = GUILayout.Toggle (showCheckScaleRatio, "Verify");
			if(showCheckScaleRatio){
				float scaleRatioPX = xARMManager.Config.ScaleRatioInch * xARMManager.Config.EditorDPI;
				Rect checkScaleRatio = GUILayoutUtility.GetRect (scaleRatioPX, scaleRatioPX,
				                                                 GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
				GUI.DrawTexture (checkScaleRatio, EditorGUIUtility.whiteTexture, ScaleMode.ScaleToFit, false);
			}
			EditorGUILayout.Space ();
		}
		EditorGUILayout.EndVertical ();
		
		EditorGUILayout.EndVertical ();
	}

	
	private static void DrawScreenCapGroups (){
		
		EditorGUILayout.BeginVertical ("box");
		
		GUILayout.Label ("ScreenCaps", EditorStyles.boldLabel);
		EditorGUILayout.Space ();

		GUILayout.Label ("Filter", EditorStyles.boldLabel);
		EditorGUILayout.BeginHorizontal ();
		xARMManager.Config.ShowLandscape = GUILayout.Toggle (xARMManager.Config.ShowLandscape, charLandscape + " Landscape");
		xARMManager.Config.ShowPortrait = GUILayout.Toggle (xARMManager.Config.ShowPortrait, charPortrait + " Portrait");
		xARMManager.Config.ShowNavigationBar = GUILayout.Toggle (xARMManager.Config.ShowNavigationBar, "Navigation Bar (Android)");
		GUILayout.FlexibleSpace ();
		EditorGUILayout.EndHorizontal ();
		EditorGUILayout.Space ();

		GUILayout.Label ("Select the Aspect Ratios and Resolutions you are targeting. Sort order does also affect xARM Preview and Gallery.");
		EditorGUILayout.Space ();

		// iOS
		xARMManager.Config.FoldoutIOS = Foldout (xARMManager.Config.FoldoutIOS, "iOS", true, EditorStyles.foldout);
		if(xARMManager.Config.FoldoutIOS){
			DrawDefaultScreenCapGroup (xARMScreenCapGroup.iOS);
		}
		EditorGUILayout.Space ();

		// Android
		xARMManager.Config.FoldoutAndroid = Foldout (xARMManager.Config.FoldoutAndroid, "Android", true, EditorStyles.foldout);
		if(xARMManager.Config.FoldoutAndroid){
			DrawDefaultScreenCapGroup (xARMScreenCapGroup.Android);
		}
		EditorGUILayout.Space ();

		// Windows Phone 8
		xARMManager.Config.FoldoutWinPhone8 = Foldout (xARMManager.Config.FoldoutWinPhone8, "Windows Phone 8", true, EditorStyles.foldout);
		if(xARMManager.Config.FoldoutWinPhone8){
			DrawDefaultScreenCapGroup (xARMScreenCapGroup.WinPhone8);
		}
		EditorGUILayout.Space ();

		// WinRT
		xARMManager.Config.FoldoutWinRT = Foldout (xARMManager.Config.FoldoutWinRT, "WinRT", true, EditorStyles.foldout);
		if(xARMManager.Config.FoldoutWinRT){
			DrawDefaultScreenCapGroup (xARMScreenCapGroup.WindowsRT);
		}
		EditorGUILayout.Space ();

		// Standalone
		xARMManager.Config.FoldoutStandalone = Foldout (xARMManager.Config.FoldoutStandalone, "Standalone", true, EditorStyles.foldout);
		if(xARMManager.Config.FoldoutStandalone){
			DrawDefaultScreenCapGroup (xARMScreenCapGroup.Standalone);
		}
		EditorGUILayout.Space ();

		// Custom
		xARMManager.Config.FoldoutCustom = Foldout (xARMManager.Config.FoldoutCustom, "Custom", true, EditorStyles.foldout);
		if(xARMManager.Config.FoldoutCustom){
			DrawDefaultScreenCapGroup (xARMScreenCapGroup.Custom);
		}
		EditorGUILayout.Space ();
		
		EditorGUILayout.EndVertical ();
	}
	
	
	// Draws all ScreenCap entries of the specified group
	private static void DrawDefaultScreenCapGroup (xARMScreenCapGroup group){
		// Header
		GUILayout.BeginHorizontal ("box");
		
		DrawColumnHeader ("Active", labelWidthEnabled + spacing, xARMScreenCap=>xARMScreenCap.Enabled);
		DrawColumnHeader ("OS", labelWidthGroup, xARMScreenCap=>xARMScreenCap.Group);
		DrawColumnHeader ("Name", labelWidthName, xARMScreenCap=>xARMScreenCap.Name);
		DrawColumnHeader ("Diagonal", labelWidthDiagonal, xARMScreenCap=>xARMScreenCap.Diagonal);
		DrawColumnHeader ("DPI Group", labelWidthDPILabel, xARMScreenCap=>xARMScreenCap.DPILabel);
		DrawColumnHeader ("DPI", labelWidthDPI, xARMScreenCap=>xARMScreenCap.DPI);
		DrawColumnHeader ("Aspect", labelWidthAspectLabel, xARMScreenCap=>xARMScreenCap.AspectLabel);
		DrawColumnHeader ("Resolution (px)", labelWidthResolution, xARMScreenCap=>xARMScreenCap.Resolution.x);
		DrawColumnHeader ("Resolution", labelWidthResolutionLabel, xARMScreenCap=>xARMScreenCap.ResolutionLabel);
		DrawColumnHeader ("Format", labelWidthFormat, xARMScreenCap=>xARMScreenCap.IsLandscape);
		DrawColumnHeader ("Stats (pos)", labelWidthStatsPos, xARMScreenCap=>xARMScreenCap.StatisticsPositioning);
		DrawColumnHeader ("Stats (%)", labelWidthStatsPerc, xARMScreenCap=>xARMScreenCap.StatisticsUsedPercent);
		DrawColumnHeader ("Sample devices", labelMinWidthDescription, xARMScreenCap=>xARMScreenCap.Description, true);
		GUILayout.EndHorizontal ();
		
		// Entries
		foreach(xARMScreenCap currScreenCap in xARMManager.AvailScreenCaps){
			if(currScreenCap.Group == group && currScreenCap.OrientationIsActive){
				DrawDefaultScreenCapEntry (currScreenCap);	
			}
			
		}
		
		EditorGUILayout.Space ();
	}
	
	
	private static void DrawColumnHeader(string text, int width, Func<xARMScreenCap, object> sortBy, bool isMinWidth = false){
		
		// fit correctly toolbar/header above entries
		int toolbarOffset = 4;
		char charSortIndicator = new char();
		
		if(lastSortedColumn == sortBy && lastSortWasAsc){
			charSortIndicator = charAsc;
		} else if(lastSortedColumn == sortBy && !lastSortWasAsc){
			charSortIndicator = charDesc;
		}
		
		if(isMinWidth){
			if(GUILayout.Button (text + " " + charSortIndicator, EditorStyles.toolbarButton, GUILayout.MinWidth (width + toolbarOffset)))
				SortAvailScreenCaps(sortBy);
		} else {
			if(GUILayout.Button (text + " " + charSortIndicator, EditorStyles.toolbarButton, GUILayout.Width (width + toolbarOffset)))
				SortAvailScreenCaps(sortBy);
		}
	}
	
	
	// Draws an default (=bultin) ScreenCap entry line
	private static void DrawDefaultScreenCapEntry (xARMScreenCap screenCap){
		
		GUILayout.BeginHorizontal ("box");
		GUILayout.Space (spacing);

		screenCap.Enabled = GUILayout.Toggle (screenCap.Enabled, "", GUILayout.Width (labelWidthEnabled));
		GUILayout.Label (screenCap.Group.ToString (), GUILayout.Width (labelWidthGroup));
		GUILayout.Label (screenCap.Name, GUILayout.Width (labelWidthName));
		GUILayout.Label (screenCap.Diagonal.ToString() + charInch, GUILayout.Width (labelWidthDiagonal));
		GUILayout.Label (screenCap.DPILabel, GUILayout.Width (labelWidthDPILabel));
		GUILayout.Label (screenCap.DPI.ToString(), GUILayout.Width (labelWidthDPI));
		GUILayout.Label (screenCap.AspectLabel, GUILayout.Width (labelWidthAspectLabel));
		GUILayout.Label (screenCap.Resolution.x + "x" + screenCap.Resolution.y + "px", GUILayout.Width (labelWidthResolution));

		if(screenCap.IsLandscape){
			GUILayout.Label (screenCap.ResolutionLabel + " " + charLandscape, GUILayout.Width (labelWidthResolutionLabel));
			GUILayout.Label ("Landscape", GUILayout.Width (labelWidthFormat));
		} else {
			GUILayout.Label (screenCap.ResolutionLabel + " " + charPortrait, GUILayout.Width (labelWidthResolutionLabel));
			GUILayout.Label ("Portrait", GUILayout.Width (labelWidthFormat));
		}

		if(screenCap.StatisticsPositioning == 999) {
			GUILayout.Label ("n/a", GUILayout.Width (labelWidthStatsPos));
		} else {
			GUILayout.Label (screenCap.StatisticsPositioning.ToString() + ".", GUILayout.Width (labelWidthStatsPos));
		}

		if(screenCap.StatisticsUsedPercent == -1f){
			GUILayout.Label ("n/a", GUILayout.Width (labelWidthStatsPerc));
		} else {
			GUILayout.Label (screenCap.StatisticsUsedPercent.ToString() + "%", GUILayout.Width (labelWidthStatsPerc));
		}

		GUILayout.Label (screenCap.Description, GUILayout.MinWidth (labelMinWidthDescription));
		
		GUILayout.EndHorizontal ();
	}
	
	private static void DrawFooter(){
		
		GUILayout.Label ("The statistics (Percent and Positioning) are related to Resolution and " +
			"are based on 'Unity Hardware Statistics 2017-01' (stats.unity3d.com). " +
			"Most other information are based on www.wikipedia.org. " +
			"Although all information were compiled with great care the accuracy of the information can not been guaranteed.", EditorStyles.wordWrappedMiniLabel);
		GUILayout.Label ("All names, brands, trademarks and registered trademarks mentioned in xARM are the property of their respective owners. " +
		                 "They are only used as some representative samples of their type.", EditorStyles.wordWrappedMiniLabel);
		GUILayout.Label ("xARM v" + xARMConfig.xARMVersion, EditorStyles.wordWrappedMiniLabel);
		
	}
	
	// helper to make foldout-labels clickable (source: http://answers.unity3d.com/questions/684414/custom-editor-foldout-doesnt-unfold-when-clicking.html)
	public static bool Foldout(bool foldout, GUIContent content, bool toggleOnLabelClick, GUIStyle style){
		Rect position = GUILayoutUtility.GetRect(40f, 40f, 16f, 16f, style);
		// EditorGUI.kNumberW == 40f but is internal
#if UNITY_3_3 || UNITY_3_4
		return EditorGUI.Foldout(position, foldout, content);
#else
		return EditorGUI.Foldout(position, foldout, content, toggleOnLabelClick, style);
#endif
	}
	public static bool Foldout(bool foldout, string content, bool toggleOnLabelClick, GUIStyle style){
		return Foldout(foldout, new GUIContent(content), toggleOnLabelClick, style);
	}

	#endregion
	
	
	private static void SortAvailScreenCaps(Func<xARMScreenCap, object> sortBy){
		
		// only sort desc if this column was sorted asc the last time/sort
		if(lastSortWasAsc && lastSortedColumn == sortBy){
			xARMManager.AvailScreenCaps = xARMManager.AvailScreenCaps.OrderByDescending (sortBy).ToList();
			lastSortWasAsc = false;
		} else {
			xARMManager.AvailScreenCaps = xARMManager.AvailScreenCaps.OrderBy (sortBy).ToList();
			lastSortWasAsc = true;
		}
		
		lastSortedColumn = sortBy;
		
	}
	
	#endregion
}
#endif