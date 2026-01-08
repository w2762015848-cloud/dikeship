using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BattleSystem
{
    public class StatusIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI引用")]
        public Image statusIcon;
        public TextMeshProUGUI stackText;
        public GameObject tooltipPrefab;

        [Header("状态设置")]
        public StatusCondition condition;
        public int remainingTurns;
        public int stackCount = 1;

        private GameObject _tooltipInstance;
        private PetEntity _ownerPet;
        private Canvas _rootCanvas; // 存储根画布引用

        void Awake()
        {
            // 查找根画布
            if (_rootCanvas == null)
            {
                Transform current = transform;
                while (current != null)
                {
                    Canvas canvas = current.GetComponent<Canvas>();
                    if (canvas != null && canvas.isRootCanvas)
                    {
                        _rootCanvas = canvas;
                        break;
                    }
                    current = current.parent;
                }

                if (_rootCanvas == null)
                {
                    _rootCanvas = FindObjectOfType<Canvas>();
                }
            }
        }

        public void Initialize(StatusCondition condition, int remainingTurns, int stackCount, PetEntity owner)
        {
            this.condition = condition;
            this.remainingTurns = remainingTurns;
            this.stackCount = stackCount;
            _ownerPet = owner;

            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            // 更新层数显示
            if (stackCount > 1 && stackText != null)
            {
                stackText.text = stackCount.ToString();
                stackText.gameObject.SetActive(true);

                // 确保字体设置正确
                if (stackText.font == null)
                {
                    // 如果字体为空，尝试设置默认字体
                    TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                    if (defaultFont != null)
                    {
                        stackText.font = defaultFont;
                    }
                }
            }
            else if (stackText != null)
            {
                stackText.gameObject.SetActive(false);
            }
        }

        public void UpdateStatus(int newRemainingTurns, int newStackCount)
        {
            remainingTurns = newRemainingTurns;
            stackCount = newStackCount;
            UpdateDisplay();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideTooltip();
        }

        private void ShowTooltip()
        {
            if (tooltipPrefab == null || _ownerPet == null || condition == StatusCondition.None)
            {
                Debug.LogWarning("无法显示工具提示：缺少必要组件");
                return;
            }

            // 销毁现有工具提示
            if (_tooltipInstance != null)
            {
                Destroy(_tooltipInstance);
            }

            if (_rootCanvas == null)
            {
                Debug.LogWarning("无法找到根Canvas");
                return;
            }

            // 创建工具提示实例
            _tooltipInstance = Instantiate(tooltipPrefab, _rootCanvas.transform);
            _tooltipInstance.transform.SetAsLastSibling(); // 确保在最上层

            // 设置工具提示内容
            StatusTooltip tooltip = _tooltipInstance.GetComponent<StatusTooltip>();
            if (tooltip != null)
            {
                string statusName = StatusEffectConfig.GetDisplayName(condition);
                string description = GetStatusDescription(condition);
                string tooltipText = $"<b>{statusName}</b>\n{description}";

                if (remainingTurns > 0)
                {
                    tooltipText += $"\n剩余回合: {remainingTurns}";
                }

                if (stackCount > 1)
                {
                    tooltipText += $"\n层数: {stackCount}";
                }

                tooltip.SetText(tooltipText);

                // 设置位置 - 使用屏幕坐标
                Vector3 iconScreenPos = RectTransformUtility.WorldToScreenPoint(null, transform.position);
                Vector2 localPoint;

                RectTransform canvasRect = _rootCanvas.GetComponent<RectTransform>();
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    iconScreenPos,
                    null,
                    out localPoint))
                {
                    tooltip.SetPosition(localPoint + new Vector2(0, 60));
                }

                tooltip.Show();
            }

            Debug.Log($"显示状态工具提示: {condition}");
        }

        private string GetStatusDescription(StatusCondition condition)
        {
            return condition switch
            {
                StatusCondition.Burn => "每回合损失最大生命值的10%，攻击力降低10%",
                StatusCondition.Freeze => "无法行动",
                StatusCondition.Paralyze => "25%概率无法行动",
                StatusCondition.Poison => "每回合损失最大生命值的10%，治疗效果减半",
                StatusCondition.Blind => "命中率降低50%",
                StatusCondition.Confusion => "40%概率攻击自己",
                StatusCondition.Parasitic => "每回合损失最大生命值的8%，施放者获得等量治疗",
                StatusCondition.Stun => "无法行动",
                _ => "无效果"
            };
        }

        private void HideTooltip()
        {
            if (_tooltipInstance != null)
            {
                StatusTooltip tooltip = _tooltipInstance.GetComponent<StatusTooltip>();
                if (tooltip != null)
                {
                    tooltip.Hide();
                    // 等待淡出动画完成后销毁
                    Destroy(_tooltipInstance, 0.5f);
                }
                else
                {
                    Destroy(_tooltipInstance);
                }
                _tooltipInstance = null;
            }
        }

        void OnDisable()
        {
            HideTooltip();
        }

        void OnDestroy()
        {
            if (_tooltipInstance != null)
            {
                Destroy(_tooltipInstance);
            }
        }
    }
}