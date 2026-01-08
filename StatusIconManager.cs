using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BattleSystem
{
    public class StatusIconManager : MonoBehaviour
    {
        [Header("UI引用")]
        public RectTransform statusIconContainer;
        public GameObject statusIconPrefab;
        public GameObject statusTooltipPrefab;

        [Header("图标设置")]
        public Sprite burnIcon;
        public Sprite freezeIcon;
        public Sprite paralyzeIcon;
        public Sprite poisonIcon;
        public Sprite blindIcon;
        public Sprite confusionIcon;
        public Sprite parasiticIcon;
        public Sprite stunIcon;

        [Header("布局设置")]
        public float iconSpacing = 5f;
        public int maxIconsPerRow = 5;
        public float iconSize = 40f; // 图标大小

        private Dictionary<PetEntity, RectTransform> _petIconContainers = new Dictionary<PetEntity, RectTransform>();
        private Dictionary<PetEntity, List<StatusIconUI>> _petStatusIcons = new Dictionary<PetEntity, List<StatusIconUI>>();
        private Dictionary<StatusCondition, Sprite> _statusSprites = new Dictionary<StatusCondition, Sprite>();

        void Awake()
        {
            InitializeStatusSprites();
            SubscribeToEvents();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeStatusSprites()
        {
            _statusSprites[StatusCondition.Burn] = burnIcon;
            _statusSprites[StatusCondition.Freeze] = freezeIcon;
            _statusSprites[StatusCondition.Paralyze] = paralyzeIcon;
            _statusSprites[StatusCondition.Poison] = poisonIcon;
            _statusSprites[StatusCondition.Blind] = blindIcon;
            _statusSprites[StatusCondition.Confusion] = confusionIcon;
            _statusSprites[StatusCondition.Parasitic] = parasiticIcon;
            _statusSprites[StatusCondition.Stun] = stunIcon;
        }

        private void SubscribeToEvents()
        {
            BattleEvents.OnStatusEffectApplied += OnStatusEffectApplied;
            BattleEvents.OnStatusEffectRemoved += OnStatusEffectRemoved;
            BattleEvents.OnStatusEffectUpdated += OnStatusEffectUpdated;
            BattleEvents.OnPetDeath += OnPetDeath;
        }

        private void UnsubscribeFromEvents()
        {
            BattleEvents.OnStatusEffectApplied -= OnStatusEffectApplied;
            BattleEvents.OnStatusEffectRemoved -= OnStatusEffectRemoved;
            BattleEvents.OnStatusEffectUpdated -= OnStatusEffectUpdated;
            BattleEvents.OnPetDeath -= OnPetDeath;
        }

        private void OnStatusEffectApplied(PetEntity pet, StatusEffect effect)
        {
            AddOrUpdateStatusIcon(pet, effect);
        }

        private void OnStatusEffectRemoved(PetEntity pet, StatusCondition condition)
        {
            RemoveStatusIcon(pet, condition);
        }

        private void OnStatusEffectUpdated(PetEntity pet, StatusEffect effect)
        {
            UpdateStatusIcon(pet, effect);
        }

        private void OnPetDeath(PetEntity pet)
        {
            ClearPetStatusIcons(pet);
        }

        private void AddOrUpdateStatusIcon(PetEntity pet, StatusEffect effect)
        {
            if (pet == null || effect == null) return;

            // 为每个宠物创建独立的图标容器
            if (!_petIconContainers.ContainsKey(pet))
            {
                CreatePetIconContainer(pet);
            }

            // 确保宠物图标列表存在
            if (!_petStatusIcons.ContainsKey(pet))
            {
                _petStatusIcons[pet] = new List<StatusIconUI>();
            }

            List<StatusIconUI> petIcons = _petStatusIcons[pet];

            // 检查是否已存在该状态图标
            StatusIconUI existingIcon = petIcons.Find(icon => icon.condition == effect.condition);

            if (existingIcon != null)
            {
                // 更新现有图标
                existingIcon.UpdateStatus(effect.remainingTurns, effect.stackCount);
            }
            else
            {
                // 创建新图标
                RectTransform container = _petIconContainers[pet];
                if (container == null)
                {
                    CreatePetIconContainer(pet);
                    container = _petIconContainers[pet];
                }

                GameObject iconObj = Instantiate(statusIconPrefab, container);
                StatusIconUI newIcon = iconObj.GetComponent<StatusIconUI>();

                if (newIcon != null)
                {
                    // 设置图标大小
                    RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                    if (iconRect != null)
                    {
                        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
                    }

                    // 设置图标
                    Image iconImage = iconObj.GetComponent<Image>();
                    if (iconImage != null && _statusSprites.ContainsKey(effect.condition))
                    {
                        iconImage.sprite = _statusSprites[effect.condition];
                    }

                    // 设置工具提示预制体
                    newIcon.tooltipPrefab = statusTooltipPrefab;

                    // 初始化图标
                    newIcon.Initialize(effect.condition, effect.remainingTurns, effect.stackCount, pet);

                    petIcons.Add(newIcon);
                }
            }

            // 重新排列图标
            RearrangeIcons(pet);
        }

        private void CreatePetIconContainer(PetEntity pet)
        {
            // 创建宠物的图标容器
            GameObject containerObj = new GameObject($"{pet.petName}_StatusIcons");
            RectTransform containerRect = containerObj.AddComponent<RectTransform>();

            // 设置为statusIconContainer的子物体
            containerRect.SetParent(statusIconContainer);
            containerRect.localScale = Vector3.one;
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(0, 0);
            containerRect.pivot = new Vector2(0, 1);

            _petIconContainers[pet] = containerRect;

            // 设置初始位置（在宠物上方）
            UpdateIconContainerPosition(pet);
        }

        private void UpdateIconContainerPosition(PetEntity pet)
        {
            if (!_petIconContainers.ContainsKey(pet)) return;

            RectTransform containerRect = _petIconContainers[pet];
            RectTransform petRect = pet.GetComponent<RectTransform>();

            if (petRect != null)
            {
                // 将宠物位置转换到容器坐标系
                Vector3 petPosition = petRect.position;
                Vector3 screenPosition = RectTransformUtility.WorldToScreenPoint(null, petPosition);
                Vector2 localPoint;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    statusIconContainer,
                    screenPosition,
                    null,
                    out localPoint))
                {
                    // 放在宠物血条上方
                    containerRect.anchoredPosition = localPoint + new Vector2(0, 50);
                }
            }
        }

        private void UpdateStatusIcon(PetEntity pet, StatusEffect effect)
        {
            if (pet == null || effect == null || !_petStatusIcons.ContainsKey(pet)) return;

            List<StatusIconUI> petIcons = _petStatusIcons[pet];
            StatusIconUI existingIcon = petIcons.Find(icon => icon.condition == effect.condition);

            if (existingIcon != null)
            {
                existingIcon.UpdateStatus(effect.remainingTurns, effect.stackCount);
            }
        }

        private void RemoveStatusIcon(PetEntity pet, StatusCondition condition)
        {
            if (pet == null || !_petStatusIcons.ContainsKey(pet)) return;

            List<StatusIconUI> petIcons = _petStatusIcons[pet];
            StatusIconUI iconToRemove = petIcons.Find(icon => icon.condition == condition);

            if (iconToRemove != null)
            {
                petIcons.Remove(iconToRemove);
                Destroy(iconToRemove.gameObject);
            }

            // 重新排列图标
            RearrangeIcons(pet);

            // 如果没有图标了，清理容器
            if (petIcons.Count == 0 && _petIconContainers.ContainsKey(pet))
            {
                Destroy(_petIconContainers[pet].gameObject);
                _petIconContainers.Remove(pet);
                _petStatusIcons.Remove(pet);
            }
        }

        private void ClearPetStatusIcons(PetEntity pet)
        {
            if (pet == null || !_petStatusIcons.ContainsKey(pet)) return;

            List<StatusIconUI> petIcons = _petStatusIcons[pet];
            foreach (var icon in petIcons)
            {
                if (icon != null)
                {
                    Destroy(icon.gameObject);
                }
            }

            if (_petIconContainers.ContainsKey(pet))
            {
                Destroy(_petIconContainers[pet].gameObject);
                _petIconContainers.Remove(pet);
            }

            _petStatusIcons.Remove(pet);
        }

        private void RearrangeIcons(PetEntity pet)
        {
            if (!_petIconContainers.ContainsKey(pet) || !_petStatusIcons.ContainsKey(pet)) return;

            List<StatusIconUI> petIcons = _petStatusIcons[pet];
            RectTransform containerRect = _petIconContainers[pet];

            // 更新容器位置
            UpdateIconContainerPosition(pet);

            // 排列图标
            for (int i = 0; i < petIcons.Count; i++)
            {
                if (petIcons[i] == null) continue;

                RectTransform iconRect = petIcons[i].GetComponent<RectTransform>();
                if (iconRect != null)
                {
                    // 计算行和列
                    int row = i / maxIconsPerRow;
                    int col = i % maxIconsPerRow;

                    // 计算位置
                    float xPos = col * (iconSize + iconSpacing);
                    float yPos = -row * (iconSize + iconSpacing);

                    iconRect.anchoredPosition = new Vector2(xPos, yPos);
                    iconRect.sizeDelta = new Vector2(iconSize, iconSize);
                }
            }
        }

        // 每帧更新图标容器位置（跟随宠物移动）
        void Update()
        {
            foreach (var pet in _petIconContainers.Keys)
            {
                if (pet != null && pet.gameObject.activeInHierarchy)
                {
                    UpdateIconContainerPosition(pet);
                    RearrangeIcons(pet);
                }
            }
        }
    }
}