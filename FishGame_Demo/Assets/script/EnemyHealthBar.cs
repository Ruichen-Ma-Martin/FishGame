using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 敌人头顶的血条。挂在敌人根物体上，血条 UI 是它的子物体，
// 所以"跟随敌人移动"由父子关系自动完成，不需要任何跟随代码。
// 敌人转向用的是 SpriteRenderer.flipX 而不是翻转缩放，因此血条不会被镜像
public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI 元素")]
    [SerializeField] private GameObject _barRoot;          // 整条血条的根，显示 / 隐藏时整体开关
    [SerializeField] private Image _fill;                  // Filled 型 Image，靠 fillAmount 表现血量
    [SerializeField] private TextMeshProUGUI _healthText;  // 可选：血量数字，留空就只显示条

    [Header("显示时机")]
    [SerializeField] private bool _alwaysVisible;          // 勾上则常显，不再自动隐藏
    [SerializeField] private float _hideDelay = 3f;        // 受伤后多久没再受伤就隐藏（秒）

    private enemy _enemy;
    private float _lastSeenHealth;   // 上一帧的血量，用来判断"这一帧是否掉血"
    private float _hideTimer;        // 距离隐藏还剩多久
    private bool _isInitialized;

    // 血条和 enemy 挂在同一个物体上，这里直接取，不用在 Inspector 里再连一次
    private void Awake()
    {
        if (!TryGetComponent(out _enemy))
        {
            Debug.LogError("EnemyHealthBar 必须和 enemy 挂在同一个物体上，否则读不到血量。脚本已停用。", this);
            enabled = false;
        }
    }

    // 放在 LateUpdate：敌人的位移在 FixedUpdate 里完成，这里刷新拿到的是本帧最终状态
    private void LateUpdate()
    {
        // 同一物体上各组件的 Start 执行顺序不保证，enemy.Start 可能还没跑就轮到血条，
        // 那时上限还是 0。所以初始血量放到第一次 LateUpdate 再取，此时所有 Start 都已结束
        if (!_isInitialized)
        {
            _lastSeenHealth = _enemy.CurrentHealth;
            SetVisible(_alwaysVisible);
            _isInitialized = true;
        }

        RefreshBar();
        UpdateVisibility();
        KeepUpright();
    }

    // 按当前血量刷新填充比例和数字
    private void RefreshBar()
    {
        float max = Mathf.Max(0.001f, _enemy.MaxHealth);      // 兜底防止上限为 0 时除零
        float current = Mathf.Max(0f, _enemy.CurrentHealth);  // 致死那一击会让血量变负，显示按 0 算

        if (_fill != null)
        {
            _fill.fillAmount = Mathf.Clamp01(current / max);
        }

        if (_healthText != null)
        {
            // "0.#" 格式：整数血量显示成 3，小数显示成 2.5，不会甩出一串 0
            _healthText.text = $"{current:0.#} / {_enemy.MaxHealth:0.#}";
        }
    }

    // 掉血就显示血条并重置计时；超过 _hideDelay 没再掉血就隐藏，让画面清爽些
    private void UpdateVisibility()
    {
        // 用"血量比上一帧低"来判断受伤，好处是不需要 enemy 额外派事件，
        // 而且以后给敌人加治疗 / 吸血也能自动正确响应
        if (_enemy.CurrentHealth < _lastSeenHealth)
        {
            SetVisible(true);
            _hideTimer = _hideDelay;
        }

        _lastSeenHealth = _enemy.CurrentHealth;

        if (_alwaysVisible)
        {
            return;
        }

        if (_hideTimer > 0f)
        {
            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0f)
            {
                SetVisible(false);
            }
        }
    }

    // 血条始终保持水平：万一敌人被互相弹开的碰撞推得旋转，血条不会跟着歪
    private void KeepUpright()
    {
        if (_barRoot != null)
        {
            _barRoot.transform.rotation = Quaternion.identity;
        }
    }

    // 整条血条的显示开关
    private void SetVisible(bool visible)
    {
        if (_barRoot != null && _barRoot.activeSelf != visible)
        {
            _barRoot.SetActive(visible);
        }
    }
}
