using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System;
using EasyMobile;
using PixelCrushers.DialogueSystem;

namespace SgLib
{
    public class UIManager : MonoBehaviour
    {
        public static bool firstLoad = true;

        [Header("Screenshot Sharing Config")]
        [Tooltip("Whether to show Share button")]
        public bool enableSharing = true;
        [Multiline(2)]
        [Tooltip("The share message, [score] will be replaced with actual score")]
        public string shareMessage = "Awesome! I've just scored [score] in Sliding Santa! #slidingsanta";
        [Tooltip("Check to attach app url as defined in AppInfo to the share message (not applied for sharing to Facebook)")]
        public bool shareAppUrl = true;

        [Header("Object References")]
        public GameManager gameManager;
        public GameObject instruction;
        public Text bestScore;
        public Text gold;
        public GameObject title;
        public Text score;
        public GameObject newBestScore;
        public GameObject watchVideo;
        public Button freeGifts;
        public Text freeGiftsText;
        public GameObject shareButton;
        public GameObject shareImage;
        public GameObject playButton;
        public GameObject selectCharacterButton;
        public GameObject menuButtons;
        public GameObject settingsPanel;
        public GameObject storePanel;
        public Button muteBtn;
        public Button unMuteBtn;
        public Button musicOnBtn;
        public Button musicOffBtn;
        public GameObject rewardingUI;
		public GameObject skillButton;
		public GameObject hpChange;
		public GameObject backButton;
		public GameObject startQuest;
		public float cooldownTime = 10f;
		private string time = "Round";

        Animator scoreAnimator;
        Sprite shareImageSprite;
        Texture2D capturedScreenshot;

        // On Android, we use a RenderTexture to take screenshot for better performance.
        #if UNITY_ANDROID
        RenderTexture screenshotRenderTexture;    
        #endif

        void OnEnable()
        {
            GameManager.NewGameEvent += OnNewGameEvent;
            PlayerController.StartRun += OnPlayerStartRun;
            ScoreManager.ScoreUpdated += OnScoreUpdated;
            SoundManager.MusicStatusChanged += OnMusicStatusChanged;
        }

        void OnDisable()
        {
            GameManager.NewGameEvent -= OnNewGameEvent;
            PlayerController.StartRun -= OnPlayerStartRun;
            ScoreManager.ScoreUpdated -= OnScoreUpdated;
            SoundManager.MusicStatusChanged -= OnMusicStatusChanged;
        }

        // Use this for initialization
        void Start()
        {
//			GameObject.Find("Captain Fronda").GetComponent<ConversationTrigger>().OnUse();
            scoreAnimator = score.GetComponent<Animator>();

            title.SetActive(false);
            instruction.SetActive(false);
            newBestScore.SetActive(false);
            score.gameObject.SetActive(false);
            watchVideo.SetActive(false);
            freeGifts.gameObject.SetActive(false); 
            freeGifts.GetComponent<Button>().interactable = false;
            shareButton.SetActive(false);
            playButton.SetActive(false);
            selectCharacterButton.SetActive(false); 
            menuButtons.SetActive(false);
            settingsPanel.SetActive(false);
            storePanel.SetActive(false);
            rewardingUI.SetActive(false);
			skillButton.SetActive(false);
			hpChange.SetActive(false);
			backButton.SetActive(false);
			startQuest.SetActive(false);

            if (firstLoad)
            {
                StartCoroutine(DisplayMenuUI(0.5f, false));
            }
            else
            {
                DisplayGameUI();
            }

            #if UNITY_ANDROID
            screenshotRenderTexture = new RenderTexture(Screen.width, Screen.width, 24);    // Squared screenshot on Android!
            #endif
        }

        // Update is called once per frame
        void Update()
        {
            score.text = ScoreManager.Instance.Score.ToString();
            bestScore.text = ScoreManager.Instance.HighScore.ToString();
            gold.text = CoinManager.Instance.Coins.ToString();
            UpdateMuteButtons();
            UpdateMusicButtons();

            if (freeGifts.gameObject.activeSelf)
            {
                TimeSpan timeToReward = DailyRewardController.Instance.TimeUntilReward;

                if (timeToReward <= TimeSpan.Zero)
                {
                    freeGiftsText.text = "GRAB YOUR REWARD!";
                    freeGifts.animator.SetTrigger("AnimateText");
                    freeGifts.interactable = true;
                }
                else
                {
                    freeGiftsText.text = string.Format("REWARD IN {0:00}.{1:00}.{2:00}", timeToReward.Hours, timeToReward.Minutes, timeToReward.Seconds);
                }
            }
        }

