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
    private void Update()
    {
        if (player == null)
        {
            RestartCurrentScene();
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
            if (isGameStopped)
            {
                GameStopHUD.SetActive(true);
                Time.timeScale = 0f;
            }
            else
            {
                GameStopHUD.SetActive(false);
                Time.timeScale = 1f; 
            }
        }
    }
    public void BackToMain()
    {
        SceneManager.LoadScene("Main");
        Time.timeScale = 1f;
    }
}
