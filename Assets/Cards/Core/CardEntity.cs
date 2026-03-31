using System.Collections;
using UnityEngine;
using TMPro; // Unity的现代文本渲染系统，强烈建议取代旧版Text
using Cards.Data;
using Cards.Core.Events;
using Cards.Zones;

namespace Cards.Core
{
    public class CardEntity : MonoBehaviour
    {
        public CardModel Model { get; private set; }
        public CardOwner Owner => Model != null ? Model.Owner : CardOwner.Neutral;
        public CardZone CurrentZone { get; private set; }
        public ZoneId? CurrentZoneId { get; private set; }
        public string CurrentZoneName { get; private set; }

    // 兼容旧代码，提供对 Data 的快捷访问
    public CardData CurrentCardData => Model?.Data;

    [Header("3D Visuals References")]
    [SerializeField] private MeshRenderer cardMeshRenderer; // 用于替换卡面材质
    [SerializeField] private TextMeshProUGUI nameText;      // 世界空间里的UI文本
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI healthText;

    private Coroutine moveCoroutine;

    // 相当于 UE 的自定义初始化函数 (InitFromData)
    public void SetupCard(CardData data, CardOwner owner = CardOwner.Player)
    {
        // 1. 初始化纯数据模型
        Model = new CardModel(data, owner);
        
        // 2. 监听模型的事件以更新表现层
        Model.OnHealthChanged += UpdateHealthVisuals;
        Model.OnDied += HandleDeath;

        // --- 核心重构：移除旧版的 AddComponent 组件系统 ---
        // 现在的卡牌实体只有 Tag，具体的逻辑由 RuleEngine 读取 CardData 中的 Effect 数据来解析
        
        UpdateVisuals();
    }

    public void SetCurrentZone(CardZone zone)
    {
        CurrentZone = zone;
        CurrentZoneId = zone != null ? zone.ZoneId : (ZoneId?)null;
            CurrentZoneName = zone != null ? zone.ZoneName : null;
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
        if (Model == null || Model.Data == null) return;

        nameText.text = Model.Data.CardName;
        attackText.text = Model.Data.Attack.ToString();
        healthText.text = Model.CurrentHealth.ToString(); // 使用当前生命值

        // 为3D化准备：把卡牌模型的某个材质替换成卡牌原画
        if(Model.Data.CardArtMaterial != null && cardMeshRenderer != null)
        {
            // 注意：不要直接改 sharedMaterial，除非你想改所有同类物体。改 material 会生成材质实例(Instance)。
            cardMeshRenderer.material = Model.Data.CardArtMaterial; 
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
            // 如果物体没激活，直接赋值
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
            
            // 使用 SmoothStep 让动画有缓动效果 (Ease In Out)
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRotation, t);
            
            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
        moveCoroutine = null;
    }

    // 鼠标点击事件，需要卡牌上有 Collider 组件 (如 BoxCollider)
    private void OnMouseDown()
    {
        // 触发点击事件，通过全局事件总线广播
        EventBus.Publish(new CardClickedEvent { Card = this });
    }

    private void HandleDeath()
    {
        // 触发死亡事件，通过全局事件总线广播
        EventBus.Publish(new CardDiedEvent { Card = this });
    }

    private void OnDestroy()
    {
        if (Model != null)
        {
            Model.OnHealthChanged -= UpdateHealthVisuals;
            Model.OnDied -= HandleDeath;
        }
    }
}
}