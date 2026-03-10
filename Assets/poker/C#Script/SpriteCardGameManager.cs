using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace CardGame
{
    public enum GameState
    {
        CoinFlipping,
        PlayerTurn,
        OpponentTurn,
        GameOver,
        DealingCards,
        ComparingCards,
        RoundEnd,
        WaitingForScoreboard
    }

    public class SpriteCardGameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private int initialHandSize = 5;
        [SerializeField] private float tableSize = 12f;
        [SerializeField] private int maxRounds = 5;
        
        [Header("Card Settings")]
        [SerializeField] private float cardScale = 0.45f;
        [SerializeField] private float cardSpacing = 1.0f;
        [SerializeField] private float fanAngle = 70f;
        
        [Header("Player Positions")]
        [SerializeField] private Vector3 playerHandBasePosition = new Vector3(0, -10f, 0);
        [SerializeField] private Vector3 opponentHandBasePosition = new Vector3(0, 10f, 0);
        [SerializeField] private Vector3 tableCenter = Vector3.zero;
        [SerializeField] private Vector3 deckPosition = new Vector3(-5f, 0f, 0f); // 牌堆位置
        
        [Header("Animation Settings")]
        [SerializeField] private float cardMoveSpeed = 12f;
        [SerializeField] private float cardDealDelay = 0.3f;
        [SerializeField] private float turnDelay = 1f;
        [SerializeField] private float roundEndDelay = 2f;
        
        [Header("Selection Settings")]
        [SerializeField] private float selectedCardHeight = 0.5f;
        [SerializeField] private Color selectedCardColor = new Color(1.3f, 1.3f, 1f);
        [SerializeField] private Color normalCardColor = Color.white;
        
        [Header("Sorting Settings")]
        [SerializeField] private int baseSortingOrder = 0;
        [SerializeField] private int handSortingOrderBase = 10;
        [SerializeField] private int playedCardSortingOrderBase = 100;
        [SerializeField] private int tableSortingOrder = -5;
        [SerializeField] private int backgroundSortingOrder = -10;
        [SerializeField] private int deckSortingOrder = -1; // 牌堆排序层级
        
        [Header("Layer Height Settings")]
        [SerializeField] private float layerHeightIncrement = 0.1f;
        [SerializeField] private float twoCardFanAngle = 30f;
        [SerializeField] private float singleCardCenterOffset = 0f;
        [SerializeField] private float deckCardHeightIncrement = 0.02f; // 牌堆卡片高度增量
        
        [Header("Input Settings")]
        [SerializeField] private KeyCode selectFirstKey = KeyCode.W;
        [SerializeField] private KeyCode selectLeftKey = KeyCode.A;
        [SerializeField] private KeyCode selectRightKey = KeyCode.D;
        [SerializeField] private KeyCode cancelSelectKey = KeyCode.S;
        [SerializeField] private KeyCode playCardKey = KeyCode.Space;
        
        [Header("AI Settings")]
        [SerializeField] private float aiThinkTime = 1f;
        
        [Header("Background Settings")]
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private float backgroundScale = 1f;
        [SerializeField] private Color backgroundColor = Color.white;
        
        [Header("Special Rules")]
        [SerializeField] private bool aceBeatsJokers = true; // A能比过大王和小王
        
        [Header("Deck Settings")]
        [SerializeField] private float deckCardScale = 0.45f; // 牌堆卡片缩放
        [SerializeField] private int maxDeckDisplayCards = 10; // 牌堆最多显示的卡牌数量
        
        private List<(CardRank rank, CardSuit suit)> cardDeck = new List<(CardRank, CardSuit)>();
        private List<SpriteCardController> playerCards = new List<SpriteCardController>();
        private List<SpriteCardController> opponentCards = new List<SpriteCardController>();
        private List<SpriteCardController> playedCards = new List<SpriteCardController>();
        private List<GameObject> deckDisplayCards = new List<GameObject>(); // 牌堆显示卡牌
        
        private SpriteCardController selectedCard = null;
        private int selectedCardIndex = -1;
        private GameObject tableObject;
        private GameObject backgroundObject;
        private GameObject deckBaseObject; // 牌堆底座
        private int currentPlayedCardOrder = 0;
        private bool canSelectCard = true;
        private GameState currentGameState = GameState.CoinFlipping;
        
        private SpriteCardController playerPlayedCard = null;
        private SpriteCardController opponentPlayedCard = null;
        
        private int playerScore = 0;
        private int opponentScore = 0;
        private int currentRound = 1;
        private bool isPlayerTurnFirst = true;
        private bool waitingForNextRound = false;
        private bool hasPlayerPlayedThisRound = false;
        private bool hasOpponentPlayedThisRound = false;
        private bool isFinalRoundCompleted = false;
        
        // 存储卡牌的原始位置信息
        private Dictionary<SpriteCardController, Vector3> cardOriginalPositions = new Dictionary<SpriteCardController, Vector3>();
        
        public IReadOnlyList<SpriteCardController> PlayerCards => playerCards;
        public IReadOnlyList<SpriteCardController> OpponentCards => opponentCards;
        public IReadOnlyList<SpriteCardController> PlayedCards => playedCards;
        public GameState CurrentGameState => currentGameState;
        public int PlayerScore => playerScore;
        public int OpponentScore => opponentScore;
        public int CurrentRound => currentRound;
        
        private void Start()
        {
            InitializeGame();
        }
        
        private void Update()
        {
            if (currentGameState == GameState.PlayerTurn && canSelectCard && playerCards.Count > 0)
            {
                HandlePlayerInput();
            }
        }
        
        private void HandlePlayerInput()
        {
            if (Input.GetKeyDown(selectFirstKey))
            {
                SelectCardByIndex(0);
            }
            else if (Input.GetKeyDown(selectLeftKey))
            {
                if (selectedCardIndex == -1)
                {
                    SelectCardByIndex(0);
                }
                else
                {
                    int newIndex = (selectedCardIndex - 1 + playerCards.Count) % playerCards.Count;
                    SelectCardByIndex(newIndex);
                }
            }
            else if (Input.GetKeyDown(selectRightKey))
            {
                if (selectedCardIndex == -1)
                {
                    SelectCardByIndex(0);
                }
                else
                {
                    int newIndex = (selectedCardIndex + 1) % playerCards.Count;
                    SelectCardByIndex(newIndex);
                }
            }
            else if (Input.GetKeyDown(cancelSelectKey))
            {
                DeselectCard();
            }
            else if (Input.GetKeyDown(playCardKey) && selectedCard != null)
            {
                PlaySelectedCard();
            }
        }
        
        public void InitializeGame()
        {
            if (CameraController.Instance != null)
            {
                CameraController.Instance.ResetToDefault();
            }
            
            CreateBackground();
            CreateTable();
            CreateDeckBase();
            InitializeCoinFlipManager();
            InitializeScoreboardManager();
            
            if (ScoreboardManager.Instance != null)
            {
                ScoreboardManager.Instance.ResetRoundState();
            }
            
            playerScore = 0;
            opponentScore = 0;
            currentRound = 1;
            waitingForNextRound = false;
            isFinalRoundCompleted = false;
            cardOriginalPositions.Clear();
            
            Debug.Log("游戏初始化完成，开始游戏");
            Debug.Log($"特殊规则: A {(aceBeatsJokers ? "能" : "不能")}比过大王和小王");
            StartCoroutine(StartWithCoinFlip());
        }
        
        private void CreateBackground()
        {
            backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(transform);
            
            SpriteRenderer backgroundRenderer = backgroundObject.AddComponent<SpriteRenderer>();
            
            if (backgroundSprite != null)
            {
                backgroundRenderer.sprite = backgroundSprite;
            }
            else
            {
                backgroundRenderer.sprite = CreateDefaultBackgroundSprite();
            }
            
            backgroundRenderer.color = backgroundColor;
            backgroundRenderer.sortingLayerName = "Background";
            backgroundRenderer.sortingOrder = backgroundSortingOrder;
            
            backgroundObject.transform.position = tableCenter;
            backgroundObject.transform.localScale = Vector3.one * backgroundScale * (tableSize / 5f);
            
            Debug.Log("背景创建完成");
        }
        
        private Sprite CreateDefaultBackgroundSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.3f));
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        }
        
        private void CreateTable()
        {
            if (CardSpriteManager.Instance != null)
            {
                Sprite randomTableSprite = CardSpriteManager.Instance.GetRandomTableSprite();
                
                if (randomTableSprite != null)
                {
                    tableObject = new GameObject("Table");
                    SpriteRenderer tableRenderer = tableObject.AddComponent<SpriteRenderer>();
                    tableRenderer.sprite = randomTableSprite;
                    tableRenderer.sortingLayerName = "Background";
                    tableRenderer.sortingOrder = tableSortingOrder;
                    
                    tableObject.transform.localScale = Vector3.one * (tableSize / 5f);
                    tableObject.transform.position = tableCenter;
                }
                else
                {
                    CreateDefaultTable();
                }
            }
            else
            {
                CreateDefaultTable();
            }
        }
        
        private void CreateDefaultTable()
        {
            tableObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            tableObject.name = "Table";
            tableObject.transform.localScale = Vector3.one * (tableSize / 10f);
            tableObject.transform.position = tableCenter;
            
            Renderer tableRenderer = tableObject.GetComponent<Renderer>();
            if (tableRenderer != null)
            {
                tableRenderer.material.color = new Color(0.2f, 0.4f, 0.1f);
            }
        }
        
        private void CreateDeckBase()
        {
            // 创建牌堆底座
            deckBaseObject = new GameObject("DeckBase");
            deckBaseObject.transform.SetParent(transform);
            deckBaseObject.transform.position = deckPosition;
            
            // 添加一个简单的底座精灵（可以用一个矩形）
            SpriteRenderer deckRenderer = deckBaseObject.AddComponent<SpriteRenderer>();
            Texture2D deckTexture = new Texture2D(1, 1);
            deckTexture.SetPixel(0, 0, new Color(0.3f, 0.2f, 0.1f, 0.7f));
            deckTexture.Apply();
            deckRenderer.sprite = Sprite.Create(deckTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            deckRenderer.sortingLayerName = "Default";
            deckRenderer.sortingOrder = deckSortingOrder - 1;
            deckBaseObject.transform.localScale = new Vector3(3f, 4f, 1f);
            
            Debug.Log("牌堆底座创建完成");
        }
        
        private void InitializeCoinFlipManager()
        {
            if (CoinFlipManager.Instance == null)
            {
                GameObject coinManagerObject = new GameObject("CoinFlipManager");
                coinManagerObject.AddComponent<CoinFlipManager>();
            }
            
            if (CardSpriteManager.Instance != null)
            {
                Sprite frontSprite = CardSpriteManager.Instance.GetCardSprite("poker_COIN_a");
                Sprite backSprite = CardSpriteManager.Instance.GetCardSprite("poker_COIN_b");
                
                if (frontSprite != null && backSprite != null)
                {
                    CoinFlipManager.Instance.SetCoinSprites(frontSprite, backSprite);
                }
            }
            
            CoinFlipManager.Instance.OnCoinFlipFinished += OnCoinFlipFinished;
        }
        
        private void InitializeScoreboardManager()
        {
            if (ScoreboardManager.Instance == null)
            {
                GameObject scoreboardObject = new GameObject("ScoreboardManager");
                scoreboardObject.AddComponent<ScoreboardManager>();
            }
        }
        
        private IEnumerator StartWithCoinFlip()
        {
            currentGameState = GameState.CoinFlipping;
            Debug.Log($"游戏开始 - 投硬币决定先手");
            
            yield return new WaitForSeconds(0.5f);
            
            if (CoinFlipManager.Instance != null && !CoinFlipManager.Instance.IsFlipping)
            {
                CoinFlipManager.Instance.StartCoinFlip();
            }
            else
            {
                bool isPlayerFirst = Random.Range(0, 2) == 0;
                OnCoinFlipFinished(isPlayerFirst);
            }
        }
        
        private void OnCoinFlipFinished(bool isFront)
        {
            isPlayerTurnFirst = isFront;
            hasPlayerPlayedThisRound = false;
            hasOpponentPlayedThisRound = false;
            
            DeselectCard();
            
            if (isFront)
            {
                Debug.Log($"玩家获得先手");
                StartCoroutine(StartGameAfterCoinFlip(true));
            }
            else
            {
                Debug.Log($"对手获得先手");
                StartCoroutine(StartGameAfterCoinFlip(false));
            }
        }
        
        private IEnumerator StartGameAfterCoinFlip(bool isPlayerFirst)
        {
            yield return new WaitForSeconds(0.5f);
            
            // 只在游戏开始时创建卡组和发牌
            if (currentRound == 1)
            {
                CreateDeck();
                CreateDeckVisuals(); // 创建牌堆视觉效果
                yield return StartCoroutine(DealCardsWithAnimation());
            }
            else
            {
                Debug.Log($"第 {currentRound} 回合继续游戏");
                ArrangeAllHands();
            }
            
            if (isPlayerFirst)
            {
                StartPlayerTurn();
            }
            else
            {
                StartCoroutine(OpponentTurn());
            }
        }
        
        private void CreateDeckVisuals()
        {
            ClearDeckVisuals();
            
            int displayCount = Mathf.Min(cardDeck.Count, maxDeckDisplayCards);
            
            for (int i = 0; i < displayCount; i++)
            {
                CreateDeckCard(i);
            }
            
            Debug.Log($"牌堆视觉效果创建完成，显示 {displayCount} 张牌");
        }
        
        private void CreateDeckCard(int index)
        {
            if (CardSpriteManager.Instance == null) return;
            
            GameObject cardObject = new GameObject($"DeckCard_{index}");
            cardObject.transform.SetParent(deckBaseObject.transform);
            
            // 设置位置 - 牌堆中的卡牌稍微偏移，形成堆叠效果
            float xOffset = Random.Range(-0.1f, 0.1f);
            float yOffset = Random.Range(-0.1f, 0.1f);
            float zOffset = index * deckCardHeightIncrement;
            
            Vector3 cardPosition = new Vector3(xOffset, yOffset, -zOffset);
            cardObject.transform.localPosition = cardPosition;
            
            // 添加轻微旋转，使牌堆看起来更自然
            float zRotation = Random.Range(-5f, 5f);
            cardObject.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
            
            // 添加SpriteRenderer
            SpriteRenderer cardRenderer = cardObject.AddComponent<SpriteRenderer>();
            Sprite cardBackSprite = CardSpriteManager.Instance.GetCardBackSprite();
            
            if (cardBackSprite != null)
            {
                cardRenderer.sprite = cardBackSprite;
            }
            
            cardRenderer.sortingLayerName = "Default";
            cardRenderer.sortingOrder = deckSortingOrder + index;
            
            cardObject.transform.localScale = Vector3.one * deckCardScale;
            
            deckDisplayCards.Add(cardObject);
        }
        
        private void ClearDeckVisuals()
        {
            foreach (var card in deckDisplayCards)
            {
                if (card != null)
                {
                    Destroy(card);
                }
            }
            deckDisplayCards.Clear();
        }
        
        private void UpdateDeckVisuals()
        {
            ClearDeckVisuals();
            
            int remainingCards = cardDeck.Count;
            int displayCount = Mathf.Min(remainingCards, maxDeckDisplayCards);
            
            for (int i = 0; i < displayCount; i++)
            {
                CreateDeckCard(i);
            }
            
            // 如果牌堆空了，隐藏底座
            if (remainingCards == 0 && deckBaseObject != null)
            {
                deckBaseObject.SetActive(false);
            }
        }
        
        private void StartPlayerTurn()
        {
            currentGameState = GameState.PlayerTurn;
            canSelectCard = true;
            
            Debug.Log($"第 {currentRound} 回合 - 玩家回合 - 请使用WASD选择卡牌，按空格出牌");
            
            if (playerCards.Count > 0)
            {
                // 自动选择第一张卡牌
                SelectCardByIndex(0);
                ShowPlayerTurnHint();
            }
            else
            {
                Debug.LogWarning("玩家没有手牌可出");
                CheckGameEnd();
            }
        }
        
        private void ShowPlayerTurnHint()
        {
            Debug.Log("提示：使用 W/A/D 选择卡牌，空格出牌，S 取消选择");
        }
        
        private IEnumerator OpponentTurn()
        {
            currentGameState = GameState.OpponentTurn;
            Debug.Log($"第 {currentRound} 回合 - 对手回合开始...");
            
            if (ScoreboardManager.Instance != null)
            {
                ScoreboardManager.Instance.RecordOpponentFirstPlay();
            }
            
            yield return new WaitForSeconds(aiThinkTime);
            
            if (opponentCards.Count > 0)
            {
                SpriteCardController cardToPlay = ChooseCardToPlay();
                if (cardToPlay != null)
                {
                    cardToPlay.FlipCard(true);
                    yield return new WaitForSeconds(0.5f);
                    StartCoroutine(PlayCardAnimation(cardToPlay, false));
                }
            }
            else
            {
                Debug.LogWarning("对手没有手牌可出");
                CheckGameEnd();
            }
        }
        
        private IEnumerator PlayCardAnimation(SpriteCardController card, bool isPlayerCard)
        {
            if (card == null) yield break;

            if (isPlayerCard)
            {
                hasPlayerPlayedThisRound = true;
                playerCards.Remove(card);
                playerPlayedCard = card;
                Debug.Log($"玩家出牌: {card.GetCardDisplayName()}");
            }
            else
            {
                hasOpponentPlayedThisRound = true;
                opponentCards.Remove(card);
                opponentPlayedCard = card;
                Debug.Log($"对手出牌: {card.GetCardDisplayName()}");
            }

            if (!playedCards.Contains(card))
            {
                playedCards.Add(card);
            }

            int playOrder = currentPlayedCardOrder++;
            card.SetSortingOrder(playOrder);

            Vector3 playPosition;
            if (isPlayerCard)
            {
                // 玩家区域：在基础位置基础上添加随机偏移
                playPosition = tableCenter + new Vector3(-3f, 0f, 0f);
                playPosition += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.3f, 0.3f), 0f);
            }
            else
            {
                // 对手区域：在基础位置基础上添加随机偏移
                playPosition = tableCenter + new Vector3(3f, 0f, 0f);
                playPosition += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.3f, 0.3f), 0f);
            }

            // 简化：只使用Z轴旋转
            float randomZRotation = Random.Range(-45f, 45f);  // Z轴旋转范围
            Quaternion playRotation = Quaternion.Euler(0f, 0f, randomZRotation);

            card.SetCardPosition(playPosition, playRotation);

            yield return new WaitForSeconds(0.8f);

            if (isPlayerCard)
            {
                ArrangePlayerHand();
            }
            else
            {
                ArrangeOpponentHand();
            }

            // 检查是否双方都已出牌（完成一回合）
            if (hasPlayerPlayedThisRound && hasOpponentPlayedThisRound)
            {
                // 双方都已出牌，进入比较环节
                currentGameState = GameState.ComparingCards;
                yield return StartCoroutine(CompareCards());
            }
            else
            {
                // 只有一方出牌，轮到另一方出牌
                if (isPlayerCard)
                {
                    // 玩家刚出牌，轮到对手
                    if (opponentCards.Count > 0)
                    {
                        currentGameState = GameState.OpponentTurn;
                        yield return new WaitForSeconds(turnDelay);
                        StartCoroutine(OpponentTurn());
                    }
                    else
                    {
                        // 对手没有手牌，检查游戏结束
                        CheckGameEnd();
                    }
                }
                else
                {
                    // 对手刚出牌，轮到玩家
                    if (playerCards.Count > 0)
                    {
                        StartPlayerTurn();
                    }
                    else
                    {
                        // 玩家没有手牌，检查游戏结束
                        CheckGameEnd();
                    }
                }
            }
        }
        
        private IEnumerator CompareCards()
        {
            Debug.Log("开始比较卡牌大小...");
            
            yield return new WaitForSeconds(1f);
            
            if (ScoreboardManager.Instance != null)
            {
                ScoreboardManager.Instance.MarkFirstRoundComparisonDone();
            }
            
            if (playerPlayedCard == null || opponentPlayedCard == null)
            {
                Debug.LogWarning("出牌记录不完整，尝试恢复...");
                RestorePlayedCards();
            }
            
            if (playerPlayedCard == null || opponentPlayedCard == null)
            {
                Debug.LogError("无法恢复出牌记录，跳过比较");
                ResetRoundState();
                CheckGameEnd();
                yield break;
            }
            
            int playerCardValue = GetCardValue(playerPlayedCard);
            int opponentCardValue = GetCardValue(opponentPlayedCard);
            
            Debug.Log($"玩家: {playerPlayedCard.GetCardDisplayName()} (点数: {playerCardValue}) vs 对手: {opponentPlayedCard.GetCardDisplayName()} (点数: {opponentCardValue})");
            
            bool playerWon = false;
            bool isDraw = false;
            
            if (playerCardValue > opponentCardValue)
            {
                playerScore++;
                playerWon = true;
                Debug.Log($"玩家获胜！得分: {playerScore}-{opponentScore}");
            }
            else if (playerCardValue < opponentCardValue)
            {
                opponentScore++;
                playerWon = false;
                Debug.Log($"对手获胜！得分: {playerScore}-{opponentScore}");
            }
            else
            {
                isDraw = true;
                Debug.Log($"平局！得分保持不变: {playerScore}-{opponentScore}");
            }
            
            MarkCardsAsCompared();
            
            bool shouldShowScoreboard = true;
            if (ScoreboardManager.Instance != null)
            {
                shouldShowScoreboard = ScoreboardManager.Instance.ShouldShowScoreboard();
            }
            
            if (shouldShowScoreboard)
            {
                Debug.Log("显示计分板...");
                currentGameState = GameState.WaitingForScoreboard;
                
                // 检查是否是游戏结束时的最后一次计分板显示
                bool isFinalRound = playerCards.Count == 0 && opponentCards.Count == 0;
                
                if (isFinalRound)
                {
                    Debug.Log("这是最后一回合，先显示计分板，然后显示最终结果");
                    isFinalRoundCompleted = true;
                    
                    if (isDraw)
                    {
                        // 平局时显示双方都为暗色的计分板
                        ScoreboardManager.ShowRoundResultDraw(playerScore, opponentScore, OnFinalRoundScoreboardHidden);
                    }
                    else
                    {
                        // 正常胜负显示
                        ScoreboardManager.ShowRoundResult(playerScore, opponentScore, playerWon, OnFinalRoundScoreboardHidden);
                    }
                }
                else
                {
                    if (isDraw)
                    {
                        // 平局时显示双方都为暗色的计分板
                        ScoreboardManager.ShowRoundResultDraw(playerScore, opponentScore, OnScoreboardHidden);
                    }
                    else
                    {
                        // 正常回合计分板
                        ScoreboardManager.ShowRoundResult(playerScore, opponentScore, playerWon, OnScoreboardHidden);
                    }
                }
                yield break;
            }
            else
            {
                Debug.Log("跳过计分板，直接继续");
                OnScoreboardHidden();
                yield break;
            }
        }
        
        private void OnFinalRoundScoreboardHidden()
        {
            Debug.Log("最后一回合计分板关闭，现在显示最终结果");
            
            // 重置回合状态
            ResetRoundState();
            
            // 显示最终结果
            StartCoroutine(ShowFinalResultAfterDelay());
        }
        
        private IEnumerator ShowFinalResultAfterDelay()
        {
            // 短暂延迟后显示最终结果
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log("显示最终胜负结果");
            currentGameState = GameState.WaitingForScoreboard;
            ScoreboardManager.ShowFinalResult(playerScore, opponentScore, OnFinalResultHidden);
        }
        
        private void OnFinalResultHidden()
        {
            Debug.Log("最终结果隐藏，游戏完全结束");
            currentGameState = GameState.GameOver;
            
            // 显示最终游戏统计
            DetermineFinalWinner();
            
            // 可以在这里添加重新开始游戏的逻辑
            Debug.Log("游戏结束，等待重新开始...");
        }
        
        private void OnScoreboardHidden()
        {
            Debug.Log("计分板隐藏回调开始");
            ResetRoundState();
            
            // 检查游戏是否结束
            if (CheckGameEnd())
            {
                Debug.Log("游戏结束");
            }
            else
            {
                Debug.Log("开始下一回合");
                // 开始下一回合
                StartNextRound();
            }
        }
        
        private void StartNextRound()
        {
            // 开始下一回合
            currentRound++;
            waitingForNextRound = false;
            
            // 重置回合状态
            ResetRoundState();
            canSelectCard = true;
            
            Debug.Log($"准备开始第 {currentRound} 回合...");
            
            // 修复：新回合不重新投硬币，保持原有的先手顺序
            if (isPlayerTurnFirst)
            {
                Debug.Log("保持玩家先手顺序");
                StartPlayerTurn();
            }
            else
            {
                Debug.Log("保持对手先手顺序");
                StartCoroutine(OpponentTurn());
            }
        }
        
        private void ResetRoundState()
        {
            playerPlayedCard = null;
            opponentPlayedCard = null;
            hasPlayerPlayedThisRound = false;
            hasOpponentPlayedThisRound = false;
        }
        
        private bool CheckGameEnd()
        {
            // 游戏结束条件：双方手牌都用完
            bool gameEnd = playerCards.Count == 0 && opponentCards.Count == 0;
            
            if (gameEnd && !waitingForNextRound && !isFinalRoundCompleted)
            {
                Debug.Log("检测到游戏结束条件，开始结束游戏流程");
                waitingForNextRound = true;
                return true;
            }
            
            return false;
        }
        
        private void DetermineFinalWinner()
        {
            Debug.Log("=== 游戏结束 ===");
            Debug.Log($"最终得分 - 玩家: {playerScore}, 对手: {opponentScore}");
            Debug.Log($"总回合数: {currentRound}");
            
            if (playerScore > opponentScore)
            {
                Debug.Log("🎉 恭喜玩家获胜！");
            }
            else if (playerScore < opponentScore)
            {
                Debug.Log("😞 对手获胜！");
            }
            else
            {
                Debug.Log("🤝 平局！");
            }
        }
        
        // ==================== 卡牌点数计算方法 ====================
        
        private int GetCardValue(SpriteCardController card)
        {
            if (card == null) return 0;
            
            // 特殊规则：A能比过大王和小王
            if (aceBeatsJokers && card.Rank == CardRank.Ace)
            {
                return 200; // A的点数设为200，比大王和小王都大
            }
            
            if (card.Suit == CardSuit.Joker)
            {
                return card.Rank == CardRank.BigJoker ? 100 : 99;
            }
            
            return card.Rank switch
            {
                CardRank.Ace => 1,
                CardRank.Two => 2,
                CardRank.Three => 3,
                CardRank.Four => 4,
                CardRank.Five => 5,
                CardRank.Six => 6,
                CardRank.Seven => 7,
                CardRank.Eight => 8,
                CardRank.Nine => 9,
                CardRank.Ten => 10,
                CardRank.Jack => 11,
                CardRank.Queen => 12,
                CardRank.King => 13,
                _ => 0
            };
        }
        
        // ==================== 卡牌选择相关方法 ====================
        
        private void SelectCardByIndex(int index)
        {
            if (index < 0 || index >= playerCards.Count) return;
            
            DeselectCard();
            
            selectedCardIndex = index;
            selectedCard = playerCards[index];
            
            ApplySelectionEffect(selectedCard);
            Debug.Log($"已选择: {selectedCard.GetCardDisplayName()} (按空格出牌，S取消选择)");
        }
        
        private void ApplySelectionEffect(SpriteCardController card)
        {
            if (card == null) return;
            
            card.SetSortingOrder(handSortingOrderBase + 100);
            
            // 保存原始位置，但只在需要时使用
            if (!cardOriginalPositions.ContainsKey(card))
            {
                cardOriginalPositions[card] = card.transform.position;
            }
            
            Vector3 selectedPosition = cardOriginalPositions[card] + Vector3.up * selectedCardHeight;
            selectedPosition.z = card.transform.position.z; // 保持层级高度
            
            // 恢复选择动画效果，但确保位置计算正确
            card.SetCardPosition(selectedPosition, card.transform.rotation);
            
            if (card.CardSpriteRenderer != null)
            {
                card.CardSpriteRenderer.color = selectedCardColor;
            }
        }
        
        private void DeselectCard()
        {
            if (selectedCard == null) return;
            
            if (selectedCard.CardSpriteRenderer != null)
            {
                selectedCard.CardSpriteRenderer.color = normalCardColor;
            }
            
            selectedCard.SetSortingOrder(handSortingOrderBase + selectedCardIndex);
            selectedCard = null;
            selectedCardIndex = -1;
            
            // 重新排列手牌，确保所有卡牌回到正确位置
            ArrangePlayerHand();
            Debug.Log("已取消选择卡牌");
        }
        
        public void PlaySelectedCard()
        {
            if (selectedCard == null || currentGameState != GameState.PlayerTurn || !canSelectCard) return;
            
            if (ScoreboardManager.Instance != null)
            {
                ScoreboardManager.Instance.RecordPlayerFirstPlay();
            }
            
            canSelectCard = false;
            StartCoroutine(PlayCardAnimation(selectedCard, true));
        }
        
        // ==================== 手牌布局方法 ====================
        
        private void ArrangePlayerHand()
        {
            ArrangeHand(playerCards, playerHandBasePosition, true);
        }
        
        private void ArrangeOpponentHand()
        {
            ArrangeHand(opponentCards, opponentHandBasePosition, false);
        }
        
        private void ArrangeAllHands()
        {
            ArrangePlayerHand();
            ArrangeOpponentHand();
        }
        
        private void ArrangeHand(List<SpriteCardController> hand, Vector3 basePosition, bool isPlayer)
        {
            int totalCards = hand.Count;
            
            for (int i = 0; i < totalCards; i++)
            {
                Vector3 position = CalculateCardPosition(i, totalCards, basePosition, isPlayer);
                Quaternion rotation = CalculateCardRotation(i, totalCards, isPlayer);
                
                float layerHeight = i * layerHeightIncrement;
                position.z = -layerHeight;
                
                // 保存卡牌的原始位置
                if (hand[i] != null)
                {
                    cardOriginalPositions[hand[i]] = position;
                    
                    // 恢复动画移动效果
                    hand[i].SetCardPosition(position, rotation);
                    hand[i].SetSortingOrder(handSortingOrderBase + i);
                    
                    if (hand[i].CardSpriteRenderer != null)
                    {
                        hand[i].CardSpriteRenderer.color = normalCardColor;
                    }
                }
            }
            
            // 如果当前有选中的卡牌，重新应用选择效果
            if (selectedCard != null && hand.Contains(selectedCard))
            {
                ApplySelectionEffect(selectedCard);
            }
        }
        
        // ==================== 卡牌位置计算方法 ====================
        
        private Vector3 CalculateCardPosition(int index, int totalCards, Vector3 basePosition, bool isPlayer)
        {
            // 特殊处理：只有1张牌时居中
            if (totalCards == 1)
            {
                return basePosition + new Vector3(singleCardCenterOffset, 0, 0);
            }
            
            // 特殊处理：只有2张牌时使用较小的扇形角度
            float currentFanAngle = totalCards == 2 ? twoCardFanAngle : fanAngle;
            float angleStep = currentFanAngle / Mathf.Max(1, totalCards - 1);
            float startAngle = -currentFanAngle * 0.5f;
            float currentAngle = startAngle + angleStep * index;
            
            if (!isPlayer)
            {
                currentAngle = -currentAngle;
                basePosition = opponentHandBasePosition;
            }
            
            float xOffset = Mathf.Sin(currentAngle * Mathf.Deg2Rad) * cardSpacing;
            float yOffset = Mathf.Cos(currentAngle * Mathf.Deg2Rad) * cardSpacing * 0.4f;
            
            if (!isPlayer)
            {
                yOffset = -Mathf.Abs(yOffset);
            }
            else
            {
                yOffset = Mathf.Abs(yOffset);
            }
            
            return basePosition + new Vector3(xOffset, yOffset, 0);
        }
        
        private Quaternion CalculateCardRotation(int index, int totalCards, bool isPlayer)
        {
            // 特殊处理：只有1张牌时不旋转
            if (totalCards == 1)
            {
                return isPlayer ? Quaternion.identity : Quaternion.Euler(0f, 0f, 180f);
            }
            
            // 特殊处理：只有2张牌时使用较小的扇形角度
            float currentFanAngle = totalCards == 2 ? twoCardFanAngle : fanAngle;
            float angleStep = currentFanAngle / Mathf.Max(1, totalCards - 1);
            float startAngle = -currentFanAngle * 0.5f;
            float currentAngle = startAngle + angleStep * index;
            
            if (!isPlayer)
            {
                currentAngle = -currentAngle;
            }
            
            float zRotation = isPlayer ? -currentAngle * 0.4f : 180f + currentAngle * 0.4f;
            
            return Quaternion.Euler(0f, 0f, zRotation);
        }
        
        // ==================== 发牌和动画方法 ====================
        
        private IEnumerator DealCardsWithAnimation()
        {
            ClearAllCards();
            currentPlayedCardOrder = playedCardSortingOrderBase;
            selectedCardIndex = -1;
            cardOriginalPositions.Clear();
            
            Debug.Log($"发牌 - 每方 {initialHandSize} 张");
            
            // 创建所有卡牌在牌堆位置
            List<SpriteCardController> allCards = new List<SpriteCardController>();
            
            for (int i = 0; i < initialHandSize && cardDeck.Count > 0; i++)
            {
                var playerCard = DrawCardFromDeck();
                SpriteCardController card = CreateCardAtDeck(playerCard.rank, playerCard.suit, false);
                playerCards.Add(card);
                allCards.Add(card);
            }
            
            for (int i = 0; i < initialHandSize && cardDeck.Count > 0; i++)
            {
                var opponentCard = DrawCardFromDeck();
                SpriteCardController card = CreateCardAtDeck(opponentCard.rank, opponentCard.suit, false);
                opponentCards.Add(card);
                allCards.Add(card);
            }
            
            // 更新牌堆视觉效果
            UpdateDeckVisuals();
            
            // 从牌堆发牌到玩家手牌位置
            yield return StartCoroutine(DealCardsFromDeck(allCards));
            
            Debug.Log($"发牌完成 - 玩家:{playerCards.Count}张, 对手:{opponentCards.Count}张");
        }
        
        private SpriteCardController CreateCardAtDeck(CardRank rank, CardSuit suit, bool showFront)
        {
            if (CardSpriteManager.Instance == null) return null;
            
            GameObject cardObject = CardSpriteManager.Instance.CreateCardInstance();
            if (cardObject == null) return null;
            
            SpriteCardController cardController = cardObject.GetComponent<SpriteCardController>();
            if (cardController != null)
            {
                // 将卡牌初始位置设在牌堆
                cardObject.transform.position = deckPosition;
                cardController.InitializeCard(rank, suit, showFront);
                cardController.SetSortingOrder(deckSortingOrder + 50); // 高于牌堆显示
                cardObject.transform.localScale = Vector3.one * cardScale;
                
                return cardController;
            }
            
            return null;
        }
        
        private IEnumerator DealCardsFromDeck(List<SpriteCardController> cards)
        {
            // 先发玩家牌
            for (int i = 0; i < playerCards.Count; i++)
            {
                SpriteCardController card = playerCards[i];
                if (card != null)
                {
                    Vector3 targetPosition = CalculateCardPosition(i, playerCards.Count, playerHandBasePosition, true);
                    Quaternion targetRotation = CalculateCardRotation(i, playerCards.Count, true);
                    
                    float layerHeight = i * layerHeightIncrement;
                    targetPosition.z = -layerHeight;
                    
                    cardOriginalPositions[card] = targetPosition;
                    card.SetCardPosition(targetPosition, targetRotation);
                    card.SetSortingOrder(handSortingOrderBase + i);
                }
                yield return new WaitForSeconds(cardDealDelay * 0.3f);
            }
            
            // 再发对手牌
            for (int i = 0; i < opponentCards.Count; i++)
            {
                SpriteCardController card = opponentCards[i];
                if (card != null)
                {
                    Vector3 targetPosition = CalculateCardPosition(i, opponentCards.Count, opponentHandBasePosition, false);
                    Quaternion targetRotation = CalculateCardRotation(i, opponentCards.Count, false);
                    
                    float layerHeight = i * layerHeightIncrement;
                    targetPosition.z = -layerHeight;
                    
                    cardOriginalPositions[card] = targetPosition;
                    card.SetCardPosition(targetPosition, targetRotation);
                    card.SetSortingOrder(handSortingOrderBase + i);
                }
                yield return new WaitForSeconds(cardDealDelay * 0.3f);
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // 翻玩家牌
            yield return StartCoroutine(FlipPlayerCardsWithLayering());
        }
        
        private IEnumerator FlipPlayerCardsWithLayering()
        {
            for (int i = 0; i < playerCards.Count; i++)
            {
                playerCards[i].FlipCard(true);
                yield return new WaitForSeconds(cardDealDelay * 0.4f);
            }
            
            yield return new WaitForSeconds(0.3f);
        }
        
        // ==================== 卡牌创建和管理方法 ====================
        
        private (CardRank rank, CardSuit suit) DrawCardFromDeck()
        {
            if (cardDeck.Count > 0)
            {
                var card = cardDeck[0];
                cardDeck.RemoveAt(0);
                return card;
            }
            return (CardRank.Ace, CardSuit.Club);
        }
        
        private void CreateDeck()
        {
            cardDeck.Clear();
            
            CardSuit[] suits = { CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade };
            CardRank[] ranks = { 
                CardRank.Two, CardRank.Three, CardRank.Four, CardRank.Five, CardRank.Six, 
                CardRank.Seven, CardRank.Eight, CardRank.Nine, CardRank.Ten, 
                CardRank.Jack, CardRank.Queen, CardRank.King, CardRank.Ace 
            };
            
            foreach (CardSuit suit in suits)
            {
                foreach (CardRank rank in ranks)
                {
                    cardDeck.Add((rank, suit));
                }
            }
            
            cardDeck.Add((CardRank.BigJoker, CardSuit.Joker));
            cardDeck.Add((CardRank.SmallJoker, CardSuit.Joker));
            
            ShuffleDeck();
        }
        
        private void ShuffleDeck()
        {
            for (int i = cardDeck.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = cardDeck[i];
                cardDeck[i] = cardDeck[j];
                cardDeck[j] = temp;
            }
        }
        
        private SpriteCardController ChooseCardToPlay()
        {
            if (opponentCards.Count == 0) return null;
            
            int playerCardValue = playerPlayedCard != null ? GetCardValue(playerPlayedCard) : 0;
            
            if (playerPlayedCard != null)
            {
                var winningCards = opponentCards
                    .Where(card => GetCardValue(card) > playerCardValue)
                    .OrderBy(card => GetCardValue(card))
                    .ToList();
                    
                if (winningCards.Count > 0)
                {
                    return winningCards[0];
                }
            }
            
            return opponentCards
                .OrderBy(card => GetCardValue(card))
                .First();
        }
        
        private void ClearTableCards()
        {
            // 修复：已打出的手牌不销毁，保留在桌面上
            // 只清空列表，但不销毁卡牌对象
            playedCards.Clear();
            currentPlayedCardOrder = playedCardSortingOrderBase;
        }
        
        private void ClearAllCards()
        {
            // 修复：只在游戏重新开始时销毁所有卡牌
            // 已打出的卡牌也会被销毁
            foreach (var card in playerCards) 
                if (card != null) Destroy(card.gameObject);
            foreach (var card in opponentCards) 
                if (card != null) Destroy(card.gameObject);
            foreach (var card in playedCards) 
                if (card != null) Destroy(card.gameObject);
            
            playerCards.Clear();
            opponentCards.Clear();
            playedCards.Clear();
            selectedCard = null;
            selectedCardIndex = -1;
            cardOriginalPositions.Clear();
            
            ClearDeckVisuals();
            
            ResetRoundState();
        }
        
        private void RestorePlayedCards()
        {
            if (playedCards.Count > 0)
            {
                var playerCardsOnTable = playedCards.Where(c => c.transform.position.x < 0).ToList();
                var opponentCardsOnTable = playedCards.Where(c => c.transform.position.x > 0).ToList();
                
                if (playerPlayedCard == null && playerCardsOnTable.Count > 0)
                {
                    playerPlayedCard = playerCardsOnTable.Last();
                }
                
                if (opponentPlayedCard == null && opponentCardsOnTable.Count > 0)
                {
                    opponentPlayedCard = opponentCardsOnTable.Last();
                }
            }
        }
        
        private void MarkCardsAsCompared()
        {
            if (playerPlayedCard != null && playerPlayedCard.CardSpriteRenderer != null)
            {
                playerPlayedCard.CardSpriteRenderer.color = new Color(0.7f, 0.7f, 0.7f);
            }
            if (opponentPlayedCard != null && opponentPlayedCard.CardSpriteRenderer != null)
            {
                opponentPlayedCard.CardSpriteRenderer.color = new Color(0.7f, 0.7f, 0.7f);
            }
        }
        
        public void SetBackgroundSprite(Sprite sprite)
        {
            backgroundSprite = sprite;
            if (backgroundObject != null)
            {
                SpriteRenderer renderer = backgroundObject.GetComponent<SpriteRenderer>();
                if (renderer != null && sprite != null)
                {
                    renderer.sprite = sprite;
                }
            }
        }
        
        public void SetBackgroundColor(Color color)
        {
            backgroundColor = color;
            if (backgroundObject != null)
            {
                SpriteRenderer renderer = backgroundObject.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = color;
                }
            }
        }
        
        public void SetBackgroundScale(float scale)
        {
            backgroundScale = scale;
            if (backgroundObject != null)
            {
                backgroundObject.transform.localScale = Vector3.one * scale * (tableSize / 5f);
            }
        }
        
        // 设置特殊规则
        public void SetAceBeatsJokers(bool enabled)
        {
            aceBeatsJokers = enabled;
            Debug.Log($"特殊规则: A {(enabled ? "能" : "不能")}比过大王和小王");
        }
        
        private void OnDestroy()
        {
            if (CoinFlipManager.Instance != null)
            {
                CoinFlipManager.Instance.OnCoinFlipFinished -= OnCoinFlipFinished;
            }
        }
        
        #if UNITY_EDITOR
        [ContextMenu("重新加载背景")]
        private void ReloadBackground()
        {
            if (backgroundObject != null)
            {
                DestroyImmediate(backgroundObject);
            }
            CreateBackground();
        }
        
        [ContextMenu("测试背景设置")]
        private void TestBackgroundSettings()
        {
            if (Application.isPlaying)
            {
                SetBackgroundColor(new Color(0.8f, 0.9f, 1f));
                SetBackgroundScale(1.2f);
                Debug.Log("背景设置测试完成");
            }
        }
        
        [ContextMenu("切换A比大小王规则")]
        private void ToggleAceRule()
        {
            SetAceBeatsJokers(!aceBeatsJokers);
        }
        
        [ContextMenu("重新创建牌堆")]
        private void RecreateDeck()
        {
            ClearDeckVisuals();
            CreateDeckVisuals();
        }
        #endif
    }
}