        void OnNewGameEvent(GameEvent e)
        {
            if (e == GameEvent.PreGameOver)
            {
                if (enableSharing)
                {
                    // Take screenshot.
                    StartCoroutine(CRCaptureScreenshot());
                }
            }
            else if (e == GameEvent.GameOver)
            {
                StartCoroutine(DisplayMenuUI(1, true));
//				GameObject.Find("Captain Fronda").GetComponent<ConversationTrigger>().OnUse();
				string saveData = PersistentDataManager.GetSaveData();
				PlayerPrefs.SetString("SavedGame", saveData);
				Debug.Log("Save Game Data: " + saveData);
				DialogueManager.ShowAlert("Game Saved");
			}
        }

        void OnPlayerStartRun()
        {
            if (selectCharacterButton.activeSelf)
            {
                selectCharacterButton.SetActive(false);
            }

            instruction.SetActive(false);
        }

        void OnScoreUpdated(int newScore)
        {
            scoreAnimator.Play("NewScore");
        }

        public void HandlePlayButton()
        {
            if (!firstLoad)
            {
                StartCoroutine(Restart());
            }
            else
            {
                DisplayGameUI();
                firstLoad = false;
            }
        }

        public void HandleSoundButton()
        {
            SoundManager.Instance.ToggleMute();
        }

        public void HandleMusicButton()
        {
            SoundManager.Instance.ToggleMusic();
        }

        public void HandleSelectCharacterButton()
        {
            SoundManager.Instance.Stop();
            SceneManager.LoadScene("CharacterSelection");
        }

        IEnumerator DisplayMenuUI(float delay = 0f, bool isGameOverUI = false)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

			freeGifts.gameObject.SetActive(false);
			playButton.SetActive(true);
			backButton.SetActive(true);
            menuButtons.SetActive(true);
			startQuest.SetActive(true);
            shareButton.SetActive(false);

