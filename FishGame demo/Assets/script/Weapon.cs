using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private float _angleDistance = 10f;
    [SerializeField] private GameObject shootPointBag;
    [SerializeField] private GameObject _bullet;

    public int _currentlevel = 1;
    public List<Transform> shootPoints = new List<Transform>();

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

    public void Shoot()
    {
        // 以鼠标的世界坐标作为瞄准基准，保证子弹朝鼠标方向飞，而不是固定朝上
        Vector3 mouseWorldPos = Camera.main != null
            ? Camera.main.ScreenToWorldPoint(Input.mousePosition)
            : transform.position + transform.up;
        mouseWorldPos.z = 0f;

        foreach (var point in shootPoints)
        {
            Vector2 aimDirection = mouseWorldPos - point.position;
            // 多发升级时每个射击点带有 Z 角偏移，把它叠加到鼠标方向上，保留扇形散射
            Vector2 finalDirection = RotateVector(aimDirection, point.localEulerAngles.z);

            GameObject newBullet = Instantiate(_bullet, point.position, point.rotation);
            bullet bulletScript = newBullet.GetComponent<bullet>();
            if (bulletScript != null)
            {
                bulletScript.Launch(finalDirection);
            }
        }
    }

    // 把二维向量绕原点旋转指定角度（度），用于在鼠标方向基础上加散射角
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

        for (int i = 0; i < level; i++)
        {
            GameObject newshootpoint = new GameObject("shootPoint_" + (i + 1));
            newshootpoint.transform.parent = shootPointBag.transform;
            newshootpoint.transform.localPosition = Vector3.zero;
            float zRot = (i - (level - 1) / 2f) * _angleDistance;
            newshootpoint.transform.localEulerAngles = new Vector3(0, 0, zRot);
            shootPoints.Add(newshootpoint.transform);
        }
    }
}
