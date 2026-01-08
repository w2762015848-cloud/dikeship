using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace BattleSystem
{
    [RequireComponent(typeof(PetEntity))]
    public class PetUI : MonoBehaviour
    {
        [Header("UI引用")]
        public Slider hpSlider;
        public Image hpFillImage;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI levelText;

        [Header("状态图标设置")]
        public Transform statusIconContainer; // 【拖拽】在UI里创建一个Horizontal Layout Group作为容器
        public GameObject statusIconPrefab;   // 【拖拽】你的 StatusIconUI 预制体

        [Header("状态图标精灵（可选）")]
        public Sprite poisonIcon;
        public Sprite burnIcon;
        public Sprite paralyzeIcon;
        public Sprite sleepIcon;
        public Sprite freezeIcon;
        public Sprite leechSeedIcon;
        public Sprite confusionIcon;

        [Header("颜色设置")]
        public Color hpHighColor = Color.green;
        public Color hpMediumColor = Color.yellow;
        public Color hpLowColor = Color.red;

        private PetEntity _petEntity;
        private Coroutine _hpAnimation;
        // 用一个字典来追踪当前显示的图标，方便删除
        private Dictionary<StatusCondition, StatusIconUI> _activeIcons = new Dictionary<StatusCondition, StatusIconUI>();

        void Awake()
        {
            _petEntity = GetComponent<PetEntity>();

            // 自动查找UI组件（如果未分配）
            if (hpSlider == null) hpSlider = GetComponentInChildren<Slider>();
            if (hpText == null) hpText = FindTextComponent("HP", "血");
            if (nameText == null) nameText = FindTextComponent("Name", "名字");

            // 如果没有指定状态图标容器，创建一个
            if (statusIconContainer == null)
            {
                GameObject container = new GameObject("StatusIcons");
                container.transform.SetParent(transform, false);

                // 设置容器的位置（在宠物上方）
                RectTransform rect = container.AddComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(0, 60f); // 在宠物上方60像素
                rect.sizeDelta = new Vector2(200, 40);

                // 添加布局组件
                HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.spacing = 5f;

                statusIconContainer = container.transform;
            }
        }

        void Start()
        {
            InitializeUI();
            SubscribeToEvents();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();

            // 清理所有状态图标
            foreach (var icon in _activeIcons.Values)
            {
                if (icon != null && icon.gameObject != null)
                {
                    Destroy(icon.gameObject);
                }
            }
            _activeIcons.Clear();
        }

        private TextMeshProUGUI FindTextComponent(params string[] keywords)
        {
            var texts = GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                foreach (var keyword in keywords)
                {
                    if (text.name.Contains(keyword))
                    {
                        return text;
                    }
                }
            }
            return null;
        }

        private void SubscribeToEvents()
        {
            BattleEvents.OnPetHPChanged += OnPetHPChanged;
            BattleEvents.OnPetUIUpdateNeeded += OnPetUIUpdateNeeded;
            BattleEvents.OnStatusEffectApplied += OnStatusEffectApplied;
            BattleEvents.OnStatusEffectRemoved += OnStatusEffectRemoved;
            BattleEvents.OnStatusEffectUpdated += OnStatusEffectUpdated;
        }

        private void UnsubscribeFromEvents()
        {
            BattleEvents.OnPetHPChanged -= OnPetHPChanged;
            BattleEvents.OnPetUIUpdateNeeded -= OnPetUIUpdateNeeded;
            BattleEvents.OnStatusEffectApplied -= OnStatusEffectApplied;
            BattleEvents.OnStatusEffectRemoved -= OnStatusEffectRemoved;
            BattleEvents.OnStatusEffectUpdated -= OnStatusEffectUpdated;
        }

        private void InitializeUI()
        {
            if (nameText != null && _petEntity != null)
            {
                nameText.text = _petEntity.petName;
            }

            if (hpSlider != null)
            {
                hpSlider.maxValue = _petEntity.MaxHP;
                hpSlider.value = _petEntity.CurrentHP;
            }

            UpdateHPDisplay();
        }

        private void OnPetHPChanged(PetEntity pet, int currentHP, int maxHP)
        {
            if (pet != _petEntity) return;
            UpdateHPDisplay();
        }

        private void OnPetUIUpdateNeeded(PetEntity pet)
        {
            if (pet != _petEntity) return;
            UpdateHPDisplay();
        }

        // 处理状态效果应用
        private void OnStatusEffectApplied(PetEntity pet, StatusEffect effect)
        {
            if (pet != _petEntity || effect == null)
            {
                Debug.Log($"不是当前宠物或effect为空: pet={pet?.petName}, _petEntity={_petEntity?.petName}");
                return;
            }

            Debug.Log($"OnStatusEffectApplied: {pet.petName} 获得状态 {effect.condition}");

            // 如果已经有这个图标了，就更新它
            if (_activeIcons.ContainsKey(effect.condition))
            {
                _activeIcons[effect.condition].UpdateStatus(effect.remainingTurns, 1);
                Debug.Log($"更新现有图标: {effect.condition}");
            }
            else
            {
                // 实例化新图标
                if (statusIconPrefab != null && statusIconContainer != null)
                {
                    GameObject iconObj = Instantiate(statusIconPrefab, statusIconContainer);
                    StatusIconUI iconUI = iconObj.GetComponent<StatusIconUI>();

                    if (iconUI == null)
                    {
                        Debug.LogError("状态图标预制体缺少 StatusIconUI 组件！");
                        Destroy(iconObj);
                        return;
                    }

                    // 初始化图标
                    iconUI.Initialize(effect.condition, effect.remainingTurns, 1, pet);

                    // 设置图标图片
                    SetStatusIconSprite(iconUI, effect.condition);

                    _activeIcons.Add(effect.condition, iconUI);
                    Debug.Log($"创建新图标: {effect.condition}, 当前图标数: {_activeIcons.Count}");
                }
                else
                {
                    Debug.LogWarning($"无法创建状态图标: prefab={statusIconPrefab}, container={statusIconContainer}");
                }
            }
        }

        // 处理状态效果移除
        private void OnStatusEffectRemoved(PetEntity pet, StatusCondition condition)
        {
            if (pet != _petEntity)
            {
                Debug.Log($"不是当前宠物: pet={pet?.petName}, _petEntity={_petEntity?.petName}");
                return;
            }

            Debug.Log($"OnStatusEffectRemoved: {pet.petName} 移除状态 {condition}");

            if (_activeIcons.ContainsKey(condition))
            {
                // 销毁图标对象
                if (_activeIcons[condition] != null)
                {
                    Destroy(_activeIcons[condition].gameObject);
                }
                _activeIcons.Remove(condition);
                Debug.Log($"移除图标: {condition}, 剩余图标数: {_activeIcons.Count}");
            }
        }

        // 处理状态效果更新
        private void OnStatusEffectUpdated(PetEntity pet, StatusEffect effect)
        {
            if (pet != _petEntity) return;

            if (_activeIcons.ContainsKey(effect.condition))
            {
                _activeIcons[effect.condition].UpdateStatus(effect.remainingTurns, 1);
                Debug.Log($"更新图标: {effect.condition}, 剩余回合: {effect.remainingTurns}");
            }
        }

        // 辅助方法：根据状态条件设置图标（修复版 - 不会编译错误）
        private void SetStatusIconSprite(StatusIconUI iconUI, StatusCondition condition)
        {
            if (iconUI == null || iconUI.statusIcon == null)
            {
                Debug.LogWarning("StatusIconUI 或 statusIcon 为空");
                return;
            }

            // 检查精灵是否在Inspector中手动设置
            Sprite iconSprite = null;

            // 使用switch语句，但只包含你项目中实际存在的枚举值
            switch (condition)
            {
                case StatusCondition.Poison:
                    iconSprite = poisonIcon;
                    break;
                case StatusCondition.Burn:
                    iconSprite = burnIcon;
                    break;
                case StatusCondition.Paralyze:
                    iconSprite = paralyzeIcon;
                    break;
                    // 只保留你项目中实际存在的枚举值
                    // 如果不确定，可以逐个添加
            }

            // 如果Inspector中没有设置，尝试从Resources加载
            if (iconSprite == null)
            {
                string spriteName = GetStatusIconName(condition);
                if (!string.IsNullOrEmpty(spriteName))
                {
                    iconSprite = Resources.Load<Sprite>($"StatusIcons/{spriteName}");
                }
            }

            // 设置图标
            if (iconSprite != null)
            {
                iconUI.statusIcon.sprite = iconSprite;
            }
            else
            {
                Debug.LogWarning($"未找到状态条件 {condition} 的图标");
                // 设置一个默认图标
                iconUI.statusIcon.color = Color.red;
            }
        }

        // 辅助方法：根据状态条件获取图标名称
        private string GetStatusIconName(StatusCondition condition)
        {
            return condition.ToString();
        }

        private void UpdateHPDisplay()
        {
            // 更新HP文本
            if (hpText != null)
            {
                hpText.text = $"{_petEntity.CurrentHP}/{_petEntity.MaxHP}";
            }

            // 更新HP条颜色
            UpdateHPBarColor();

            // 动画更新HP条
            if (hpSlider != null)
            {
                if (_hpAnimation != null)
                {
                    StopCoroutine(_hpAnimation);
                }
                _hpAnimation = StartCoroutine(AnimateHPBar(_petEntity.CurrentHP));
            }
        }

        private System.Collections.IEnumerator AnimateHPBar(float targetValue)
        {
            if (hpSlider == null) yield break;

            float startValue = hpSlider.value;
            float elapsed = 0f;
            float duration = BattleConstants.HP_ANIMATION_DURATION;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                hpSlider.value = Mathf.Lerp(startValue, targetValue, t);
                yield return null;
            }

            hpSlider.value = targetValue;
            _hpAnimation = null;
        }

        private void UpdateHPBarColor()
        {
            if (hpFillImage == null || _petEntity.MaxHP <= 0) return;

            float hpPercent = (float)_petEntity.CurrentHP / _petEntity.MaxHP;

            if (hpPercent <= 0.3f)
                hpFillImage.color = hpLowColor;
            else if (hpPercent <= 0.6f)
                hpFillImage.color = hpMediumColor;
            else
                hpFillImage.color = hpHighColor;
        }

        void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && hpSlider != null)
            {
                var pet = GetComponent<PetEntity>();
                if (pet != null)
                {
                    hpSlider.maxValue = pet.MaxHP;
                    hpSlider.value = pet.CurrentHP;
                    UpdateHPBarColor();
                }
            }
#endif
        }
    }
}