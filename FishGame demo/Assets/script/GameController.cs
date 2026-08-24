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
        instance = this;
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
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
