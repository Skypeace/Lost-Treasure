using UnityEngine;
using System.Collections;
using System;

namespace EasyMobile
{
    // We need a game object to receive and handle messages from native side,
    // therefore this class inherits MonoBehaviour so it can be attached to that game object.
    public class MobileNativeAlert : MonoBehaviour
    {
        // The current active alert
        public static MobileNativeAlert Instance { get; private set; }

        /// <summary>
        /// Occurs when the alert is closed. The returned int value can be 0,1 and 2
        /// corresponding to button1, button2 and button3 respectively.
        /// </summary>
        public event Action<int> OnComplete = delegate {};

        private static readonly string ALERT_GAMEOBJECT = "MobileNativeAlert";

        /// <summary>
        /// Shows an alert dialog with 3 buttons.
        /// </summary>
        /// <returns>The three buttons alert.</returns>
        /// <param name="title">Title.</param>
        /// <param name="message">Message.</param>
        /// <param name="button1">Button1.</param>
        /// <param name="button2">Button2.</param>
        /// <param name="button3">Button3.</param>
        public static MobileNativeAlert CreateThreeButtonsAlert(string title, string message, string button1, string button2, string button3)
        {
            #if UNITY_IOS || UNITY_ANDROID
            if (Instance != null)
                return null;    // only allow one alert at a time

            // Create a Unity game object to receive messages from native side
            Instance = new GameObject(ALERT_GAMEOBJECT).AddComponent<MobileNativeAlert>();

            // Show the native platform-specific alert            
            #if UNITY_IOS
            iOSNativeAlert.ShowThreeButtonsAlert(title, message, button1, button2, button3);
            #elif UNITY_ANDROID
            AndroidNativeAlert.ShowThreeButtonsAlert(title, message, button1, button2, button3);
            #endif

            return Instance;

            #else
            return null;    // platform not supported
            #endif
        }

        /// <summary>
        /// Shows an alert dialog with 2 buttons.
        /// </summary>
        /// <returns>The two buttons alert.</returns>
        /// <param name="title">Title.</param>
        /// <param name="message">Message.</param>
        /// <param name="button1">Button1.</param>
        /// <param name="button2">Button2.</param>
        public static MobileNativeAlert CreateTwoButtonsAlert(string title, string message, string button1, string button2)
        {
            #if UNITY_IOS || UNITY_ANDROID
            if (Instance != null)
                return null;    // only allow one alert at a time

            // Create a Unity game object to receive messages from native side
            Instance = new GameObject(ALERT_GAMEOBJECT).AddComponent<MobileNativeAlert>();

            // Show the native platform-specific alert            
            #if UNITY_IOS
            iOSNativeAlert.ShowTwoButtonsAlert(title, message, button1, button2);
            #elif UNITY_ANDROID
            AndroidNativeAlert.ShowTwoButtonsAlert(title, message, button1, button2);
            #endif

            return Instance;

            #else
            return null;    // platform not supported
            #endif
        }

        /// <summary>
        /// Shows an alert dialog with 1 button.
        /// </summary>
        /// <returns>The one button alert.</returns>
        /// <param name="title">Title.</param>
        /// <param name="message">Message.</param>
        /// <param name="button">Button.</param>
        public static MobileNativeAlert CreateOneButtonAlert(string title, string message, string button)
        {
            #if UNITY_IOS || UNITY_ANDROID
            if (Instance != null)
                return null;    // only allow one alert at a time

            // Create a Unity game object to receive messages from native side
            Instance = new GameObject(ALERT_GAMEOBJECT).AddComponent<MobileNativeAlert>();

            // Show the native platform-specific alert            
            #if UNITY_IOS
            iOSNativeAlert.ShowOneButtonAlert(title, message, button);
            #elif UNITY_ANDROID
            AndroidNativeAlert.ShowOneButtonAlert(title, message, button);
            #endif

            return Instance;

            #else
            return null;    // platform not supported
            #endif
        }

        /// <summary>
        /// Creates an alert dialog with the default "OK" button.
        /// </summary>
        /// <returns>The alert.</returns>
        /// <param name="title">Title.</param>
        /// <param name="message">Message.</param>
        public static MobileNativeAlert CreateAlert(string title, string message)
        {
            return CreateOneButtonAlert(title, message, "OK");
        }

        #if UNITY_ANDROID
        /// <summary>
        /// Shows a toast message (Android only).
        /// </summary>
        /// <param name="message">Message.</param>
        public static void ShowToast(string message)
        {
            AndroidNativeAlert.ShowToast(message);
        }
        #endif

        // Alert callback to be called from native side with UnitySendMessage
        private void OnNativeAlertCallback(string buttonIndex)
        {
            int bIndex = Convert.ToInt16(buttonIndex);

            // Fire event
            OnComplete(bIndex);

            // Destroy the used object
            Instance = null;
            Destroy(gameObject);
        }
    }
}

