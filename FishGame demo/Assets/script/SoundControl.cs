using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundControl : MonoBehaviour
{
    public Audio_SO[] _audioList;
    private void Awake()
    {
        bullet.BulletExplosion += bulletSFX_play;
        enemy.OnEnemyDeath += enemySFX_play;

    }

    void bulletSFX_play(bullet bullet)
    {
        foreach (var item in _audioList)
        {
            if (item.AudioName == bullet.tag)
            {
                AudioSource.PlayClipAtPoint(item.AudioClip, bullet.transform.position);
            }
        }
    }
    void enemySFX_play(enemy Enemy)
    {
        foreach (var item in _audioList)
        {
            if (item.AudioName == Enemy.tag)
            {
                AudioSource.PlayClipAtPoint(item.AudioClip, Enemy.transform.position);
            }
        }
    }
}


