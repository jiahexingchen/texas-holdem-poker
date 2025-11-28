using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace TexasHoldem.Scenes
{
    public class LoadingSceneController : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text tipsText;
        [SerializeField] private Image backgroundImage;

        [Header("Settings")]
        [SerializeField] private float minimumLoadTime = 2f;
        [SerializeField] private string nextSceneName = "MainScene";

        private string[] _tips = new string[]
        {
            "德州扑克中，位置非常重要，后位玩家有信息优势",
            "不要玩太多手牌，耐心等待好牌",
            "注意观察对手的下注模式",
            "诈唬要有选择性，不要过度诈唬",
            "管理好你的筹码，不要在一手牌上冒太大风险",
            "同花连牌在多人底池中价值更高",
            "顶对顶踢在单挑时是强牌",
            "学会适时弃牌，止损也是盈利的一部分",
            "记住，扑克是一场马拉松，不是短跑",
            "保持冷静，情绪化决策是大忌"
        };

        private void Start()
        {
            ShowRandomTip();
            StartCoroutine(LoadNextScene());
        }

        private void ShowRandomTip()
        {
            if (tipsText != null && _tips.Length > 0)
            {
                int index = Random.Range(0, _tips.Length);
                tipsText.text = "提示: " + _tips[index];
            }
        }

        private IEnumerator LoadNextScene()
        {
            float startTime = Time.time;

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
            asyncLoad.allowSceneActivation = false;

            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                
                if (progressBar != null)
                {
                    progressBar.value = progress;
                }

                if (progressText != null)
                {
                    progressText.text = $"{(int)(progress * 100)}%";
                }

                if (asyncLoad.progress >= 0.9f)
                {
                    float elapsed = Time.time - startTime;
                    if (elapsed >= minimumLoadTime)
                    {
                        if (progressBar != null) progressBar.value = 1f;
                        if (progressText != null) progressText.text = "100%";
                        
                        yield return new WaitForSeconds(0.5f);
                        asyncLoad.allowSceneActivation = true;
                    }
                }

                yield return null;
            }
        }

        public void SetNextScene(string sceneName)
        {
            nextSceneName = sceneName;
        }
    }
}
