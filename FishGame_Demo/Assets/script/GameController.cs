using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public static GameController instance { get; private set; }
    public bullet bullet;
    public enemyattack enemyattack;
    public player player;
    public Weapon weapon;
    public animationEvent animationEvent;
    public DialogueUI dialogueUI;
    public GetDamageEffect GetDamageEffect;
    public playerController playerController;
    public NPC NPC;
    //public enemy enemy;
    private bool isGameStopped = false;
    public GameObject GameStopHUD;
    // player 一开始就没连线时关掉死亡检测，否则 Update 会把场景无限重载
    private bool _canDetectPlayerDeath;
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
    private void Start()
    {
        // 死亡判定的依据是"开局有 player，中途变空"。如果开局就是空的，这个条件永远成立，
        // 场景会一帧一个地重载下去，所以先确认初始状态再决定要不要开启检测
        _canDetectPlayerDeath = player != null;
        if (!_canDetectPlayerDeath)
        {
            Debug.LogError("GameController 的 player 字段没有赋值，已关闭死亡重开检测（否则场景会无限重载）。", this);
        }
    }

    private void Update()
    {
        if (_canDetectPlayerDeath && player == null)
        {
            RestartCurrentScene();
            return;
        }
        Stopgame();
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
            isGameStopped = !isGameStopped;
            // 没连暂停面板也要保证 timeScale 能恢复，不然会卡在 0 让整个游戏静止
            if (GameStopHUD != null)
            {
                GameStopHUD.SetActive(isGameStopped);
            }
            Time.timeScale = isGameStopped ? 0f : 1f;
        }
    }
    public void BackToMain()
    {
        SceneManager.LoadScene("Main");
        Time.timeScale = 1f;
    }
}
