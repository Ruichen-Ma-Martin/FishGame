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
    private void Awake()
    {
        _NPCCanvas.SetActive(false);
        _currentDialogue = _StartDialogue;
    }

    private void Update()
    {
        checkDialogueStart();

    }

    
    void checkDialogueStart()
    {
        if(Vector2.Distance(transform.position, GameController.instance.player.transform.position) < 1.5f)
        {
            _NPCCanvas.SetActive(true);
            if (Input.GetKeyDown(KeyCode.E) && !_isDialogueStart&& !_isShopOpean)
            {
                GameController.instance.playerController.enabled = false;
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
            GameController.instance.dialogueUI.ShowDialogue(_currentDialogue._lines[_currentline]);
            _currentline++;
        }
        else if(_currentDialogue._playerReplyOptions != null && _currentDialogue._playerReplyOptions.Length > 0)
        {
            GameController.instance.dialogueUI.ShowAnswer(_currentDialogue._playerReplyOptions);
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
            GameController.instance.playerController.enabled = true;
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
       
        GameController.instance.dialogueUI.DialogueHide();
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
