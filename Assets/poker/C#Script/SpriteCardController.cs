using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    public class SpriteCardController : MonoBehaviour
    {
        [Header("Sprite Components")]
        [SerializeField] private SpriteRenderer cardSpriteRenderer;
        [SerializeField] private BoxCollider2D cardCollider;
        
        [Header("Card Settings")]
        [SerializeField] private string cardIdentifier;
        [SerializeField] private bool showFront = true;
        
        [Header("Collider Settings")]
        [SerializeField] private Vector2 colliderSize = new Vector2(4.992317f, 6.523827f);
        [SerializeField] private Vector2 colliderOffset = new Vector2(0.6177261f, 0.2700429f);
        
        [Header("Animation Settings")]
        [SerializeField] private float moveSpeed = 10f;
        
        private Vector3 originalPosition;
        private bool isMoving = false;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        
        private CardSuit cardSuit;
        private CardRank cardRank;
        
        public string CardIdentifier => cardIdentifier;
        public bool ShowFront => showFront;
        public CardSuit Suit => cardSuit;
        public CardRank Rank => cardRank;
        public Vector3 OriginalPosition => originalPosition;
        public SpriteRenderer CardSpriteRenderer => cardSpriteRenderer;
        
        private void Start()
        {
            InitializeCardComponents();
        }
        
        private void Update()
        {
            if (isMoving)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, moveSpeed * Time.deltaTime);
                
                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                {
                    isMoving = false;
                    transform.SetPositionAndRotation(targetPosition, targetRotation);
                }
            }
        }
        
        private void InitializeCardComponents()
        {
            if (cardCollider == null)
                cardCollider = GetComponent<BoxCollider2D>();
            
            if (cardCollider != null)
            {
                cardCollider.size = colliderSize;
                cardCollider.offset = colliderOffset;
            }
            
            originalPosition = transform.position;
        }
        
        public void InitializeCard(CardRank rank, CardSuit suit, bool frontVisible = true)
        {
            cardRank = rank;
            cardSuit = suit;
            cardIdentifier = CardSpriteManager.Instance.GenerateCardName(rank, suit);
            showFront = frontVisible;
            UpdateCardSprite();
        }
        
        public void InitializeCard(string cardName, bool frontVisible = true)
        {
            cardIdentifier = cardName;
            showFront = frontVisible;
            ParseCardName(cardName);
            UpdateCardSprite();
        }
        
        private void ParseCardName(string cardName)
        {
            if (cardName.Contains("JOKER"))
            {
                cardSuit = CardSuit.Joker;
                cardRank = cardName.Contains("_a_") ? CardRank.BigJoker : CardRank.SmallJoker;
            }
            else
            {
                string[] parts = cardName.Split('_');
                if (parts.Length >= 4)
                {
                    string rankStr = parts[2];
                    string suitStr = parts[3];
                    
                    cardRank = ParseRank(rankStr);
                    cardSuit = ParseSuit(suitStr);
                }
            }
        }
        
        private CardRank ParseRank(string rankStr)
        {
            return rankStr.ToUpper() switch
            {
                "A" => CardRank.Ace,
                "JAKE" => CardRank.Jack,
                "QUEEN" => CardRank.Queen,
                "KING" => CardRank.King,
                "2" => CardRank.Two,
                "3" => CardRank.Three,
                "4" => CardRank.Four,
                "5" => CardRank.Five,
                "6" => CardRank.Six,
                "7" => CardRank.Seven,
                "8" => CardRank.Eight,
                "9" => CardRank.Nine,
                "10" => CardRank.Ten,
                _ => CardRank.Ace
            };
        }
        
        private CardSuit ParseSuit(string suitStr)
        {
            return suitStr.ToLower() switch
            {
                "club" => CardSuit.Club,
                "diamond" => CardSuit.Diamond,
                "heart" => CardSuit.Heart,
                "spade" => CardSuit.Spade,
                _ => CardSuit.Club
            };
        }
        
        public void FlipCard(bool showFrontSide)
        {
            if (showFront == showFrontSide) return;
            
            showFront = showFrontSide;
            UpdateCardSprite();
        }
        
        private void UpdateCardSprite()
        {
            if (cardSpriteRenderer == null || CardSpriteManager.Instance == null) return;
            
            Sprite sprite = showFront ? 
                CardSpriteManager.Instance.GetCardSprite(cardIdentifier) : 
                CardSpriteManager.Instance.GetCardBackSprite();
                
            if (sprite != null)
            {
                cardSpriteRenderer.sprite = sprite;
            }
        }
        
        public void SetCardPosition(Vector3 position, Quaternion rotation)
        {
            targetPosition = position;
            targetRotation = rotation;
            originalPosition = position;
            isMoving = true;
        }
        
        public void SetCardPositionImmediate(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            originalPosition = position;
            isMoving = false;
        }
        
        public void SetSortingOrder(int order)
        {
            if (cardSpriteRenderer != null)
            {
                cardSpriteRenderer.sortingOrder = order;
            }
        }
        
        public string GetCardDisplayName()
        {
            if (cardSuit == CardSuit.Joker)
            {
                return cardRank == CardRank.SmallJoker ? "小王" : "大王";
            }
            
            string suitName = cardSuit switch
            {
                CardSuit.Club => "梅花",
                CardSuit.Diamond => "方块",
                CardSuit.Heart => "红心",
                CardSuit.Spade => "黑桃",
                _ => ""
            };
            
            string rankName = cardRank switch
            {
                CardRank.Jack => "J",
                CardRank.Queen => "Q",
                CardRank.King => "K",
                CardRank.Ace => "A",
                _ => ((int)cardRank).ToString()
            };
            
            return $"{suitName}{rankName}";
        }
        
        public void SetColliderSizeAndOffset(Vector2 size, Vector2 offset)
        {
            if (cardCollider != null)
            {
                cardCollider.size = size;
                cardCollider.offset = offset;
            }
        }
    }
}