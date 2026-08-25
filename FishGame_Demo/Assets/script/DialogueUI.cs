using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogueUI : MonoBehaviour
{
    public GameObject _dialoguePanel;
    public GameObject _playerOptionPanel;
    public TMP_Text _DialogueText;
    public TMP_Text _PlayerOption1Text;
    public TMP_Text _PlayerOption2Text;

    public void ShowDialogue(string DialogueText)
    {
        gameObject.SetActive(true);
        _dialoguePanel.SetActive(true);
        _playerOptionPanel.SetActive(false);
        _DialogueText.text = DialogueText;
    }

    public void ShowAnswer(string[] Options)
    {
        
        _playerOptionPanel.SetActive(true);
        _PlayerOption1Text.text = Options[0];
        if (Options.Length >= 2)
        {
            _PlayerOption2Text.transform.parent.gameObject.SetActive(true);
            _PlayerOption2Text.text = Options[1];
        }
        else
        {
            _PlayerOption2Text.transform.parent.gameObject.SetActive(false);
            _PlayerOption2Text.text = "";
        }

    }

    public void DialogueHide()
    {
        _dialoguePanel.SetActive(false);
        _playerOptionPanel.SetActive(false);
        gameObject.SetActive(false);
        _DialogueText.text = "";
    }
}



