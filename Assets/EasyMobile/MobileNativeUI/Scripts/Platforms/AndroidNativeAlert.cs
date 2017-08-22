using UnityEngine;
using System.Collections;

namespace EasyMobile
{
    public static class AndroidNativeAlert
    {
        #if UNITY_ANDROID
        private static readonly string JAVA_NATIVE_POPUP_CLASS = "com.sglib.unityplugins.nativepopup.NativePopup";
        #endif

        private static void CallStaticNative(string method, params object[] args)
        {
            #if UNITY_ANDROID
            AndroidJavaObject activity;
            using (AndroidJavaClass unityCls = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            { 
                activity = unityCls.GetStatic<AndroidJavaObject>("currentActivity");
            }

            AndroidJavaClass popupCls = new AndroidJavaClass(JAVA_NATIVE_POPUP_CLASS);

            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        popupCls.CallStatic(method, args);
                    })); 
            #endif
        }

        public static void ShowThreeButtonsAlert(string title, string message, string button1, string button2, string button3)
        {
            CallStaticNative("ShowThreeButtonsAlert", title, message, button1, button2, button3);
        }

        public static void ShowTwoButtonsAlert(string title, string message, string button1, string button2)
        {
            CallStaticNative("ShowTwoButtonsAlert", title, message, button1, button2);
        }

        public static void ShowOneButtonAlert(string title, string message, string button)
        {
            CallStaticNative("ShowOneButtonAlert", title, message, button);
        }

        public static void ShowToast(string message)
        {
            CallStaticNative("ShowToast", message);
        }
    }
}

