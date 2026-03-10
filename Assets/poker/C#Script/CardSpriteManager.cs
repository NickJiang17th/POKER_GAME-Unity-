using System.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace CardGame
{
    public class CardSpriteManager : MonoBehaviour
    {
        public static CardSpriteManager Instance { get; private set; }
        
        [Header("Card Sprites")]
        [SerializeField] private Sprite cardBackSprite;
        [SerializeField] private List<Sprite> cardSprites = new List<Sprite>();
        
        [Header("Table Sprites")]
        [SerializeField] private List<Sprite> tableSprites = new List<Sprite>();
        
        [Header("Prefabs")]
        [SerializeField] private GameObject cardPrefab;
        
        private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        
        public GameObject CardPrefab => cardPrefab;
        public Sprite CardBackSprite => cardBackSprite;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSpriteCache();
        }
        
        private void InitializeSpriteCache()
        {
            spriteCache.Clear();
            
            foreach (var sprite in cardSprites)
            {
                if (sprite != null && !spriteCache.ContainsKey(sprite.name))
                {
                    spriteCache.Add(sprite.name, sprite);
                }
            }
        }
        
        public Sprite GetCardSprite(string cardName)
        {
            if (spriteCache.TryGetValue(cardName, out var sprite))
            {
                return sprite;
            }
            
            foreach (var key in spriteCache.Keys)
            {
                if (key.Contains(cardName) || cardName.Contains(key))
                {
                    return spriteCache[key];
                }
            }
            
            return null;
        }
        
        public Sprite GetCardBackSprite() => cardBackSprite;
        
        public Sprite GetCoinSprite(bool isFront)
        {
            string spriteName = isFront ? "poker_COIN_a" : "poker_COIN_b";
            return GetCardSprite(spriteName);
        }
        
        public bool HasCoinSprites()
        {
            return GetCardSprite("poker_COIN_a") != null && GetCardSprite("poker_COIN_b") != null;
        }
        
        public Sprite GetRandomTableSprite()
        {
            if (tableSprites == null || tableSprites.Count == 0)
            {
                return null;
            }
            
            int randomIndex = Random.Range(0, tableSprites.Count);
            return tableSprites[randomIndex];
        }
        
        public GameObject CreateCardInstance()
        {
            if (cardPrefab != null)
            {
                return Instantiate(cardPrefab);
            }
            
            return null;
        }
        
        public string GenerateCardName(CardRank rank, CardSuit suit)
        {
            if (suit == CardSuit.Joker)
            {
                return rank == CardRank.SmallJoker ? "poker_card_JOKER_b_Sprite_0" : "poker_card_JOKER_a_Sprite_0";
            }
            
            string rankStr = rank switch
            {
                CardRank.Jack => "JAKE",
                CardRank.Queen => "QUEEN",
                CardRank.King => "KING",
                CardRank.Ace => "A",
                _ => ((int)rank).ToString()
            };
            
            string suitStr = suit.ToString().ToLower();
            return $"poker_card_{rankStr}_{suitStr}_Sprite_0";
        }
        
        [ContextMenu("重新加载精灵缓存")]
        public void ReloadSpriteCache()
        {
            InitializeSpriteCache();
        }
    }
}