#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using xARM;


public static class xARMManager {
	
	#region Fields
	private static xARMConfig config;
	
	// Ref to the xARMProxy
	private static GameObject myxARMProxyGO;
	private static xARMProxy myxARMProxy;
	
	// Refs to xARM windows
	private static xARMPreviewWindow _myxARMPreviewWindow;
	private static xARMGalleryWindow _myxARMGalleryWindow;
	// Window states
	public static bool PreviewWindowIsAlive = false;
	public static bool GalleryWindowIsAlive = false;
	// helper
	private static double previewHeartbeat;
	private static double galleryHeartbeat;
	private static double checkedPreviewHeartbeat;
	private static double checkedGalleryHeartbeat;
	
	// Ref to GameView
	private static EditorWindow _gameView;
	public static Vector2 GameViewToolbarOffset = new Vector2(0,17);
	public static int YScrollOffset = 5;
	private static Vector2 defaultGameViewResolution;
	
	// was the list of SCs changed?
	public static bool AvailScreenCapsChanged = false;
	public static xARMScreenCap UpdatingScreenCap;

	// time of last change in editor
	private static double lastChangeInEditorTime = 0;
	private static double lastAllScreenCapsUpdatedTime = 0;

	// states
	public static bool GalleryIsUpdating = false;
	public static bool PreviewIsUpdating = false;
	public static bool ScreenCapUpdateInProgress = false; // true from trigger SC-update, wait x frames, until read SC from screen
	public static bool FinalizeScreenCapInProgress = false; // true after all SCs are updated, while GV changes to default resolution
	private  static bool skipNextUpdate = false;
	private static string currentScene = GetCurrentScene();
	public static bool HideGameViewToggle = false;



	// Delegate to run code before/after ScreenCap update
	public static OnStartScreenCapUpdateDelegate OnStartScreenCapUpdate;
	public static OnPreScreenCapUpdateDelegate OnPreScreenCapUpdate;
	public static OnPostScreenCapUpdateDelegate OnPostScreenCapUpdate;
	public static OnFinalizeScreenCapUpdateDelegate OnFinalizeScreenCapUpdate;
	
	// Delegates
	public delegate void OnStartScreenCapUpdateDelegate();
	public delegate void OnPreScreenCapUpdateDelegate();
	public delegate void OnPostScreenCapUpdateDelegate();
	public delegate void OnFinalizeScreenCapUpdateDelegate();
	#endregion
	
	#region Properties
	public static xARMConfig Config{
		get {
			if(config == null){
				config = xARMConfig.InitOrLoad ();
			}
			return config;
		}
	}
	
	public static List<xARMScreenCap> AvailScreenCaps{
		get {return Config.AvailScreenCaps;}
		set {Config.AvailScreenCaps = value;}
	}

	public static List<xARMScreenCap> ActiveScreenCaps{
		get {
			List<xARMScreenCap> activeScreenCaps = new List<xARMScreenCap>();
			
			foreach(xARMScreenCap currScreenCap in AvailScreenCaps){
				if(currScreenCap.OrientationIsActive && currScreenCap.Enabled) activeScreenCaps.Add (currScreenCap);
			}
			
			return activeScreenCaps;
		}
	}
	
	public static string[] ScreenCapList{
		get {
			string[] longNameList = new string[ActiveScreenCaps.Count];
			for(int x = 0; x < ActiveScreenCaps.Count; x++){
				longNameList[x] = ActiveScreenCaps[x].LongName;
			}
			return longNameList;
		}
	}
	
	public static xARMProxy Proxy{
		get {return myxARMProxy;}
	}
	
	public static GameObject ProxyGO{
		get {return myxARMProxyGO;}
	}
	
	public static EditorWindow GameView{
		get {
			// get Ref if necessary
			if(!_gameView){
				foreach(EditorWindow curr in Resources.FindObjectsOfTypeAll(typeof(EditorWindow))){
#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3|| UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0
					if(curr.title == "UnityEditor.GameView") _gameView = curr;
#else
					if(curr.titleContent.text == "Game") _gameView = curr;
#endif
				}
			}
			
			return _gameView;
		}
	}
		
	private static xARMPreviewWindow myxARMPreviewWindow{
		get {
			// get Ref if necessary
			if(!_myxARMPreviewWindow){
				foreach(xARMPreviewWindow curr in Resources.FindObjectsOfTypeAll(typeof(xARMPreviewWindow))){
					_myxARMPreviewWindow = curr;
				}
			}
			
			return _myxARMPreviewWindow;
		}
	}
	
	private static xARMGalleryWindow myxARMGalleryWindow{
		get {
			// get Ref if necessary
			if(!_myxARMGalleryWindow){
				foreach(xARMGalleryWindow curr in Resources.FindObjectsOfTypeAll(typeof(xARMGalleryWindow))){
					_myxARMGalleryWindow = curr;
				}
			}
			
			return _myxARMGalleryWindow;
		}
	}
	
	private static Rect currentGameViewRect{
		get {
			return GameView.position;
		}
		set {
			GameView.position = value;
		}
	}

	public static Vector2 CurrentGameViewPosition{
		get{
			return new Vector2(currentGameViewRect.xMin, currentGameViewRect.yMin);
		}
	}

	public static Vector2 DefaultGameViewResolution{
		get {
			if(xARMManager.Config.GameViewInheritsPreviewSize && defaultGameViewResolution.x >= 100 && defaultGameViewResolution.y >= 100){
				return defaultGameViewResolution;
			} else {
				return xARMManager.Config.FallbackGameViewSize;
			}
		}
		set {defaultGameViewResolution = value;}
	}

