#if UNITY_EDITOR
using UnityEngine;
#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3|| UNITY_4_5
// no uGUI available
#else
using UnityEngine.UI;
#endif
using xARM;

[ExecuteInEditMode]
public class xARMDelegateUGUI : MonoBehaviour {
#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3|| UNITY_4_5
	// no uGUI available
#else

	private static CanvasScaler[] allCanvasScaler;

	// hook delegates
	void OnEnable () {
		xARMManager.OnStartScreenCapUpdate += StartUpdate;
		xARMManager.OnPreScreenCapUpdate += PreUpdate;
		xARMManager.OnFinalizeScreenCapUpdate += FinUpdate;
	}
	
	// unhook delegates
	void OnDisable () {
		xARMManager.OnStartScreenCapUpdate -= StartUpdate;
		xARMManager.OnPreScreenCapUpdate -= PreUpdate;
		xARMManager.OnFinalizeScreenCapUpdate -= FinUpdate;

		// ensure to reset to uGUI's default settings
		ResetDPI();
	}

	void StartUpdate (){
		GetAllCanvasScaler ();
	}

	private void PreUpdate (){
		// Set DPI of ScreenCap
		SetDPI(xARMManager.UpdatingScreenCap.DPI);
	}

	private void FinUpdate (){
		if(xARMManager.Config.IntegrationUGUIPhySizeKeepDPI){
			// ensure DPI of Preview's SC is set
			if(xARMPreviewWindow.SelectedScreenCap is xARMScreenCap) SetDPI(xARMPreviewWindow.SelectedScreenCap.DPI);
		} else {
			ResetDPI();
		}
	}

	private void GetAllCanvasScaler (){
		allCanvasScaler = FindObjectsOfType<CanvasScaler>();
	}

	private void SetDPI(float dpi){
		if(allCanvasScaler == null) GetAllCanvasScaler();

		foreach(CanvasScaler scaler in allCanvasScaler){
			if(scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPhysicalSize) HandleConstantPhysicalSize(scaler, dpi);
		}
	}

	private void ResetDPI(){
		GetAllCanvasScaler(); // ensure to have an up to date list for Edit->Play switch
		SetDPI(Screen.dpi);
	}

	#region based on uGUI code

	private void HandleConstantPhysicalSize(CanvasScaler scaler, float dpiToUse){
//		float currentDpi = Screen.dpi;
//		float dpi = (currentDpi == 0 ? m_FallbackScreenDPI : currentDpi);
		float dpi = dpiToUse; // original: Screen.dpi
		float targetDPI = 1;
//		switch (m_PhysicalUnit)
		switch (scaler.physicalUnit){
		case CanvasScaler.Unit.Centimeters: targetDPI = 2.54f; break;
		case CanvasScaler.Unit.Millimeters: targetDPI = 25.4f; break;
		case CanvasScaler.Unit.Inches:      targetDPI =     1; break;
		case CanvasScaler.Unit.Points:      targetDPI =    72; break;
		case CanvasScaler.Unit.Picas:       targetDPI =     6; break;
		}
		
		SetScaleFactor(dpi / targetDPI, scaler);
//		SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit * targetDPI / m_DefaultSpriteDPI);
		SetReferencePixelsPerUnit(scaler.referencePixelsPerUnit * targetDPI / scaler.defaultSpriteDPI, scaler);
	}


	private void SetScaleFactor(float scaleFactor, CanvasScaler scaler){
//		if (scaleFactor == m_PrevScaleFactor)
//			return;
		
//		m_Canvas.scaleFactor = scaleFactor;
		scaler.GetComponent<Canvas>().scaleFactor = scaleFactor;
//		m_PrevScaleFactor = scaleFactor;
	}
	
	private void SetReferencePixelsPerUnit(float referencePixelsPerUnit, CanvasScaler scaler){
//		if (referencePixelsPerUnit == m_PrevReferencePixelsPerUnit)
//			return;

//		m_Canvas.referencePixelsPerUnit = referencePixelsPerUnit;
		scaler.GetComponent<Canvas>().referencePixelsPerUnit = referencePixelsPerUnit;
//		m_PrevReferencePixelsPerUnit = referencePixelsPerUnit;
	}

	#endregion

#endif


// uGUI 4.6-5.3 
// source: https://bitbucket.org/Unity-Technologies/ui/src/b5f9aae6ff7c2c63a521a1cb8b3e3da6939b191b/UnityEngine.UI/UI/Core/Layout/CanvasScaler.cs?at=5.3
//protected virtual void HandleConstantPhysicalSize()
//    {
//        float currentDpi = Screen.dpi;
//        float dpi = (currentDpi == 0 ? m_FallbackScreenDPI : currentDpi);
//        float targetDPI = 1;
//        switch (m_PhysicalUnit)
//        {
//            case Unit.Centimeters: targetDPI = 2.54f; break;
//            case Unit.Millimeters: targetDPI = 25.4f; break;
//            case Unit.Inches:      targetDPI =     1; break;
//            case Unit.Points:      targetDPI =    72; break;
//            case Unit.Picas:       targetDPI =     6; break;
//        }
//
//        SetScaleFactor(dpi / targetDPI);
//        SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit * targetDPI / m_DefaultSpriteDPI);
//    }
//
//    protected void SetScaleFactor(float scaleFactor)
//    {
//        if (scaleFactor == m_PrevScaleFactor)
//            return;
//
//        m_Canvas.scaleFactor = scaleFactor;
//        m_PrevScaleFactor = scaleFactor;
//    }
//
//    protected void SetReferencePixelsPerUnit(float referencePixelsPerUnit)
//    {
//        if (referencePixelsPerUnit == m_PrevReferencePixelsPerUnit)
//            return;
//
//        m_Canvas.referencePixelsPerUnit = referencePixelsPerUnit;
//        m_PrevReferencePixelsPerUnit = referencePixelsPerUnit;
//    }

}
#endif