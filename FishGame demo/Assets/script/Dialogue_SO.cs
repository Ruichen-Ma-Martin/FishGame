using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueLine", menuName = "ScriptableObjects/DialogueLine", order = 1)]
public class Dialogue_SO : ScriptableObject
{
    public string[] _lines;
    public string[] _playerReplyOptions;
    public Dialogue_SO[] _npcReplies;
    public bool _isOpenShop;
}
