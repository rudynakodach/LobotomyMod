using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LobotomyMod.Patches;
using UnityEngine.UI;
using UnityEngine;
using System.Collections;

namespace LobotomyMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class LobotomyMod : BaseUnityPlugin
    {
        private protected Harmony harmony = new Harmony("rudynakodach.LobotomyMod");
        public static ManualLogSource LogSource { get; private set; }

        private void Awake()
        {
            LogSource = BepInEx.Logging.Logger.CreateLogSource("rudynakodach.LobotomyMod");
            
            harmony.PatchAll(typeof(LobotomyPatch));
            harmony.PatchAll(typeof(StartOfRoundPatch));
            LogSource.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }

    public class LobotomyControllerB : MonoBehaviour
    {
        public static LobotomyControllerB instance {get; private set;}
        PlayerControllerB playerController;
        RawImage rawImage;

        private bool isLobotomyAttackActive = false;
        private bool isUiReady = false;
        public bool lobotomyControllerStarted = false;
        public bool isActive = true;

        void Awake()
        {
            playerController = GameNetworkManager.Instance.localPlayerController;
        }

        void Update()
        {
            if (!playerController.criticallyInjured && !lobotomyControllerStarted)
            {
                playerController.DamagePlayer(5, true, true);
            }

            if (!isUiReady)
            {
                if(GameNetworkManager.Instance == null)
                {
                    return;
                }
                if(GameNetworkManager.Instance.localPlayerController == null)
                {
                    return;
                }
                if (GameNetworkManager.Instance.localPlayerController.gameplayCamera == null)
                {
                    return;
                }

                GameObject canvasContainer = new GameObject("LobotomyModCanvas");
                canvasContainer = Instantiate(canvasContainer);
                canvasContainer.transform.parent = GameNetworkManager.Instance.localPlayerController.gameplayCamera.gameObject.transform;
                Canvas c = canvasContainer.AddComponent<Canvas>();

                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 100;

                GameObject lobotomyImageObject = new GameObject("LobotomyModOverlay");
                lobotomyImageObject = Instantiate(lobotomyImageObject);
                lobotomyImageObject.transform.parent = canvasContainer.transform;
                
                rawImage = lobotomyImageObject.AddComponent<RawImage>();
                rawImage.material = new Material(rawImage.material);
                rawImage.material.name = "LobotomyModUIMaterial";

                rawImage.raycastTarget = false;
                rawImage.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
                rawImage.rectTransform.anchoredPosition = Vector2.zero;
                rawImage.color = Color.white;

                rawImage.gameObject.SetActive(false);

                isUiReady = true;
                instance = this;
                LobotomyMod.LogSource.LogDebug("Lobotomy UI ready!");
            }
            else
            {
                if (isActive)
                {
                    if (playerController == null)
                    {
                        return;
                    }

                    if (playerController.criticallyInjured && !lobotomyControllerStarted)
                    {
                        LobotomyMod.LogSource.LogDebug("Started invoking LobotomyContorller.");
                        InvokeRepeating(nameof(LobotomyController), 1f, 1f);
                        lobotomyControllerStarted = true;
                    }
                    else if ((!playerController.criticallyInjured || playerController.isPlayerDead) && lobotomyControllerStarted)
                    {
                        LobotomyMod.LogSource.LogDebug("Stopped invoking LobotomyContorller.");
                        CancelInvoke(nameof(LobotomyController));
                        lobotomyControllerStarted = false;
                    }
                } 
                else
                {
                    if(lobotomyControllerStarted)
                    {
                        CancelInvoke();
                    }
                }
            }
        }

        private IEnumerator fadeLobotomyOut(System.Action callback)
        {
            Color originalColor = rawImage.color;

            Color targetColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
            float fadeOutTime = Random.Range(0.75f, 1.75f);
            float elapsedTime = 0f;

            while (elapsedTime < fadeOutTime)
            {
                rawImage.color = Color.Lerp(originalColor, targetColor, elapsedTime / fadeOutTime);

                yield return null;

                elapsedTime += Time.deltaTime;
            }

            rawImage.color = targetColor;

            callback();
        }

        private IEnumerator recordFrame(System.Action<Texture2D> callback)
        {
            yield return new WaitForEndOfFrame();

            Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();

            callback(texture);
        }

        private IEnumerator stopAttack()
        {
            yield return new WaitForSeconds(Random.Range(.35f, .75f));

            StartCoroutine(fadeLobotomyOut(() =>
            {
                isLobotomyAttackActive = false;
                rawImage.gameObject.SetActive(false);
            }));
        }

        private IEnumerator startAttack(Texture2D texture)
        {
            yield return new WaitForSeconds(Random.Range(.35f, .75f));

            rawImage.material.mainTexture = texture;
            rawImage.gameObject.SetActive(true);

            StartCoroutine(stopAttack());
        }

        private void LobotomyController()
        {
            if(isLobotomyAttackActive)
            {
                return;
            }

            if (Random.Range(0f, 100f) < 20f) //TODO: Add configurable and/or scalable attack chances for lower hp values
            {
                StartCoroutine(recordFrame((texture) =>
                {
                    LobotomyMod.LogSource.LogInfo("Starting a lobotomy attack...");
                    rawImage.color = new Color(.85f, .85f, .85f, .85f);
                    isLobotomyAttackActive = true;
                    StartCoroutine(startAttack(texture));
                }));
            }
        }
    }
}