	// skip Update()-Events caused by xARM (+ GameView has focus)
	public static bool ExecuteUpdate{
		get{
			// execute this update?
			if(
				!skipNextUpdate &&
				!FinalizeScreenCapInProgress &&
				(!GalleryIsUpdating && !PreviewIsUpdating) &&
				(!GameViewHasFocus || (GameViewHasFocus && xARMManager.Config.UpdatePreviewWhileGameViewHasFocus))
				)
			{ // use update
				skipNextUpdate = false;
				return true;

			} else { // skip scene change
				skipNextUpdate = false;
				return false;
			}
		}
	}

	// used by other assets
	public static bool UpdateInProgress{
		get{
			if(GalleryIsUpdating == false && PreviewIsUpdating == false && ScreenCapUpdateInProgress == false && FinalizeScreenCapInProgress == false){
				return false;
			} else {
				return true;
			}
		}
	}

	// used to check if other assets are updating
	public static bool OtherUpdateInProgress{
		get{
			// what asset exist and is it updating?
			bool xCBMIsUpdating = GetUpdateInProgress("xCBMManager", "UpdateInProgress");

			if(xCBMIsUpdating){
				skipNextUpdate = true; // skip updates caused by other assets

				return true;
			} else {
				return false;
			}
		}

	}


	public static bool GameViewHasFocus{
		get {
			if(GameView){
				return GameView.Equals(EditorWindow.focusedWindow);
			} else {
				return false;
			}
		}
	}

	// returns Editor's current mode
	public static EditorMode CurrEditorMode{
		get {
			if(!EditorApplication.isPlaying){ // Edit
				return EditorMode.Edit;

			} else if(EditorApplication.isPlaying && EditorApplication.isPaused){ // Pause
				return EditorMode.Pause;

			} else if(EditorApplication.isPlaying && !EditorApplication.isPaused){ // Play
				return EditorMode.Play;

			} else {
				return EditorMode.Other;

			}
		}
	}
	#endregion
	
	#region Functions
	static xARMManager() {
		InitAvailScreenCaps ();
	}

