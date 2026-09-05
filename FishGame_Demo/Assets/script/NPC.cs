using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class NPC : MonoBehaviour
{
    private bool _isDialogueStart = false;
    private bool _WaitForPlayerInput = false;
    public bool _isShopOpean =false;
    public Dialogue_SO _StartDialogue;
    public Dialogue_SO _refuseDialogue;
    private Dialogue_SO _currentDialogue;
    private int _currentline = 0;

    public GameObject _NPCCanvas;
    public GameObject _ShopUI;

    // 对话依赖改成 Inspector 注入，不再通过 GameController 中转
    [SerializeField] private DialogueUI _dialogueUI;
    [SerializeField] private player _player;
    [SerializeField] private playerController _playerController;

    private void Awake()
    {
        _NPCCanvas.SetActive(false);
        _currentDialogue = _StartDialogue;

        // 三个引用缺任何一个，对话流程都跑不通（测距、显示台词、锁玩家操作），
        // 与其等到玩家走近才空引用报错，不如开局就停掉并说清楚缺什么
        if (_dialogueUI == null || _player == null || _playerController == null)
        {
            Debug.LogError("NPC 的 _dialogueUI / _player / _playerController 没有全部赋值，请在 Inspector 里连线。脚本已停用。", this);
            enabled = false;
        }
    }

    private void Update()
    {
        checkDialogueStart();

    }

    
    void checkDialogueStart()
    {
        if(Vector2.Distance(transform.position, _player.transform.position) < 1.5f)
        {
            _NPCCanvas.SetActive(true);
            if (Input.GetKeyDown(KeyCode.E) && !_isDialogueStart&& !_isShopOpean)
            {
                _playerController.enabled = false;
                StartDialogue();
                
            }
            if (Input.GetMouseButtonDown(0) && !_WaitForPlayerInput && _isDialogueStart && !_isShopOpean)
            {
                StartDialogue();
            }
        }
        else
        {
            EndDialogue();
        }
    }

    
    private void StartDialogue()
    {
        _isDialogueStart = true;
        if(_currentline < _currentDialogue._lines.Length)
        {
            _dialogueUI.ShowDialogue(_currentDialogue._lines[_currentline]);
            _currentline++;
        }
        else if(_currentDialogue._playerReplyOptions != null && _currentDialogue._playerReplyOptions.Length > 0)
        {
            _dialogueUI.ShowAnswer(_currentDialogue._playerReplyOptions);
            _WaitForPlayerInput = true;
        }
        else if(_currentDialogue._isOpenShop == true)
        {
            _ShopUI.SetActive(true);
            _isShopOpean = true;
            EndDialogue() ;
        }
        else
        { 
            _playerController.enabled = true;
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        _NPCCanvas.SetActive(false);
        _isDialogueStart = false;
        _WaitForPlayerInput = false;
        _currentline = 0;
        _currentDialogue = _StartDialogue;
       
        _dialogueUI.DialogueHide();
    }

    public void AnswerSelection(int Option)
    {
       
        {
             _currentline = 0;
             _WaitForPlayerInput = false;
             _currentDialogue = _currentDialogue._npcReplies[Option];
             StartDialogue();
        }
           
        
    }

   
}