            if (!isGameOverUI)
            {
                title.SetActive(false);
            }
            else
            {
                selectCharacterButton.SetActive(false);
                score.gameObject.SetActive(true);

                // Show New Best score.
                if (ScoreManager.Instance.HasNewHighScore)
                {
                    newBestScore.SetActive(true);
                }
                
                // Show Share button.
                #if UNITY_ANDROID
                if (enableSharing && screenshotRenderTexture != null && screenshotRenderTexture.IsCreated())
                {
                    capturedScreenshot = MobileNativeShare.CaptureFullScreenshot(screenshotRenderTexture);

                    yield return new WaitForSeconds(0.2f);
                    shareImageSprite = Utilities.Instance.Texture2DToSprite(capturedScreenshot);
                    Transform imgTf = shareImage.transform;
                    Image imgComp = imgTf.GetComponent<Image>();

                    float scaleFactor = imgTf.GetComponent<RectTransform>().rect.width / shareImageSprite.rect.width;
                    imgComp.sprite = shareImageSprite;
                    imgComp.SetNativeSize();
                    imgTf.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
                
                    yield return new WaitForSeconds(0.3f);
					shareButton.SetActive(false);
                }
                #else
                if (enableSharing && capturedScreenshot != null)
                {
                    yield return new WaitForSeconds(0.2f);
                    shareImageSprite = Utilities.Instance.Texture2DToSprite(capturedScreenshot);
                    Transform imgTf = shareImage.transform;
                    Image imgComp = imgTf.GetComponent<Image>();

                    float scaleFactor = imgTf.GetComponent<RectTransform>().rect.width / shareImageSprite.rect.width;
                    imgComp.sprite = shareImageSprite;
                    imgComp.SetNativeSize();
                    imgTf.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);

                    yield return new WaitForSeconds(0.3f);
                    shareButton.SetActive(true);
                }
                #endif
            }
        }

        public void DisplayGameUI()
        {
            watchVideo.SetActive(false);
            freeGifts.gameObject.SetActive(false);
            shareButton.SetActive(false);
            playButton.SetActive(false);
			backButton.SetActive(false);
            menuButtons.SetActive(false);
            title.gameObject.SetActive(false);
			selectCharacterButton.SetActive(false);
			startQuest.SetActive(false);

            instruction.SetActive(true);
            score.gameObject.SetActive(true);
			skillButton.SetActive(true);
			hpChange.SetActive(true);
        }

        public void ShowSettingsPanel()
        {
            settingsPanel.SetActive(true);
        }

        public void HideSettingsPanel()
        {
            settingsPanel.SetActive(false);
        }

        public void ShowStore()
        {
            storePanel.SetActive(true);
        }

        public void HideStore()
        {
            storePanel.SetActive(false);
        }

        public void ShowWatchForCoinsBtn()
        {
			watchVideo.SetActive(false);
        }

        public void HideWatchForCoinsBtn()
        {
            watchVideo.SetActive(false);
        }

        public void GrabDailyReward()
        {
            if (DailyRewardController.Instance.TimeUntilReward <= TimeSpan.Zero)
            {
//                freeGifts.animator.Stop();

                float reward = UnityEngine.Random.Range(gameManager.minRewardValue, gameManager.maxRewardValue);

                // Round the number and make it mutiplies of 5 only.
                int roundedReward = (Mathf.RoundToInt(reward) / 5) * 5;

                // Show the reward UI
                ShowRewardUI(roundedReward);

                // Update next time for the reward
                DailyRewardController.Instance.SetNextRewardTime(gameManager.rewardIntervalHours, gameManager.rewardIntervalMinutes, gameManager.rewardIntervalSeconds);
            }
        }

        public void ShowRewardUI(int reward)
        {
            rewardingUI.SetActive(true);
            rewardingUI.GetComponent<GiftRewardController>().Reward(reward);
        }

        public void HideRewardUI()
        {
            rewardingUI.SetActive(false);
        }

        void UpdateMuteButtons()
        {
            if (SoundManager.Instance.IsMuted())
            {
                unMuteBtn.gameObject.SetActive(false);
                muteBtn.gameObject.SetActive(true);
            }
            else
            {
                unMuteBtn.gameObject.SetActive(true);
                muteBtn.gameObject.SetActive(false);
            }
        }

        void UpdateMusicButtons()
        {
            if (SoundManager.Instance.IsMusicOff())
            {
                musicOffBtn.gameObject.SetActive(true);
                musicOnBtn.gameObject.SetActive(false);
            }
            else
            {
                musicOffBtn.gameObject.SetActive(false);
                musicOnBtn.gameObject.SetActive(true);
            }
        }

        void OnMusicStatusChanged(bool isOn)
        {
            if (!isOn)
            {
                SoundManager.Instance.Stop();
            }
        }

        IEnumerator Restart()
        {
            yield return new WaitForSeconds(0.2f);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        IEnumerator CRCaptureScreenshot()
        {
            #if UNITY_ANDROID
            if (screenshotRenderTexture != null)
            {
                // Wait for right timing to take screenshot
                yield return new WaitForEndOfFrame();

                // Temporarily render the camera content to our screenshotRenderTexture.
                // Later we'll share the screenshot from this rendertexture.
                Camera.main.targetTexture = screenshotRenderTexture;
                Camera.main.Render();
                yield return null;
                Camera.main.targetTexture = null;
            }
            #else
            // Wait for right timing to take screenshot
            yield return new WaitForEndOfFrame();

            capturedScreenshot = MobileNativeShare.CaptureFullScreenshot();
            #endif
        }

        public void ShareScreenshot()
        {
            if (capturedScreenshot == null)
            {
                Debug.Log("ShareScreenshot: FAIL. No captured screenshot.");
                return;
            }           

            string msg = shareMessage;

            msg = msg.Replace("[score]", ScoreManager.Instance.Score.ToString());

            if (shareAppUrl)
            {
                #if UNITY_IOS
                msg += "\n\n" + AppInfo.Instance.APPSTORE_SHARE_LINK;
                #elif UNITY_ANDROID
                msg += "\n\n" + AppInfo.Instance.PLAYSTORE_SHARE_LINK;
                #endif
            }  

            MobileNativeShare.ShareImageByTexture2D(capturedScreenshot, "sglib_screenshot.png", msg);
        }

        public void ShowLeaderboardUI()
        {
            if (GameServiceManager.Instance.IsInitialized)
            {
                GameServiceManager.Instance.ShowLeaderboardUI();
            }
            else
            {
                #if UNITY_IOS
                MobileNativeAlert.CreateOneButtonAlert("Service Unavailable", "The player is not signed in to Game Center.", "OK");
                #elif UNITY_ANDROID
                GameServiceManager.Instance.Init();
                #endif
            }
        }

        public void ShowAchievementUI()
        {
            if (GameServiceManager.Instance.IsInitialized)
            {
                GameServiceManager.Instance.ShowAchievementsUI();
            }
            else
            {
                #if UNITY_IOS
                MobileNativeAlert.CreateOneButtonAlert("Service Unavailable", "The player is not signed in to Game Center.", "OK");
                #elif UNITY_ANDROID
                GameServiceManager.Instance.Init();
                #endif
            }
        }

		public void OnButtonPress()
		{
			StartCoroutine("DisableButtonForSeconds", cooldownTime);
			int oldTimeValue = DialogueLua.GetVariable(time).AsInt;
			int newTimeValue = Mathf.Clamp(oldTimeValue +1, 0, 9999);
			DialogueLua.SetVariable(time, newTimeValue);
			DialogueManager.SendUpdateTracker();
		}

		IEnumerator DisableButtonForSeconds(float DisableTime)
		{
			GetComponent<Button>().interactable = false;
			yield return new WaitForSeconds(DisableTime);
			GetComponent<Button>().interactable = true;
		}
		public void OnBackButtonPress()
		{
			SceneManager.LoadScene("Storyselection");
		}
		public void OnStartQuestPress()
		{
			GameObject.Find("Captain Fronda").GetComponent<ConversationTrigger>().OnUse();
		}
    }
}