	#region Init
	private static void InitAvailScreenCaps(){
		
		// List of all default Resolutions/Aspect Ratios (ScreenCaps)
		// iOS
		addOrUpdateScreenCap(new xARMScreenCap("iPhone", 		new Vector2(1136, 640), "WDVGA","16:9~", 	4.0f, 326, "@2x", 	xARMScreenCapGroup.iOS, 1, 29.6f, "iPhone SE, 5S, 5C, 5 & iPod touch 6., 5."));
		addOrUpdateScreenCap(new xARMScreenCap("iPad", 			new Vector2(2048, 1536), "QXGA","", 		9.7f, 264, "@2x", 	xARMScreenCapGroup.iOS, 2, 20.7f, "iPad 4., 3., Pro (9.7), Air 2, Air"));
		addOrUpdateScreenCap(new xARMScreenCap("iPad mini", 	new Vector2(2048, 1536), "QXGA","", 		7.9f, 326, "@2x", 	xARMScreenCapGroup.iOS, 2, 20.7f, "iPad mini 4, mini 3, mini 2"));
		addOrUpdateScreenCap(new xARMScreenCap("iPhone", 		new Vector2(1334, 750), "custom", "16:9~", 	4.7f, 326, "@2x", 	xARMScreenCapGroup.iOS, 3, 20.1f, "iPhone 7, 6S, 6"));
		addOrUpdateScreenCap(new xARMScreenCap("iPad", 			new Vector2(1024, 768), "XGA",	"", 		9.7f, 132, "@1x", 	xARMScreenCapGroup.iOS, 4, 11.4f, "iPad 2., 1."));
		addOrUpdateScreenCap(new xARMScreenCap("iPad mini", 	new Vector2(1024, 768), "XGA",	"", 		7.9f, 163, "@1x", 	xARMScreenCapGroup.iOS, 4, 11.4f, "iPad mini"));
		addOrUpdateScreenCap(new xARMScreenCap("iPhone", 		new Vector2(1920, 1080), "FHD", "", 		5.5f, 401, "@2.6x",	xARMScreenCapGroup.iOS, 5, 10.3f, "iPhone 7 Plus, 6S Plus, 6 Plus (Unity uses native resolution)"));
		addOrUpdateScreenCap(new xARMScreenCap("iPhone", 		new Vector2(960, 640), "DVGA", 	"", 		3.5f, 326, "@2x", 	xARMScreenCapGroup.iOS, 6,  5.3f, "iPhone 4S, 4 & iPod touch 4."));
		addOrUpdateScreenCap(new xARMScreenCap("iPad Pro",	 	new Vector2(2732, 2048), "custom", "4:3~", 12.9f, 264, "@2x", 	xARMScreenCapGroup.iOS, 7,  0.1f, "iPad Pro (12.9)"), new xARMScreenCap(new Vector2(2732, 2048), 12.22f, xARMScreenCapGroup.iOS)); 
		addOrUpdateScreenCap(new xARMScreenCap("iPhone", 		new Vector2(480, 320), "HVGA",	"",			3.5f, 163, "@1x", 	xARMScreenCapGroup.iOS, 8,    0f, "iPhone 3GS, 3G, 1 & iPod touch 3., 2., 1."));
		addOrUpdateScreenCap(new xARMScreenCap("iPhone", 		new Vector2(2208, 1242), "custom", "", 		5.5f,   0, "@3x", 	xARMScreenCapGroup.iOS, 999, -1f, "Probable future iPhone resolution"));

		// Android 
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1280, 720), "HD", "", 			4.7f, 0, "", 			xARMScreenCapGroup.Android,  1, 28.9f, "Galaxy Nexus, Fire Phone, Galaxy Note 2 (5.5), ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1920, 1080), "FHD","", 			5.0f, 0, "", 			xARMScreenCapGroup.Android,  2, 21.4f, "Nexus 5X (5.2), 5 (4.96), Galaxy S5 (5.1), S4 (5), ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(800, 480), "WVGA", "", 			3.7f, 0, "", 			xARMScreenCapGroup.Android,  3, 10.3f, "Nexus S (4), Nexus One (3.7), Galaxy S2 (4.3), ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(854, 480), "FWVGA","16:9~",		4.0f, 0, "", 			xARMScreenCapGroup.Android,  4,  9.7f, "Xperia Play, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(960, 540), "qHD","",	 		4.3f, 0, "", 			xARMScreenCapGroup.Android,  5,  8.9f, "Sensation, Droid Razr, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1024, 600), "WSVGA","5:3~",	 	7.0f, 0, "", 			xARMScreenCapGroup.Android,  6,  7.8f, "Galaxy Tab, Kindle Fire, Nook Tablet, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1280, 800), "WXGA", "", 		7.0f, 0, "", 			xARMScreenCapGroup.Android,  7,  5.0f, "Nexus 7, Kindle Fire HD, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1280, 800), "WXGA","", 		   10.1f, 0, "", 			xARMScreenCapGroup.Android,  7,  5.0f, "Galaxy Tab 2 10.1, Galaxy Tab 10.1, Galaxy Note 10.1, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(2560, 1440), "QHD","",	 		6.0f, 0, "", 			xARMScreenCapGroup.Android,  8,  2.4f, "Nexus 6P (5.7), 6 (5.96), Galaxy Note 4 (5.7)..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(480, 320), "HVGA","",	 		3.2f, 0, "", 			xARMScreenCapGroup.Android,  9,  1.2f, "Galaxy, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1024, 768), "XGA","", 			8.0f, 0, "", 			xARMScreenCapGroup.Android, 10,  0.8f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1024, 768), "XGA","", 	       10.0f, 0, "", 			xARMScreenCapGroup.Android, 10,  0.8f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1920, 1200), "WUXGA","", 		8.9f, 0, "", 			xARMScreenCapGroup.Android, 11,  0.8f, "Kindle Fire HD 8.9, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1920, 1200), "WUXGA","", 		7.0f, 0, "", 			xARMScreenCapGroup.Android, 11,  0.8f, "Nexus 7 2., ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(320, 240), "QVGA","", 			3.1f, 0, "", 			xARMScreenCapGroup.Android, 12,  0.4f, "Galaxy Mini, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(2560, 1600), "WQXGA","",  	   10.1f, 0, "", 			xARMScreenCapGroup.Android, 13,  0.2f, "Nexus 10, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(800, 600), "SVGA","", 			7.0f, 0, "", 			xARMScreenCapGroup.Android, 14,  0.2f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(2048, 1536), "QXGA","", 		8.9f, 0, "", 			xARMScreenCapGroup.Android, 15,  0.2f, "Nexus 9..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(2048, 1536), "QXGA","", 		9.7f, 0, "", 			xARMScreenCapGroup.Android, 15,  0.2f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1024, 576), "WSVGA","",  	   10.0f, 0, "", 			xARMScreenCapGroup.Android, 16,  0.2f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1280, 768), "WXGA","", 			4.7f, 0, "", 			xARMScreenCapGroup.Android, 17,  0.1f, "Nexus 4, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1600, 900), "HD+","", 	 	   13.0f, 0, "", 			xARMScreenCapGroup.Android, 18,  0.1f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1440, 900), "WXGA+","", 		7.0f, 0, "", 			xARMScreenCapGroup.Android, 19,  0.0f, "Nook HD, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1800, 1080), "custom","", 		5.1f, 0, "", 			xARMScreenCapGroup.Android, 20,  0.0f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1920, 1280), "custom","", 		9.0f, 0, "", 			xARMScreenCapGroup.Android, 21,  0.0f, "Nook HD+, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(640, 360), "nHD","", 			3.5f, 0, "", 			xARMScreenCapGroup.Android,999,   -1f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1066, 600), "custom","", 		7.6f, 0, "", 			xARMScreenCapGroup.Android,999,   -1f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(960, 640), "DVGA","", 			4.0f, 0, "", 			xARMScreenCapGroup.Android,999,   -1f, "..."));

		// Windows Phone 8
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(800, 480), "WVGA","", 			4.0f, 0, "",			xARMScreenCapGroup.WinPhone8, 1, 54.6f, "Lumia 620 (3.8), 520, 525 (4.0), 720, 820 (4.3), 625 (4.7), ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1280, 720), "HD","", 			4.8f, 0, "",			xARMScreenCapGroup.WinPhone8, 2, 23.3f, "ATIV S, S Neo, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1280, 720), "HD","", 			6.0f, 0, "",			xARMScreenCapGroup.WinPhone8, 2, 23.3f, "Lumia 1320, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(854, 480), "FWVGA","16:9~", 	4.5f, 0, "",			xARMScreenCapGroup.WinPhone8, 3, 12.2f, "Lumia 630, 635, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(960, 540), "qHD","",	 		5.0f, 0, "", 			xARMScreenCapGroup.WinPhone8, 4,  4.1f, "Lumia 535, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1280, 768), "WXGA","", 			4.5f, 0, "",			xARMScreenCapGroup.WinPhone8, 5,  3.7f, "Lumia 920, 925, 928, 1020, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1920, 1080), "FHD","", 			5.0f, 0, "",			xARMScreenCapGroup.WinPhone8, 6,  1.8f, "Lumia Icon, 930, ATIV SE, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1920, 1080), "FHD","", 			6.0f, 0, "",			xARMScreenCapGroup.WinPhone8, 6,  1.8f, "Lumia 1520, ..."));


		// WinRT
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(800, 480), "WVGA", "", 			4.0f, 0, "", 			xARMScreenCapGroup.WindowsRT, 1, 21.6f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1280, 720), "HD", "", 			5.0f, 0, "", 			xARMScreenCapGroup.WindowsRT, 2, 14.2f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(960, 540), "qHD","",	 		4.0f, 0, "", 			xARMScreenCapGroup.WindowsRT, 3,  5.3f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1280, 768), "WXGA","", 			5.0f, 0, "",			xARMScreenCapGroup.WindowsRT, 4,  2.0f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1920, 1080), "FHD","", 		   10.6f, 0, "", 			xARMScreenCapGroup.WindowsRT, 5,  1.6f, "Surface 2 (10.6), Lumia 2520 (10.1), ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1366, 768), "WXGA","16:9~",    10.6f, 0, "", 			xARMScreenCapGroup.WindowsRT, 6,  0.7f, "Surface (10.6), ATIV Tab (10.1), ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1366, 768), "WXGA","16:9~",    11.6f, 0, "", 			xARMScreenCapGroup.WindowsRT, 6,  0.7f, "IdeaPad Yoga 11, ..."));
		// WinRT stats of 2017-01 include strange resolutions like 1x1 (39.4%), 2x2 (11.2%), ...

		
		// Standalone
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1920, 1080), "FHD","", 			  40, 0, "", 			xARMScreenCapGroup.Standalone,  1,44.6f, "TVs, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1920, 1080), "FHD","", 			  24, 0, "", 			xARMScreenCapGroup.Standalone,  1,44.6f, "Desktop Monitors, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1366, 768), "WXGA","16:9~", 	  14, 0, "", 			xARMScreenCapGroup.Standalone,  2,10.2f, "Notebooks, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1440, 900), "WXGA+","", 		  19, 0, "", 			xARMScreenCapGroup.Standalone,  3, 4.4f, "Desktop Monitors, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1600, 900), "HD+","", 			  15, 0, "", 			xARMScreenCapGroup.Standalone,  4, 3.3f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1366, 696), "custom","", 		  14, 0, "", 			xARMScreenCapGroup.Standalone,  5, 3.3f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1024, 768), "XGA","", 			  14, 0, "", 			xARMScreenCapGroup.Standalone,  6, 3.1f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1280, 800), "WXGA","", 			  14, 0, "", 			xARMScreenCapGroup.Standalone,  7, 2.4f, "Notebooks, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1280, 1024), "SXGA","", 		  17, 0, "", 			xARMScreenCapGroup.Standalone,  8, 1.7f, "Desktop Monitors, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(800, 600), "SVGA","", 			  14, 0, "", 			xARMScreenCapGroup.Standalone,  9, 1.6f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1680, 1050), "WSXGA+","", 		  15, 0, "", 			xARMScreenCapGroup.Standalone, 10, 1.5f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1200, 900), "custom","", 		  14, 0, "", 			xARMScreenCapGroup.Standalone, 11, 1.1f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1360, 768), "WXGA","16:9~", 	  14, 0, "", 			xARMScreenCapGroup.Standalone, 12, 1.0f, "Notebooks, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1152, 864), "XGA+","", 			  17, 0, "", 			xARMScreenCapGroup.Standalone, 13, 0.9f, "Desktop Monitors, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(2560, 1440), "QHD","", 			  27, 0, "", 			xARMScreenCapGroup.Standalone, 14, 0.7f, "Desktop Monitors, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1280, 768), "WXGA","", 			  14, 0, "", 			xARMScreenCapGroup.Standalone, 15, 0.5f, "Notebooks, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1280, 960), "SXGA-","", 		  17, 0, "", 			xARMScreenCapGroup.Standalone, 16, 0.5f, "..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1920, 1200), "WUXGA","", 		  24, 0, "", 			xARMScreenCapGroup.Standalone, 17, 0.3f, "Desktop Monitors, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1280, 720), "HD","", 			  32, 0, "", 			xARMScreenCapGroup.Standalone, 18, 0.3f, "TVs, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(2560, 1080), "UW-UXGA","21:9~",   29, 0, "", 			xARMScreenCapGroup.Standalone, 19, 0.2f, "Desktop Monitors, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1024, 600), "WSVGA","16:9~", 	  10, 0, "", 			xARMScreenCapGroup.Standalone, 20, 0.1f, "Netbooks, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(1600, 1200), "UXGA","", 		  20, 0, "", 			xARMScreenCapGroup.Standalone,999,  -1f, "Desktop Monitors, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(3440, 1440), "UWQHD","21:9~", 	  34, 0, "", 			xARMScreenCapGroup.Standalone,999,  -1f, "Desktop Monitors, ..."));
		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(3840, 2160), "4K UHD-1","", 	  55, 0, "", 			xARMScreenCapGroup.Standalone,999,  -1f, "Desktop Monitors, ..."));
		// Resolutions this great aren't supported by the Game View
