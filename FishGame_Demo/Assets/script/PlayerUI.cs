using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 玩家 HUD：每帧把血量、体力、SAN、血肉四项数值刷到屏幕 UI 上。
// 这里只负责"读数值 -> 画界面"，不持有任何游戏状态，也不修改玩家数据
public class PlayerUI : MonoBehaviour
{
    [Header("UI 元素")]
    [SerializeField] private Image _healthBarFill;       // 血条填充（Filled 型 Image）
    [SerializeField] private Image _staminaBarFill;      // 体力条填充
    [SerializeField] private Image _sanBarFill;          // SAN 条填充
    [SerializeField] private TextMeshProUGUI _healthText; // 血条上的数字，形如 "3 / 5"
    [SerializeField] private TextMeshProUGUI _fleshText; // 血肉数字

    [Header("数据来源")]
    [SerializeField] private player _player;                // 血量 / 血肉 / SAN
    [SerializeField] private playerController _controller;   // 体力

    void Awake()
    {
        // 引用漏连是最常见的配置失误。等到 Update 里才空引用会每帧刷满 Console，
        // 所以在这里一次性报清楚具体缺哪个槽位
        if (_player == null)
        {
            Debug.LogError("PlayerUI 的 _player 没有赋值，血量 / 血肉 / SAN 不会更新。请在 Inspector 里把玩家物体拖进来。", this);
        }

        if (_controller == null)
        {
            Debug.LogError("PlayerUI 的 _controller 没有赋值，体力条不会更新。请在 Inspector 里把带 playerController 的物体拖进来。", this);
        }
    }

    void Update()
    {
        RefreshPlayerBars();
        RefreshStaminaBar();
    }

    // 刷新玩家自身的三项：血条、SAN 条、血肉数字，数据都来自 player
    void RefreshPlayerBars()
    {
        if (_player == null)
        {
            return;
        }

        if (_healthBarFill != null)
        {
            _healthBarFill.fillAmount = ToFillAmount(_player.CurrentHealth, _player.MaxHealth);
        }

        if (_healthText != null)
        {
            // "0.#" 格式：整数血量显示成 5，半血之类的小数显示成 3.5，不会甩出一串 0。
            // 显示值夹到 0 起步，因为死亡到销毁之间有 0.2 秒延迟，这期间血量是负的
            float shownHealth = Mathf.Max(0f, _player.CurrentHealth);
            _healthText.text = $"{shownHealth:0.#} / {_player.MaxHealth:0.#}";
        }

        if (_sanBarFill != null)
        {
            _sanBarFill.fillAmount = ToFillAmount(_player.CurrentSan, _player.MaxSan);
        }

        if (_fleshText != null)
        {
            _fleshText.text = _player.CurrentFlesh.ToString();
        }
    }

    // 刷新体力条：体力是移动逻辑的一部分，存在 playerController 上而不是 player 上
    void RefreshStaminaBar()
    {
        if (_controller == null || _staminaBarFill == null)
        {
            return;
        }

        _staminaBarFill.fillAmount = ToFillAmount(_controller.CurrentStamina, _controller.MaxStamina);
    }

    // 把"当前值 / 上限"换成 0~1 的填充比例。
    // 分母兜底防止上限为 0 时除零；夹到 0~1 是因为血量可能超过上限（治疗没有封顶）
    // 或短暂为负（死亡到销毁之间有 0.2 秒延迟）
    static float ToFillAmount(float current, float max)
    {
        return Mathf.Clamp01(current / Mathf.Max(0.001f, max));
    }
}
