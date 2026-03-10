using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    public class ScoreboardManager : MonoBehaviour
    {
        public static ScoreboardManager Instance { get; private set; }
        
        [Header("Scoreboard Sprites")]
        [SerializeField] private Sprite scoreboardBase;
        [SerializeField] private Sprite[] numberSprites;
        [SerializeField] private Sprite[] darkNumberSprites;
        
        [Header("Result Sprites")]
        [SerializeField] private Sprite winSprite;
        [SerializeField] private Sprite loseSprite;
        
        [Header("Display Settings")]
        [SerializeField] private float displayDuration = 3f;
        
        [Header("Scoreboard Position Settings")]
        [SerializeField] private Vector3 scoreboardPosition = new Vector3(0, 0, 0);
        [SerializeField] private float numberOffsetX = 2f;
        [SerializeField] private float numberScale = 1f; // 数字大小缩放
        
        [Header("Final Result Position Settings")]
        [SerializeField] private Vector3 finalResultPosition = new Vector3(0, 0, 0);
        [SerializeField] private float resultScale = 1.5f; // 结果图标大小
        
        [Header("Background Settings")]
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);
        
        private GameObject scoreboardObject;
        private GameObject backgroundObject;
        private GameObject playerScoreObject;
        private GameObject opponentScoreObject;
        private GameObject resultObject;
        
        private bool isShowing = false;
        private System.Action onHideCallback;
        
        private bool isFirstRound = true;
        private bool firstRoundComparisonDone = false;
        private bool isShowingFinalResult = false;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            CreateScoreboardObjects();
            HideScoreboardImmediate();
        }
        
        private void CreateScoreboardObjects()
        {
            // 创建背景
            backgroundObject = new GameObject("ScoreboardBackground");
            backgroundObject.transform.SetParent(transform);
            SpriteRenderer bgRenderer = backgroundObject.AddComponent<SpriteRenderer>();
            bgRenderer.sprite = CreateBackgroundSprite();
            bgRenderer.color = backgroundColor;
            bgRenderer.sortingLayerName = "UI";
            bgRenderer.sortingOrder = 999;
            
            // 创建计分板底座
            scoreboardObject = new GameObject("ScoreboardBase");
            scoreboardObject.transform.SetParent(transform);
            SpriteRenderer baseRenderer = scoreboardObject.AddComponent<SpriteRenderer>();
            if (scoreboardBase != null)
            {
                baseRenderer.sprite = scoreboardBase;
            }
            baseRenderer.sortingLayerName = "UI";
            baseRenderer.sortingOrder = 1000;
            
            // 创建玩家分数显示
            playerScoreObject = new GameObject("PlayerScore");
            playerScoreObject.transform.SetParent(scoreboardObject.transform);
            SpriteRenderer playerRenderer = playerScoreObject.AddComponent<SpriteRenderer>();
            playerRenderer.sortingLayerName = "UI";
            playerRenderer.sortingOrder = 1001;
            
            // 创建对手分数显示
            opponentScoreObject = new GameObject("OpponentScore");
            opponentScoreObject.transform.SetParent(scoreboardObject.transform);
            SpriteRenderer opponentRenderer = opponentScoreObject.AddComponent<SpriteRenderer>();
            opponentRenderer.sortingLayerName = "UI";
            opponentRenderer.sortingOrder = 1001;
            
            // 创建结果显示
            resultObject = new GameObject("ResultDisplay");
            resultObject.transform.SetParent(transform);
            SpriteRenderer resultRenderer = resultObject.AddComponent<SpriteRenderer>();
            resultRenderer.sortingLayerName = "UI";
            resultRenderer.sortingOrder = 1002;
            resultObject.SetActive(false);
        }
        
        private Sprite CreateBackgroundSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        }
        
        // 显示回合结果（内部方法）
        private void ShowRoundResultInternal(int playerScore, int opponentScore, bool playerWon, System.Action onHide = null)
        {
            if (isShowing) 
            {
                onHide?.Invoke();
                return;
            }
            
            if (!CheckSpriteResources())
            {
                onHide?.Invoke();
                return;
            }
            
            isShowing = true;
            isShowingFinalResult = false;
            onHideCallback = onHide;
            
            // 设置位置 - 使用计分板位置
            scoreboardObject.transform.position = scoreboardPosition;
            backgroundObject.transform.position = scoreboardPosition;
            backgroundObject.transform.localScale = new Vector3(50f, 50f, 1f);
            
            // 隐藏结果对象（正常回合不显示结果）
            resultObject.SetActive(false);
            
            // 更新分数显示 - 根据胜负情况决定数字颜色
            if (playerWon)
            {
                // 玩家获胜：玩家数字高亮，对手数字暗色
                UpdateScoreDisplay(playerScoreObject, playerScore, true);
                UpdateScoreDisplay(opponentScoreObject, opponentScore, false);
            }
            else
            {
                // 对手获胜或平局：玩家数字暗色，对手数字高亮
                UpdateScoreDisplay(playerScoreObject, playerScore, false);
                UpdateScoreDisplay(opponentScoreObject, opponentScore, true);
            }
            
            // 设置分数位置和大小
            playerScoreObject.transform.localPosition = new Vector3(-numberOffsetX, 0f, 0f);
            opponentScoreObject.transform.localPosition = new Vector3(numberOffsetX, 0f, 0f);
            playerScoreObject.transform.localScale = Vector3.one * numberScale;
            opponentScoreObject.transform.localScale = Vector3.one * numberScale;
            
            // 显示所有对象
            scoreboardObject.SetActive(true);
            backgroundObject.SetActive(true);
            playerScoreObject.SetActive(true);
            opponentScoreObject.SetActive(true);
            
            StartCoroutine(ScoreboardDisplayRoutine());
        }
        
        // 显示回合结果（新增方法，支持平局情况）
        private void ShowRoundResultWithDrawInternal(int playerScore, int opponentScore, System.Action onHide = null)
        {
            if (isShowing) 
            {
                onHide?.Invoke();
                return;
            }
            
            if (!CheckSpriteResources())
            {
                onHide?.Invoke();
                return;
            }
            
            isShowing = true;
            isShowingFinalResult = false;
            onHideCallback = onHide;
            
            // 设置位置 - 使用计分板位置
            scoreboardObject.transform.position = scoreboardPosition;
            backgroundObject.transform.position = scoreboardPosition;
            backgroundObject.transform.localScale = new Vector3(50f, 50f, 1f);
            
            // 隐藏结果对象（正常回合不显示结果）
            resultObject.SetActive(false);
            
            // 平局时双方数字都显示为暗色
            UpdateScoreDisplay(playerScoreObject, playerScore, false); // 玩家数字暗色
            UpdateScoreDisplay(opponentScoreObject, opponentScore, false); // 对手数字暗色
            
            // 设置分数位置和大小
            playerScoreObject.transform.localPosition = new Vector3(-numberOffsetX, 0f, 0f);
            opponentScoreObject.transform.localPosition = new Vector3(numberOffsetX, 0f, 0f);
            playerScoreObject.transform.localScale = Vector3.one * numberScale;
            opponentScoreObject.transform.localScale = Vector3.one * numberScale;
            
            // 显示所有对象
            scoreboardObject.SetActive(true);
            backgroundObject.SetActive(true);
            playerScoreObject.SetActive(true);
            opponentScoreObject.SetActive(true);
            
            StartCoroutine(ScoreboardDisplayRoutine());
        }
        
        // 显示最终结果（内部方法）
        private void ShowFinalResultInternal(int playerScore, int opponentScore, System.Action onHide = null)
        {
            if (isShowing) 
            {
                onHide?.Invoke();
                return;
            }
            
            if (!CheckSpriteResources() || winSprite == null || loseSprite == null)
            {
                onHide?.Invoke();
                return;
            }
            
            isShowing = true;
            isShowingFinalResult = true;
            onHideCallback = onHide;
            
            // 判断胜负
            bool playerWon = playerScore > opponentScore;
            bool isDraw = playerScore == opponentScore;
            
            // 设置最终结果位置
            Vector3 resultWorldPosition = finalResultPosition;
            
            // 设置结果图标位置（居中）
            resultObject.transform.position = resultWorldPosition;
            resultObject.transform.localScale = Vector3.one * resultScale;
            
            // 设置背景位置
            backgroundObject.transform.position = resultWorldPosition;
            backgroundObject.transform.localScale = new Vector3(50f, 50f, 1f);
            
            // 最终结果时隐藏分数显示和计分板底座
            playerScoreObject.SetActive(false);
            opponentScoreObject.SetActive(false);
            scoreboardObject.SetActive(false); // 隐藏计分板底座
            
            // 设置结果精灵
            SpriteRenderer resultRenderer = resultObject.GetComponent<SpriteRenderer>();
            if (isDraw)
            {
                // 平局时不显示胜利/失败图标，或者可以显示平局图标
                resultRenderer.sprite = null; // 或者设置为平局专用的图标
                Debug.Log("游戏平局，不显示胜利/失败图标");
            }
            else
            {
                resultRenderer.sprite = playerWon ? winSprite : loseSprite;
            }
            
            // 显示结果对象和背景
            resultObject.SetActive(true);
            backgroundObject.SetActive(true);
            
            StartCoroutine(FinalResultDisplayRoutine());
        }
        
        private IEnumerator ScoreboardDisplayRoutine()
        {
            Time.timeScale = 0f;
            yield return StartCoroutine(WaitForInput());
            HideScoreboard();
        }
        
        private IEnumerator FinalResultDisplayRoutine()
        {
            Time.timeScale = 0f;
            yield return StartCoroutine(WaitForInput());
            HideScoreboard();
        }
        
        private IEnumerator WaitForInput()
        {
            bool inputReceived = false;
            float displayTime = 0f;
            
            while (!inputReceived && displayTime < displayDuration)
            {
                displayTime += Time.unscaledDeltaTime;
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    inputReceived = true;
                }
                yield return null;
            }
        }
        
        private void UpdateScoreDisplay(GameObject scoreObject, int score, bool isWinner)
        {
            SpriteRenderer renderer = scoreObject.GetComponent<SpriteRenderer>();
            if (renderer == null) return;
            
            score = Mathf.Clamp(score, 0, 9);
            Sprite[] targetSprites = isWinner ? numberSprites : darkNumberSprites;
            
            if (targetSprites != null && score >= 0 && score < targetSprites.Length && targetSprites[score] != null)
            {
                renderer.sprite = targetSprites[score];
            }
        }
        
        public void HideScoreboard()
        {
            if (!isShowing) return;
            
            Time.timeScale = 1f;
            scoreboardObject.SetActive(false);
            backgroundObject.SetActive(false);
            playerScoreObject.SetActive(false);
            opponentScoreObject.SetActive(false);
            resultObject.SetActive(false);
            
            isShowing = false;
            isShowingFinalResult = false;
            onHideCallback?.Invoke();
            onHideCallback = null;
        }
        
        public void HideScoreboardImmediate()
        {
            Time.timeScale = 1f;
            scoreboardObject.SetActive(false);
            backgroundObject.SetActive(false);
            playerScoreObject.SetActive(false);
            opponentScoreObject.SetActive(false);
            resultObject.SetActive(false);
            isShowing = false;
            isShowingFinalResult = false;
            onHideCallback = null;
        }
        
        public bool IsShowing => isShowing;
        public bool IsShowingFinalResult => isShowingFinalResult;
        
        // 静态方法 - 显示回合结果（支持平局）
        public static void ShowRoundResult(int playerScore, int opponentScore, bool playerWon, System.Action onHide = null)
        {
            if (Instance != null)
            {
                Instance.ShowRoundResultInternal(playerScore, opponentScore, playerWon, onHide);
            }
            else
            {
                onHide?.Invoke();
            }
        }
        
        // 静态方法 - 显示回合结果（平局专用）
        public static void ShowRoundResultDraw(int playerScore, int opponentScore, System.Action onHide = null)
        {
            if (Instance != null)
            {
                Instance.ShowRoundResultWithDrawInternal(playerScore, opponentScore, onHide);
            }
            else
            {
                onHide?.Invoke();
            }
        }
        
        // 静态方法 - 显示最终结果
        public static void ShowFinalResult(int playerScore, int opponentScore, System.Action onHide = null)
        {
            if (Instance != null)
            {
                Instance.ShowFinalResultInternal(playerScore, opponentScore, onHide);
            }
            else
            {
                onHide?.Invoke();
            }
        }
        
        public void SetScoreboardSprites(Sprite baseSprite, Sprite[] numbers, Sprite[] darkNumbers, Sprite win, Sprite lose)
        {
            scoreboardBase = baseSprite;
            numberSprites = numbers;
            darkNumberSprites = darkNumbers;
            winSprite = win;
            loseSprite = lose;
            
            if (scoreboardObject != null && scoreboardBase != null)
            {
                scoreboardObject.GetComponent<SpriteRenderer>().sprite = scoreboardBase;
            }
        }
        
        // 设置数字大小
        public void SetNumberScale(float scale)
        {
            numberScale = Mathf.Max(0.1f, scale);
        }
        
        // 设置结果图标大小
        public void SetResultScale(float scale)
        {
            resultScale = Mathf.Max(0.1f, scale);
        }
        
        // 设置最终结果位置参数
        public void SetFinalResultPosition(Vector3 position, float scale = 1.5f)
        {
            finalResultPosition = position;
            resultScale = scale;
        }
        
        // 设置计分板位置和数字参数
        public void SetScoreboardPosition(Vector3 position, float numberOffset = 2f, float numberScale = 1f)
        {
            scoreboardPosition = position;
            numberOffsetX = numberOffset;
            this.numberScale = numberScale;
        }
        
        public void ResetRoundState()
        {
            isFirstRound = true;
            firstRoundComparisonDone = false;
        }
        
        public void RecordOpponentFirstPlay()
        {
            if (isFirstRound)
            {
                firstRoundComparisonDone = false;
            }
        }
        
        public void RecordPlayerFirstPlay()
        {
            if (isFirstRound)
            {
                firstRoundComparisonDone = false;
            }
        }
        
        public void MarkFirstRoundComparisonDone()
        {
            if (isFirstRound && !firstRoundComparisonDone)
            {
                firstRoundComparisonDone = true;
            }
        }
        
        public bool ShouldShowScoreboard()
        {
            if (!isFirstRound || firstRoundComparisonDone)
            {
                if (isFirstRound && firstRoundComparisonDone)
                {
                    isFirstRound = false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public bool IsFirstRoundComplete()
        {
            return !isFirstRound;
        }
        
        private bool CheckSpriteResources()
        {
            if (scoreboardBase == null) return false;
            if (numberSprites == null || numberSprites.Length != 10) return false;
            if (darkNumberSprites == null || darkNumberSprites.Length != 10) return false;
            
            for (int i = 0; i < numberSprites.Length; i++)
            {
                if (numberSprites[i] == null) return false;
            }
            
            for (int i = 0; i < darkNumberSprites.Length; i++)
            {
                if (darkNumberSprites[i] == null) return false;
            }
            
            return true;
        }
        
        private void Update()
        {
            if (isShowing && Input.GetKeyDown(KeyCode.Escape))
            {
                HideScoreboard();
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        #if UNITY_EDITOR
        // 在编辑器中调试位置
        [ContextMenu("测试最终结果位置")]
        private void TestFinalResultPosition()
        {
            if (Application.isPlaying)
            {
                ShowFinalResult(3, 2, () => Debug.Log("测试完成"));
            }
            else
            {
                Debug.Log("请在运行模式下测试最终结果位置");
            }
        }
        
        [ContextMenu("测试计分板位置")]
        private void TestScoreboardPosition()
        {
            if (Application.isPlaying)
            {
                ShowRoundResult(3, 2, true, () => Debug.Log("测试完成"));
            }
            else
            {
                Debug.Log("请在运行模式下测试计分板位置");
            }
        }
        
        [ContextMenu("测试平局计分板")]
        private void TestDrawScoreboard()
        {
            if (Application.isPlaying)
            {
                ShowRoundResultDraw(3, 3, () => Debug.Log("平局测试完成"));
            }
            else
            {
                Debug.Log("请在运行模式下测试平局计分板");
            }
        }
        
        [ContextMenu("重置所有位置设置")]
        private void ResetAllPositions()
        {
            scoreboardPosition = Vector3.zero;
            numberOffsetX = 2f;
            numberScale = 1f;
            finalResultPosition = Vector3.zero;
            resultScale = 1.5f;
            Debug.Log("所有位置设置已重置为默认值");
        }
        #endif
    }
}