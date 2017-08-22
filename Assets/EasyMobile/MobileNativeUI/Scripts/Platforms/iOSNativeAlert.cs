using UnityEngine;
using System.Collections;

#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace EasyMobile
{
    public static class iOSNativeAlert
    {
        #if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void _ShowThreeButtonsAlert(string title, string message, string button1, string button2, string button3);

        [DllImport("__Internal")]
        private static extern void _ShowTwoButtonsAlert(string title, string message, string button1, string button2);

        [DllImport("__Internal")]
        private static extern void _ShowOneButtonAlert(string title, string message, string button);
        #endif

        public static void ShowThreeButtonsAlert(string title, string message, string button1, string button2, string button3)
        {
            #if UNITY_IOS && !UNITY_EDITOR
            _ShowThreeButtonsAlert(title, message, button1, button2, button3);
            #endif
        }

        public static void ShowTwoButtonsAlert(string title, string message, string button1, string button2)
        {
            #if UNITY_IOS && !UNITY_EDITOR
            _ShowTwoButtonsAlert(title, message, button1, button2);
            #endif
        }

        public static void ShowOneButtonAlert(string title, string message, string button)
        {
            #if UNITY_IOS && !UNITY_EDITOR
            _ShowOneButtonAlert(title, message, button);
            #endif
        }
    }
}