//		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(5120, 2160), "5K","21:9~", 		  80, 0, "", 			xARMScreenCapGroup.Standalone,999,  -1f, "Ultra wide TVs, ..."));
//		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(5120, 2880), "5K UHD+","", 		  27, 0, "", 			xARMScreenCapGroup.Standalone,999,  -1f, "Desktop Monitors, ..."));
//		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(7680, 4320), "8K UHD-2","", 	  90, 0, "", 			xARMScreenCapGroup.Standalone,999,  -1f, "..."));
//		addOrUpdateScreenCap(new xARMScreenCap("", 			new Vector2(8192, 4608), "8K","", 			 100, 0, "", 			xARMScreenCapGroup.Standalone,999,  -1f, "..."));
		// Standalone stats of 2017-01 includes strange resolutions of 1x1 (5.5%)


		// Custom
		/* How to add custom ScreenCaps:
		 * Every line adds one (landscape) ScreenCap. Portaits are created automatically.
		 * DPI, etc. are set automatically if not set manually.
		 * Use "xARMScreenCapGroup.Custom" to put new SreenCaps into the Custom categorie in Options.
		 * Don't change existing lines and ensure to make a backup of your added lines. After updating xARM the additons need to be inserted again.
		 * 
		 * Example (1920x1080 Full-HD 42"):
		 */
		// addOrUpdateScreenCap (new xARMScreenCap("", new Vector2(1920, 1080), "","", 42, 0, "", xARMScreenCapGroup.Custom, 999, -1f, "Description"));

		// create portrait ScreenCaps
		List<xARMScreenCap> portraitScreenCaps = new List<xARMScreenCap>();
		foreach(xARMScreenCap currScreenCap in AvailScreenCaps){
			if(currScreenCap.Group != xARMScreenCapGroup.Standalone && currScreenCap.IsLandscape && currScreenCap.IsBaseRes) portraitScreenCaps.Add (currScreenCap.Clone (true));
		}
		// add
		foreach(xARMScreenCap currScreenCap in portraitScreenCaps){
			addOrUpdateScreenCap (currScreenCap);
		}
		portraitScreenCaps.Clear();
		

		// create Android ScreenCaps with navigation/system bar offset
		List<xARMScreenCap[]> navigationBarScreenCaps = new List<xARMScreenCap[]>();
		foreach(xARMScreenCap currScreenCap in AvailScreenCaps){
			if(currScreenCap.Group == xARMScreenCapGroup.Android && currScreenCap.IsBaseRes)
				
				navigationBarScreenCaps.Add (currScreenCap.CreateNavigationBarVersion ());
		}
		// add
		foreach(xARMScreenCap[] currScreenCap in navigationBarScreenCaps){
			addOrReplaceScreenCap (currScreenCap[0], currScreenCap[1]); //
		}
		navigationBarScreenCaps.Clear ();

		// mark loaded SC list as unchanged to prevent resave
		xARMScreenCap.ListChanged = false;
	}
	
	private static void addOrUpdateScreenCap (xARMScreenCap screenCapToAdd, xARMScreenCap screenCapToReplace = null){
		int screenCapIndex;

		// replace existing SC
		if(screenCapToReplace is xARMScreenCap){
			// find SC to replace
			screenCapIndex = getScreenCapIndex (screenCapToReplace);
			// no SC to replace > find SC to update
			if(screenCapIndex == -1) screenCapIndex = getScreenCapIndex (screenCapToAdd);

		} else {
			screenCapIndex = getScreenCapIndex (screenCapToAdd);
		}

		if(screenCapIndex >= 0){ // update (don't add duplicates)
			// values to keep
			bool origEnabledState = AvailScreenCaps[screenCapIndex].Enabled;
			
			AvailScreenCaps[screenCapIndex] = screenCapToAdd;
			AvailScreenCaps[screenCapIndex].Enabled = origEnabledState;
			
		} else { // add
			AvailScreenCaps.Add (screenCapToAdd);
		}
	}

	private static void addOrReplaceScreenCap (xARMScreenCap screenCapToAdd, xARMScreenCap screenCapToReplace){
		int screenCapIndexToReplace = getScreenCapIndex (screenCapToReplace);
		int screenCapIndexToAdd = getScreenCapIndex (screenCapToAdd);
		
		if(screenCapIndexToReplace >= 0){ // replace
			// values to keep
			bool origEnabledState = AvailScreenCaps[screenCapIndexToReplace].Enabled;
			
			AvailScreenCaps[screenCapIndexToReplace] = screenCapToAdd;
			AvailScreenCaps[screenCapIndexToReplace].Enabled = origEnabledState;
			
		} else if(screenCapIndexToAdd >= 0){ // update (don't add duplicates)
			// values to keep
			bool origEnabledState = AvailScreenCaps[screenCapIndexToAdd].Enabled;
			
			AvailScreenCaps[screenCapIndexToAdd] = screenCapToAdd;
			AvailScreenCaps[screenCapIndexToAdd].Enabled = origEnabledState;

		} else { // add
			AvailScreenCaps.Add (screenCapToAdd);
		}
	}
	
	private static int getScreenCapIndex(xARMScreenCap screenCapToCheck){
		for (int x= 0; x< AvailScreenCaps.Count; x++){
			if(AvailScreenCaps[x] == screenCapToCheck) return x;
		}
		
		return -1;
	}
	#endregion
	
	#region Proxy
	public static void CreateProxyGO (){
		// create Proxy only if needed and not while switching to play mode 
		if(!myxARMProxyGO && (EditorApplication.isPlaying || !EditorApplication.isPlayingOrWillChangePlaymode)){
						
			// create GO with components attached
			myxARMProxyGO = new GameObject("xARMProxy");
			myxARMProxy = myxARMProxyGO.AddComponent<xARMProxy> ();
			if(xARMManager.Config.IntegrationUGUIPhySize) myxARMProxyGO.AddComponent<xARMDelegateUGUI> ();

			
			if(!myxARMProxy){
				RemoveProxyGO ();
				xARMPreviewWindow.WarningBoxText = "Could not create xARMProxy. Do NOT put xARM in the Editor folder.";
				xARMGalleryWindow.WarningBoxText = "Could not create xARMProxy. Do NOT put xARM in the Editor folder.";
			}
		}
	}
	
	public static void RemoveProxyGO (){
		MonoBehaviour.DestroyImmediate (myxARMProxyGO);
	}

	public static void RecreateProxyGO (){
		RemoveProxyGO();
		CreateProxyGO();
	}

	// reset xARM values on scene switch
	public static void ResetOnSceneSwitch(){
		if (currentScene != GetCurrentScene()){
			currentScene = GetCurrentScene();
			// reset
			ScreenCapUpdateInProgress = false;
			SceneChanged ();
		}
	}
	#endregion
	
	#region ScreenCaps
	public static bool IsToUpdate(xARMScreenCap screenCap){
		if(screenCap.LastUpdateTime != lastChangeInEditorTime && screenCap.LastUpdateTryTime != lastChangeInEditorTime){
			return true;
		} else {
			return false;
		}
	}
	

	public static void UpdateScreenCap(xARMScreenCap screenCap){
		// make current ScreenCap available to delegates
		UpdatingScreenCap = screenCap;

		ResizeGameView (screenCap.Resolution);

		// run custom code
		if(OnPreScreenCapUpdate != null) OnPreScreenCapUpdate ();

		xARMManager.ScreenCapUpdateInProgress = true;

		// wait x frames to ensure correct results with other (lazy) plugins
		Proxy.StartWaitXFramesCoroutine (screenCap, xARMManager.Config.FramesToWait);
	}


	public static void UpdateScreenCapAtEOF(xARMScreenCap screenCap){
		// capture Render at EndOfFrame
		Proxy.StartUpdateScreenCapCoroutine (screenCap);
		// force EndOfFrame - to execute yield
		GameView.Repaint ();
	}


	public static void ReadScreenCapFromGameView(xARMScreenCap screenCap){

		int width = (int)screenCap.Resolution.x;
		int height = (int)screenCap.Resolution.y;

		// check if the GameView has the correct size
		if(screenCap.Resolution.x == Screen.width && screenCap.Resolution.y == Screen.height){ 
			// read screen to Tex2D
			Texture2D screenTex = new Texture2D(width, height, TextureFormat.RGB24, false);
			screenTex.ReadPixels (new Rect(0, 0, width, height), 0, 0, false);
			screenTex.Apply (false, false); // readable to enable export as file

			// update ScreenCap
			screenCap.Texture = screenTex;
			screenCap.LastUpdateTime = lastChangeInEditorTime;
			screenCap.LastUpdateTryTime = lastChangeInEditorTime;
			screenCap.UpdatedSuccessful = true;
			
			// repaint editor windows
			if(myxARMPreviewWindow) myxARMPreviewWindow.Repaint ();
			if(myxARMGalleryWindow) myxARMGalleryWindow.Repaint ();
			
		} else {
			// mark this ScreenCap as not updated and display message
			screenCap.LastUpdateTryTime = lastChangeInEditorTime;
			screenCap.UpdatedSuccessful = false;

#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3|| UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3
			xARMGalleryWindow.WarningBoxText = "Could not update all ScreenCaps. Switch 'GameView' to 'Free Aspect'.";
			xARMPreviewWindow.WarningBoxText = "Could not update all ScreenCaps. Switch 'GameView' to 'Free Aspect'.";

#else	// 5.4+
			xARMGalleryWindow.WarningBoxText = "Could not update all ScreenCaps. Switch 'GameView' to 'Free Aspect' and (de)activate Retina/HiDPI mode in Options.";
			xARMPreviewWindow.WarningBoxText = "Could not update all ScreenCaps. Switch 'GameView' to 'Free Aspect' and (de)activate Retina/HiDPI mode in Options.";

#endif
		}
		
		// run custom code
		if(OnPostScreenCapUpdate != null) OnPostScreenCapUpdate ();

		UpdatingScreenCap = null;
	}
	
	public static void FinalizeScreenCapUpdate(){
		// unload unused ScreenCaps
		Resources.UnloadUnusedAssets ();

		// set default GameView size
		ResizeGameViewToDefault ();

		// LiveUpdate - remove help message if it's not longer relevant
		if(AllScreenCapsUpdatedSuccesfull()){
			xARMPreviewWindow.WarningBoxText = "";
			xARMGalleryWindow.WarningBoxText = "";
		}
		
		// run custom code
		if(OnFinalizeScreenCapUpdate != null) OnFinalizeScreenCapUpdate ();
	}

	private static void AbortUpdatePreview(){
		AbortUpdate(ref PreviewIsUpdating);
	}

	private static void AbortUpdateGallery(){
		AbortUpdate(ref GalleryIsUpdating);
	}

	private static void AbortUpdate(ref bool windowIsUpdating){
		// rest all states
		windowIsUpdating = false;
		ScreenCapUpdateInProgress = false;
		FinalizeScreenCapUpdate ();
		FinalizeScreenCapInProgress = false;
	}

	public static bool AllScreenCapsUpdatedRecently(){
		// no recent scene change?
		if(lastAllScreenCapsUpdatedTime == lastChangeInEditorTime) return false;
		
		// update still in progress?
		foreach(xARMScreenCap currScreenCap in ActiveScreenCaps){
			if(currScreenCap.LastUpdateTryTime != lastChangeInEditorTime) return false;
		}
		
		return true;
	}

	public static void SetAllScreenCapsUpdated(){
		lastAllScreenCapsUpdatedTime = lastChangeInEditorTime;
	}
	
	private static bool AllScreenCapsUpdatedSuccesfull(){
		foreach(xARMScreenCap currScreenCap in ActiveScreenCaps){
			if(!currScreenCap.UpdatedSuccessful) return false;
		}
		return true;
	}
	
	#endregion
	
	#region GameView
	// update Game View position
	public static void UpdateGameView(){
		ResizeGameView (new Vector2(Screen.width, Screen.height), true); // force update

		GameView.Focus ();
		skipNextUpdate = true;
	}

	public static void SwitchHideGameView(){
		HideGameViewToggle = !HideGameViewToggle;
		
		Config.HideGameView = HideGameViewToggle;
		UpdateGameView ();
		
		// repaint editor windows
		if(myxARMPreviewWindow) myxARMPreviewWindow.Repaint ();
		if(myxARMGalleryWindow) myxARMGalleryWindow.Repaint ();
	}


	private static void ResizeGameView(Vector2 newSize, bool force = false){
		// get current Game View resolution
		Vector2 screen = new Vector2(Screen.width, Screen.height);

		// on HiDPI everything is halfed
		if(xARMManager.Config.EditorUsesHIDPI){
			newSize.x /= 2;
			newSize.y /= 2;

			// use Game View window size, because screen.x/y can also be another active EditorWindow
			// screen.x /= 2;
			// screen.y /= 2;
			screen.x = currentGameViewRect.width;
			screen.y = currentGameViewRect.height;
			screen -= GameViewToolbarOffset;

		}

		if(!GameView) OpenGameView();

		// ensure current GV position value is correct
		SaveGameViewPosition ();

		// force is used to change the GV position without changing its resolution/size
		if(force){
			// --- FloatingGameView() ---
			Vector2 gameViewPos;
			
			// select target GV position
			if(Config.HideGameView){
				gameViewPos = Config.HiddenGameViewPosition;
			} else {
				gameViewPos = Config.GameViewPosition;
			}

			int x = Mathf.RoundToInt(gameViewPos.x);
			int y = Mathf.RoundToInt(gameViewPos.y);

			int width = Mathf.RoundToInt(currentGameViewRect.width);
			int height = Mathf.RoundToInt(currentGameViewRect.height);
			
			if(!GameView) OpenGameView();
			
			// undock GameView and set default size (doesn't work 100%)
			currentGameViewRect = new Rect(x, y, width, height);

			// skip update caused by this
			skipNextUpdate = true;
		}
		// if not forced resize only on resolution changes
		else if(newSize.x != screen.x || newSize.y != screen.y){ 
			
//			xARMProxy.DebugLog("resize! " + screen.ToString());

			// save original values
			Vector2 prevMinSize, prevMaxSize;
			prevMinSize = GameView.minSize;
			prevMaxSize = GameView.maxSize;

			// undock and resize
			FloatingGameView (newSize);
			
			// add toolbar offset
			Vector2 newSizeWithOffset = newSize + GameViewToolbarOffset;

			// ensure resize
			GameView.minSize = newSizeWithOffset;
			GameView.maxSize = newSizeWithOffset;
			
			// restore previous values (keeps GV resizable)
			GameView.minSize = prevMinSize;
			GameView.maxSize = prevMaxSize;

			// skip update caused by this
			skipNextUpdate = true;
		}
	}

	public static void ResizeGameViewToDefault(){
		ResizeGameView (xARMManager.DefaultGameViewResolution);
	}

	// ensures there is a free floating GameView window
	public static void FloatingGameView(Vector2 size){
		Vector2 gameViewPos;

		// select target GV position
		if(Config.HideGameView){
			gameViewPos = Config.HiddenGameViewPosition;
		} else {
			gameViewPos = Config.GameViewPosition;
		}

		// add offset
		size += GameViewToolbarOffset;
		
		int width = Mathf.RoundToInt(size.x);
		int height = Mathf.RoundToInt(size.y);
		int x = Mathf.RoundToInt(gameViewPos.x);
		int y = Mathf.RoundToInt(gameViewPos.y);

		if(!GameView) OpenGameView();

		// undock GameView and set default size (doesn't work 100%)
		currentGameViewRect = new Rect(x, y, width, height);
	} 

	// save GameView position when focused and not hidden
	public static void SaveGameViewPosition(){

		if(GameViewHasFocus && Config.AutoTraceGameViewPosition && !Config.HideGameView){ // trace GV position

			// workaround to not save some positions
			if(
				// workaround: 0,12 is the position while switching edit<->play mode
				CurrentGameViewPosition != new Vector2(0f, 12f) && 
				CurrentGameViewPosition != new Vector2(0f, 12f + YScrollOffset) && 
				// workaround: assume odd y-scrolling, if Game View has only moved y+yScrollOffset
				CurrentGameViewPosition != new Vector2(Config.GameViewPosition.x, Config.GameViewPosition.y + YScrollOffset) &&
				// workaround: hide GV position (fast repeated clicking on "Hide GV")
				CurrentGameViewPosition != new Vector2(Config.HiddenGameViewPosition.x, Config.HiddenGameViewPosition.y) &&
				CurrentGameViewPosition != new Vector2(Config.HiddenGameViewPosition.x, Config.HiddenGameViewPosition.y + YScrollOffset) 
			){
				// save position
				Config.GameViewPosition = CurrentGameViewPosition;
			}
		} 
		else if(!Config.AutoTraceGameViewPosition && !Config.HideGameView) { // use fixed GV position
			Config.GameViewPosition = Config.FixedGameViewPosition;
		}
	}

	public static void EnsureNextFrame (){
		// in Editor mode - fake scene change
		if(xARMManager.CurrEditorMode == EditorMode.Edit){
			if(Proxy){
				// add random rotation to fake scene change (Proxy-HideFlag has to be None)
				Proxy.gameObject.transform.rotation = Random.rotation;
			}
		}

		// in Play mode - frames are rolling by

		// in Pause mode - Step
		if(xARMManager.CurrEditorMode == EditorMode.Pause){
			EditorApplication.Step ();
		}
	}

	private static string GetCurrentScene(){
#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3|| UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
		return EditorApplication.currentScene; 
#else
		return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
#endif

	}


	public static void SceneChanged(){
		lastChangeInEditorTime = EditorApplication.timeSinceStartup;
	}

	private static void OpenGameView(){
		EditorApplication.ExecuteMenuItem ("Window/Game");
	}
	#endregion

	#region Preview and Gallery tools
	public static void RepaintAllWindows (){
		xARMPreviewWindow.RepaintNextUpdate = true;
		xARMGalleryWindow.RepaintNextUpdate = true;
	}


	public static void SendHeartbeatPreview(){
		// save heartbeat
		previewHeartbeat = EditorApplication.timeSinceStartup;
	}

	public static void SendHeartbeatGallery(){
		// save heartbeat
		galleryHeartbeat = EditorApplication.timeSinceStartup;

	}


	//check if a window was deactivated (e.g. tab is in background)
	public static void CheckHeartbeat(){
		// time of last heartbeat unchanged & was alive last time = just got missing
		if(previewHeartbeat == checkedPreviewHeartbeat && PreviewWindowIsAlive){
			PreviewWindowIsAlive = false;
			AbortUpdatePreview();

		} else if(previewHeartbeat != checkedPreviewHeartbeat) {
			PreviewWindowIsAlive = true;
			checkedPreviewHeartbeat = previewHeartbeat;
		}

		if(galleryHeartbeat == checkedGalleryHeartbeat && GalleryWindowIsAlive){
			GalleryWindowIsAlive = false;
			AbortUpdateGallery();

		} else if(galleryHeartbeat != checkedGalleryHeartbeat) {
			GalleryWindowIsAlive = true;
			checkedGalleryHeartbeat = galleryHeartbeat;
		}
	}


	// save one SC as file
	public static void SaveScreenCapFile (){
		xARMScreenCap screenCap = xARMPreviewWindow.SelectedScreenCap;

		if(screenCap.Texture.width != 4){ // not placeholder
			string defaultFileName = screenCap.Name + " " + screenCap.Diagonal + " " +screenCap.DPILabel + " " + screenCap.Resolution.x + "x" + screenCap.Resolution.y  + ".png";
			// open export to file panel
			string exportFilePath = EditorUtility.SaveFilePanel ("Export ScreenCap as PNG", xARMManager.Config.ExportPath, defaultFileName, "png");

			// export
			if(exportFilePath.Length > 0) ExportScreenCapToFile (screenCap, exportFilePath);

		} else {
			Debug.LogWarning ("xARM: ScreenCap not exported. Please update it before export.");
		}

	}

	// save all SCs as files
	public static void SaveAllScreenCapFiles (){
		// open export to folder panel
		string exportFolderPath = EditorUtility.SaveFolderPanel ("Export all ScreenCaps as PNGs (overwrites existing files)", xARMManager.Config.ExportPath, ".png");

		if(exportFolderPath.Length > 0){
			// export all SCs
			foreach(xARMScreenCap currScreenCap in ActiveScreenCaps){
				if(currScreenCap.Texture.width != 4){ // not placeholder
					string fileName = currScreenCap.Name + " " + currScreenCap.Diagonal + " " + currScreenCap.DPILabel + " " + currScreenCap.Resolution.x + "x" + currScreenCap.Resolution.y + ".png";

					// export
					ExportScreenCapToFile (currScreenCap, exportFolderPath + "/" + fileName);

				} else {
					Debug.LogWarning ("xARM: ScreenCap not exported. Please update it before export.");
				}
			}
		}
	}

	// export ScreenCap as PNG file
	private static void ExportScreenCapToFile (xARMScreenCap screenCap, string path){
		FileStream fs = new FileStream(path, FileMode.Create);
		BinaryWriter bw = new BinaryWriter(fs);
		bw.Write (screenCap.Texture.EncodeToPNG ());
		bw.Close ();
		fs.Close ();
	}

	public static void PreviewNextScreenCap (){
		if(myxARMPreviewWindow) xARMPreviewWindow.SelectNextScreencap();
	}

	public static void PreviewPreviousScreenCap (){
		if(myxARMPreviewWindow) xARMPreviewWindow.SelectPreviousScreencap();
	}

	#endregion

	#region Other assets
	// get state of other asset
	private static bool GetUpdateInProgress(string reflectType, string reflectProperty){

		System.Type type = System.Type.GetType(reflectType);
		if(type == null) return false;

		System.Reflection.PropertyInfo property = type.GetProperty(reflectProperty);
		if(property == null) return false;

		return (bool)property.GetValue(type, null);
	}

	// abort other assets update
	private static void AbortOtherUpdate(string reflectType, string reflectMethod){

		System.Type type = System.Type.GetType(reflectType);
		if(type != null){
			System.Reflection.MethodInfo method = type.GetMethod(reflectMethod);
			if(method != null){
				method.Invoke(new object[]{}, null);
			}
		}
	}
	#endregion

	#endregion
}
#endif