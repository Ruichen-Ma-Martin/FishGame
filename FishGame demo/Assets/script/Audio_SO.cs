using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Audio", menuName = "ScriptableObjects/Audio", order = 2)]

public class Audio_SO : ScriptableObject
{
    public string AudioName;
    public AudioClip AudioClip;
}
