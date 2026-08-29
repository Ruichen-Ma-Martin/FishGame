using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 摄像机跟随玩家，并用墙围成的关卡内边把画面夹住，永远不拍到墙外
public class cameraController : MonoBehaviour
{
    public Transform player;   // 跟随目标：玩家
    // 平滑时间从这里读，需在 Inspector 里挂上 LevelConfig 资源
    [SerializeField] private LevelConfig_SO _levelConfig;

    private Camera _cam;           // 本物体上的 Camera，用来算半屏宽高
    private Vector3 _velocity;     // SmoothDamp 用的速度缓存，运行时状态
    private float _minX, _maxX;    // 关卡内边：左右墙的内侧
    private float _minY, _maxY;    // 关卡内边：底/顶墙的内侧
    private bool _hasLevelBounds;  // 有没有成功从墙算出边界

    // 缓存摄像机和由墙围出的关卡内边
    private void Start()
    {
        _cam = GetComponent<Camera>();
        CacheLevelBounds();

        if (player != null)
        {
            Vector3 startPos = new Vector3(player.position.x, player.position.y, transform.position.z);
            transform.position = ClampToLevel(startPos);
        }
    }

    // 平滑跟随后再夹到关卡内，Z 始终保持原值
    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        Vector3 targetPos = player.position;
        targetPos.z = transform.position.z;  // 保持摄像机 Z 不变

        float smoothTime = _levelConfig != null ? _levelConfig.cameraSmoothTime : 0.2f;
        Vector3 smoothPos = Vector3.SmoothDamp(
            transform.position, targetPos, ref _velocity, smoothTime);

        transform.position = ClampToLevel(smoothPos);
    }

    // 查找所有 tag "wall" 的碰撞体，用它们的内侧围出可拍摄范围
    // 竖墙（更瘦）提供左右内边，横墙（更扁）提供上下内边；没有底墙时用竖墙的底边
    private void CacheLevelBounds()
    {
        GameObject[] walls = GameObject.FindGameObjectsWithTag("wall");
        if (walls == null || walls.Length == 0)
        {
            Debug.LogError("cameraController 找不到 tag 为 wall 的物体，摄像机将不夹边界。", this);
            _hasLevelBounds = false;
            return;
        }

        Collider2D leftWall = null;
        Collider2D rightWall = null;
        Collider2D topWall = null;
        Collider2D bottomWall = null;
        float unionMinX = float.PositiveInfinity;
        float unionMaxX = float.NegativeInfinity;
        float unionMinY = float.PositiveInfinity;
        float unionMaxY = float.NegativeInfinity;

        for (int i = 0; i < walls.Length; i++)
        {
            Collider2D col = walls[i].GetComponent<Collider2D>();
            if (col == null)
            {
                continue;
            }

            Bounds bounds = col.bounds;
            unionMinX = Mathf.Min(unionMinX, bounds.min.x);
            unionMaxX = Mathf.Max(unionMaxX, bounds.max.x);
            unionMinY = Mathf.Min(unionMinY, bounds.min.y);
            unionMaxY = Mathf.Max(unionMaxY, bounds.max.y);

            // 横向更长视为天花板/地面，否则视为左右竖墙（与昆虫边界判定一致）
            bool isHorizontal = bounds.size.x >= bounds.size.y;
            if (isHorizontal)
            {
                if (topWall == null || bounds.center.y > topWall.bounds.center.y)
                {
                    topWall = col;
                }
                if (bottomWall == null || bounds.center.y < bottomWall.bounds.center.y)
                {
                    bottomWall = col;
                }
            }
            else
            {
                if (leftWall == null || bounds.center.x < leftWall.bounds.center.x)
                {
                    leftWall = col;
                }
                if (rightWall == null || bounds.center.x > rightWall.bounds.center.x)
                {
                    rightWall = col;
                }
            }
        }

        // 内边：玩家活动空间在墙的内侧，摄像机也只允许拍到这里
        _minX = leftWall != null ? leftWall.bounds.max.x : unionMinX;
        _maxX = rightWall != null ? rightWall.bounds.min.x : unionMaxX;
        _maxY = topWall != null ? topWall.bounds.min.y : unionMaxY;
        // 只有一面横墙时它是天花板，底边改用竖墙包围盒，避免把天花板当成地板
        if (bottomWall != null && bottomWall != topWall)
        {
            _minY = bottomWall.bounds.max.y;
        }
        else
        {
            float sideFloor = unionMinY;
            if (leftWall != null)
            {
                sideFloor = Mathf.Min(sideFloor, leftWall.bounds.min.y);
            }
            if (rightWall != null)
            {
                sideFloor = Mathf.Min(sideFloor, rightWall.bounds.min.y);
            }
            _minY = sideFloor;
        }

        _hasLevelBounds = _maxX > _minX && _maxY > _minY;
        if (!_hasLevelBounds)
        {
            Debug.LogError("cameraController 从墙算出的关卡范围无效，摄像机将不夹边界。", this);
        }
    }

    // 把摄像机中心夹在关卡内：留出半屏，使画面边缘刚好贴在墙上
    // 关卡比画面还小时，min+半屏 会超过 max-半屏，此时把摄像机锁在关卡中心
    private Vector3 ClampToLevel(Vector3 position)
    {
        if (!_hasLevelBounds || _cam == null)
        {
            position.z = transform.position.z;
            return position;
        }

        float halfHeight = _cam.orthographicSize;
        float halfWidth = halfHeight * _cam.aspect;

        float minCamX = _minX + halfWidth;
        float maxCamX = _maxX - halfWidth;
        if (minCamX > maxCamX)
        {
            minCamX = maxCamX = (_minX + _maxX) * 0.5f;
        }

        float minCamY = _minY + halfHeight;
        float maxCamY = _maxY - halfHeight;
        if (minCamY > maxCamY)
        {
            minCamY = maxCamY = (_minY + _maxY) * 0.5f;
        }

        position.x = Mathf.Clamp(position.x, minCamX, maxCamX);
        position.y = Mathf.Clamp(position.y, minCamY, maxCamY);
        position.z = transform.position.z;
        return position;
    }
}
