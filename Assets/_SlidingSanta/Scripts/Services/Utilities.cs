using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

namespace SgLib
{
    /// <summary>
    /// This class is not a singleton, but there's an static member Instance which is
    /// assigned to any alive instance of this class so that other classes can use it to access 
    /// public member methods. This way we can use it as a MonoBehaviour object to assign
    /// for button event in the inspector, while still can call its methods from the script
    /// just as an actual singleton.
    /// Usage: place a Utility object attached with this script in any scene needs to use these utilities.
    /// </summary>
    public class Utilities : MonoBehaviour
    {
        public static Utilities Instance { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Utilities anotherInstance = (Utilities)FindObjectOfType(typeof(Utilities));

                if (anotherInstance != null)
                {
                    Instance = anotherInstance;
                }
                else
                {
                    Instance = null;
                }
            }
        }

        public static IEnumerator CRWaitForRealSeconds(float time)
        {
            float start = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup < start + time)
            {
                yield return null;
            }
        }

        public void PlayButtonSound()
        {
            SoundManager.Instance.PlaySound(SoundManager.Instance.clickButton);
        }

        // Opens a specific scene
        public void GoToScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public void ToggleMute()
        {
            SoundManager.Instance.ToggleMute();
        }

        public void RateApp()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    Application.OpenURL(AppInfo.Instance.APPSTORE_LINK);
                    break;
        			
                case RuntimePlatform.Android:
                    Application.OpenURL(AppInfo.Instance.PLAYSTORE_LINK);
                    break;
            }
        }

        public void ShowMoreGames()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    Application.OpenURL(AppInfo.Instance.APPSTORE_HOMEPAGE);
                    break;
    			
                case RuntimePlatform.Android:
                    Application.OpenURL(AppInfo.Instance.PLAYSTORE_HOMEPAGE);
                    break;
            }
        }

        public void OpenFacebookPage()
        {
            Application.OpenURL(AppInfo.Instance.FACEBOOK_LINK);
        }

        public void OpenTwitterPage()
        {
            Application.OpenURL(AppInfo.Instance.TWITTER_LINK);
        }

        public void ContactUs()
        {
            string email = AppInfo.Instance.SUPPORT_EMAIL;
            string subject = EscapeURL(AppInfo.Instance.APP_NAME + " [" + Application.version + "] Support");
            string body = EscapeURL("");
            Application.OpenURL("mailto:" + email + "?subject=" + subject + "&body=" + body);
        }

        public string EscapeURL(string url)
        {
            return WWW.EscapeURL(url).Replace("+", "%20");
        }

        public int[] GenerateShuffleIndices(int length)
        {
            int[] array = new int[length];

            // Populate array
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = i;
            }

            // Shuffle
            for (int j = 0; j < array.Length; j++)
            {
                int tmp = array[j];
                int randomPos = Random.Range(j, array.Length);
                array[j] = array[randomPos];
                array[randomPos] = tmp;
            }

            return array;
        }

        /// <summary>
        /// Creates a sprite object from the given texture2D.
        /// </summary>
        /// <returns>The created sprite.</returns>
        /// <param name="filepath">Image file path.</param>
        public Sprite Texture2DToSprite(Texture2D texture)
        {
            if (texture != null)
            {
                // Create the sprite with the default pivot at image center
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                return sprite;
            }

            return null;
        }
    }
}