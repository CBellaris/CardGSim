using System.Collections;
using UnityEngine;
using TMPro; // Unity的现代文本渲染系统，强烈建议取代旧版Text
using Cards.Data;
using Cards.Core.Events;
using Cards.Services;

namespace Cards.Core
{
    public class CardEntityView : MonoBehaviour
    {
        public CardInstance Card { get; private set; }
        public CardModel Model => Card?.Model;
        public CardOwner Owner => Card != null ? Card.Owner : CardOwner.Neutral;

        public CardData CurrentCardData => Card?.Data as CardData;

        [Header("3D Visuals References")]
        [SerializeField] private MeshRenderer cardMeshRenderer; // 用于替换卡面材质
        [SerializeField] private TextMeshProUGUI nameText;      // 世界空间里的UI文本
        [SerializeField] private TextMeshProUGUI attackText;
        [SerializeField] private TextMeshProUGUI healthText;

        private Coroutine moveCoroutine;
        private GameContext context;

        public void SetupCard(CardInstance card, GameContext gameContext = null)
        {
            if (Model != null)
            {
                Model.OnHealthChanged -= UpdateHealthVisuals;
            }

            Card = card;
            context = gameContext;

            if (Model != null)
            {
                Model.OnHealthChanged += UpdateHealthVisuals;
            }

            UpdateVisuals();
        }

        private void UpdateHealthVisuals(int newHealth)
        {
            if (healthText != null)
            {
                healthText.text = newHealth.ToString();
            }
        }

        private void UpdateVisuals()
        {
            if (Card?.Data == null)
            {
                return;
            }

            nameText.text = Card.Data.CardName;
            attackText.text = Card.Data.Attack.ToString();
            healthText.text = Model != null ? Model.CurrentHealth.ToString() : "0";

            CardData currentData = CurrentCardData;
            if (currentData != null && currentData.CardArtMaterial != null && cardMeshRenderer != null)
            {
                cardMeshRenderer.material = currentData.CardArtMaterial;
            }
        }

        /// <summary>
        /// 使用协程实现平滑移动和旋转
        /// </summary>
        public void MoveTo(Vector3 targetPosition, Quaternion targetRotation, float duration = 0.3f)
        {
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
            }

            if (gameObject.activeInHierarchy)
            {
                moveCoroutine = StartCoroutine(MoveCoroutine(targetPosition, targetRotation, duration));
            }
            else
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
        }

        private IEnumerator MoveCoroutine(Vector3 targetPosition, Quaternion targetRotation, float duration)
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = Mathf.SmoothStep(0f, 1f, t);

                transform.position = Vector3.Lerp(startPos, targetPosition, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRotation, t);

                yield return null;
            }

            transform.position = targetPosition;
            transform.rotation = targetRotation;
            moveCoroutine = null;
        }

        private void OnMouseDown()
        {
            if (Card != null)
            {
                context?.Events?.Publish(new CardClickedEvent { Card = Card });
            }
        }

        private void OnDestroy()
        {
            if (Model != null)
            {
                Model.OnHealthChanged -= UpdateHealthVisuals;
            }
        }
    }
}
