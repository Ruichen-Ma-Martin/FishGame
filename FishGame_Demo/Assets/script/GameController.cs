using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

// 场景流程控制：暂停、重开、回主菜单。
// 这里刻意不再持有 player / weapon / NPC 等对象引用 —— 那些依赖已经改由各脚本
// 自己用 [SerializeField] 注入或通过静态事件解耦，避免这个类退化成什么都装的 god object
public class GameController : MonoBehaviour
{
    public static GameController instance { get; private set; }

    private bool _isGameStopped = false;
    // 暂停面板。改名后加 FormerlySerializedAs，保住场景里原本的连线不丢
    [SerializeField, FormerlySerializedAs("GameStopHUD")] private GameObject _gameStopHUD;

    private void Awake()
    {
        // 单例初始化：必须先判空再赋值。如果先写 instance = this，判断就永远不成立，
        // 重复的 GameController 不会被销毁，后来的实例还会顶掉正确的那个引用
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    // 死亡检测改成监听事件：不再每帧轮询 player 是否变成 null，
    // 也就不需要"开局有没有 player"这种兜底判断（原来那个判断是为了防止场景无限重载）
    private void OnEnable()
    {
        player.OnPlayerDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        player.OnPlayerDeath -= HandlePlayerDeath;
    }

    private void Update()
    {
        Stopgame();
    }

    // 玩家死亡后的场景流程：回主菜单
    private void HandlePlayerDeath()
    {
        BackToMain();
    }

    public void RestartCurrentScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    void Stopgame()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _isGameStopped = !_isGameStopped;
            // 没连暂停面板也要保证 timeScale 能恢复，不然会卡在 0 让整个游戏静止
            if (_gameStopHUD != null)
            {
                _gameStopHUD.SetActive(_isGameStopped);
            }
            Time.timeScale = _isGameStopped ? 0f : 1f;
        }
    }

    public void BackToMain()
    {
        SceneManager.LoadScene("Main");
        Time.timeScale = 1f;
    }
}
