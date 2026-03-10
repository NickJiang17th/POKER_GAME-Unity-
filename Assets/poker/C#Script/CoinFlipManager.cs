using System.Collections;
using UnityEngine;

namespace CardGame
{
    public class CoinFlipManager : MonoBehaviour
    {
        public static CoinFlipManager Instance { get; private set; }
        
        [Header("Coin Sprites")]
        public Sprite coinFrontSprite;
        public Sprite coinBackSprite;
        
        [Header("Animation Controller")]
        [SerializeField] private RuntimeAnimatorController coinFlipAnimator;
        [SerializeField] private string animationName = "CoinFlip";
        
        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 2f;
        [SerializeField] private int animationPlayCount = 3;
        [SerializeField] private float resultDisplayTime = 1.5f;
        
        [Header("Size Settings")]
        [SerializeField] private float animationSize = 1.5f;
        [SerializeField] private float resultSize = 1.5f;
        [SerializeField] private float coinSpriteSize = 1f;
        
        [Header("Visual Settings")]
        [SerializeField] private int sortingOrder = 1000;
        [SerializeField] private string sortingLayer = "UI";
        
        private SpriteRenderer coinRenderer;
        private Animator coinAnimator;
        private GameObject coinObject;
        private bool isFlipping = false;
        
        public System.Action<bool> OnCoinFlipFinished;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateCoinObject();
        }
        
        private void CreateCoinObject()
        {
            coinObject = new GameObject("CoinFlip");
            coinObject.transform.SetParent(transform);
            
            coinRenderer = coinObject.AddComponent<SpriteRenderer>();
            coinRenderer.sortingOrder = sortingOrder;
            coinRenderer.sortingLayerName = sortingLayer;
            coinRenderer.enabled = false;
            
            coinAnimator = coinObject.AddComponent<Animator>();
            if (coinFlipAnimator != null)
            {
                coinAnimator.runtimeAnimatorController = coinFlipAnimator;
            }
        }
        
        public void StartCoinFlip()
        {
            if (isFlipping) return;
            
            StartCoroutine(CoinFlipRoutine());
        }
        
        private IEnumerator CoinFlipRoutine()
        {
            isFlipping = true;
            
            coinObject.transform.position = Vector3.zero;
            coinObject.transform.rotation = Quaternion.identity;
            coinObject.transform.localScale = Vector3.one * animationSize;
            coinRenderer.enabled = true;
            
            yield return StartCoroutine(PlayAnimatorAnimation());
            yield return StartCoroutine(ShowRandomResult());
            
            isFlipping = false;
        }
        
        private IEnumerator PlayAnimatorAnimation()
        {
            if (coinAnimator != null && coinFlipAnimator != null)
            {
                coinAnimator.Rebind();
                coinAnimator.enabled = true;
                
                for (int i = 0; i < animationPlayCount; i++)
                {
                    if (!string.IsNullOrEmpty(animationName))
                    {
                        coinAnimator.Play(animationName, -1, 0f);
                    }
                    else
                    {
                        coinAnimator.Play(0, -1, 0f);
                    }
                    
                    coinObject.transform.localScale = Vector3.one * animationSize;
                    yield return new WaitForSeconds(animationDuration);
                    
                    if (i < animationPlayCount - 1)
                    {
                        yield return new WaitForSeconds(0.2f);
                    }
                }
                
                coinAnimator.enabled = false;
            }
            else
            {
                yield return StartCoroutine(PlaySimpleFlipAnimation());
            }
        }
        
        private IEnumerator PlaySimpleFlipAnimation()
        {
            float duration = animationDuration * animationPlayCount;
            float elapsed = 0f;
            int totalFlips = 8 * animationPlayCount;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                bool showFront = (Mathf.FloorToInt(progress * totalFlips) % 2 == 0);
                coinRenderer.sprite = showFront ? coinFrontSprite : coinBackSprite;
                
                float rotation = progress * 720f * animationPlayCount;
                coinObject.transform.rotation = Quaternion.Euler(0, 0, rotation);
                
                float scale = (animationSize - 0.5f) + Mathf.Sin(progress * Mathf.PI * 4) * 0.3f;
                coinObject.transform.localScale = Vector3.one * scale;
                
                yield return null;
            }
            
            coinObject.transform.rotation = Quaternion.identity;
            coinObject.transform.localScale = Vector3.one * animationSize;
        }
        
        private IEnumerator ShowRandomResult()
        {
            bool isFront = Random.Range(0, 2) == 0;
            Sprite resultSprite = isFront ? coinFrontSprite : coinBackSprite;
            
            coinRenderer.sprite = resultSprite;
            coinObject.transform.localScale = Vector3.one * resultSize * coinSpriteSize;
            
            yield return new WaitForSeconds(resultDisplayTime);
            
            coinRenderer.enabled = false;
            OnCoinFlipFinished?.Invoke(isFront);
        }
        
        public void SetCoinSprites(Sprite front, Sprite back)
        {
            coinFrontSprite = front;
            coinBackSprite = back;
        }
        
        public bool IsFlipping => isFlipping;
        
        public void StopAnimation()
        {
            if (isFlipping)
            {
                StopAllCoroutines();
                if (coinAnimator != null)
                {
                    coinAnimator.enabled = false;
                }
                coinRenderer.enabled = false;
                isFlipping = false;
            }
        }
        
        public void Cleanup()
        {
            if (coinObject != null)
            {
                Destroy(coinObject);
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            Cleanup();
        }
    }
}