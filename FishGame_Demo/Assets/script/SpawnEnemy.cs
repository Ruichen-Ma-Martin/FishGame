using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnEnemy : MonoBehaviour
{
    public GameObject Enemy;
    public float _firstSpawnTime;
    public float _SpawnDuration;
    private float _timer;
    private bool _isFirstEnemySpawn=false;

    private void Update()
    {
        _timer += Time.deltaTime;
        spawnEnemy();
    }
    
    private void spawnEnemy()
    {
        if (_timer >= _firstSpawnTime)
        {
            Instantiate(Enemy,gameObject.transform.position, Quaternion.identity);
            _timer = 0;
            _isFirstEnemySpawn = true;
        }
        if( _timer >= _SpawnDuration && _isFirstEnemySpawn == true)
        {
            Instantiate(Enemy, gameObject.transform.position, Quaternion.identity);
            _timer = 0;
        }
    }
}
