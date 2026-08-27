using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private float _angleDistance = 10f;
    [SerializeField] private GameObject shootPointBag;
    [SerializeField] private GameObject _bullet;

    public int _currentlevel = 1;
    public List<Transform> shootPoints = new List<Transform>();

    private bool _hasReportedMissingBullet;

    void Start()
    {
        // 按初始等级生成射击点
        UpdateShootingPoint(_currentlevel);
    }

    // 武器升级：等级 +1 并重新排布射击点，等级越高同时射出的子弹越多
    public void LevelUp()
    {
        _currentlevel++;
        UpdateShootingPoint(_currentlevel);
    }

    // 按调用者给定的方向开火：现在由玩家传入鱼头朝向，弹道和鱼头指向始终一致
    public void Shoot(Vector2 aimDirection)
    {
        if (_bullet == null)
        {
            if (!_hasReportedMissingBullet)
            {
                Debug.LogError("Weapon 的 _bullet 没有赋值，无法生成子弹。请在 Inspector 里挂上子弹预制体。", this);
                _hasReportedMissingBullet = true;
            }
            return;
        }

        // 射击点被外部清空或 Start 时生成失败，这里补建一次，免得点了没反应又没有任何提示
        if (shootPoints.Count == 0)
        {
            UpdateShootingPoint(_currentlevel);
        }

        // 方向为零时退回自身 up，避免子弹原地不动
        if (aimDirection.sqrMagnitude < 0.0001f)
        {
            aimDirection = transform.up;
        }

        foreach (var point in shootPoints)
        {
            // 多发升级时每个射击点带有 Z 角偏移，把它叠加到瞄准方向上，保留扇形散射
            Vector2 finalDirection = RotateVector(aimDirection, point.localEulerAngles.z);

            GameObject newBullet = Instantiate(_bullet, point.position, point.rotation);
            bullet bulletScript = newBullet.GetComponent<bullet>();
            if (bulletScript != null)
            {
                bulletScript.Launch(finalDirection);
            }
        }
    }

    // 把二维向量绕原点旋转指定角度（度），用于在瞄准方向基础上加散射角
    Vector2 RotateVector(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);
    }

    // 重建射击点：先清掉旧的，再按等级生成 level 个，以正前方为中心左右均匀展开成扇形
    void UpdateShootingPoint(int level)
    {
        foreach (var point in shootPoints)
        {
            Destroy(point.gameObject);
        }
        shootPoints.Clear();

        // 等级被填成 0 或负数时至少留一个射击点，否则武器会静默地一发都打不出来
        level = Mathf.Max(1, level);

        // 没指定挂载点就挂在武器自己身上，位置和朝向一样正确，不至于因为漏连线就完全打不出子弹
        Transform bag = shootPointBag != null ? shootPointBag.transform : transform;
        if (shootPointBag == null)
        {
            Debug.LogWarning("Weapon 的 shootPointBag 没有赋值，射击点暂时挂在武器自身上。", this);
        }

        for (int i = 0; i < level; i++)
        {
            GameObject newshootpoint = new GameObject("shootPoint_" + (i + 1));
            newshootpoint.transform.parent = bag;
            newshootpoint.transform.localPosition = Vector3.zero;
            float zRot = (i - (level - 1) / 2f) * _angleDistance;
            newshootpoint.transform.localEulerAngles = new Vector3(0, 0, zRot);
            shootPoints.Add(newshootpoint.transform);
        }
    }
}
