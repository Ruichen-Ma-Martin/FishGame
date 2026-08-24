using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using System.Runtime.CompilerServices;
using System;


public class enemyControl : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _rb;
    private float _moveSpeed = 3f;
    private float moveDirection = -1f;
    [SerializeField] private GameObject player;

    [SerializeField] private Vector2 _areaSize = new Vector2(5f, 5f);
    [SerializeField] private Vector2 _AreaCenter;
    //private float _timer;
    //private float _timerattack;
    //private float _CDattack = 2f;
    //private float _DistanceToPlayer;

    [SerializeField] private GameObject _attack;
    private Vector2 _attackR;
    private Vector2 _attackL;
    
    public SpriteRenderer _enmeySprite;
    public Animator _enemyAnim;
    private animationEvent EventHandler;
    private enum State 
    { MoveInRange,followPlayer, stopMove }
    private State _currentstate;
    private bool _canchangeState = true;




    private void Awake()
    {
        _attackR = _attack.transform.localPosition;
        _attackL = -_attackR;
        EventHandler = GetComponent<animationEvent>();
        EventHandler.OnAttack += hitplayer;
        EventHandler.EndAttack += hitFinish;
        EventHandler.AttackFinish += iscanchange;
        player = GameObject.FindGameObjectWithTag("Player");
    }


    void Update()
    {
        
        //_DistanceToPlayer = Mathf.Abs(Vector2.Distance(transform.position, player.transform.position));
        changeState();
        rightNowState();

    }
    void changeState()
    {
        if(_canchangeState)
        {
            switch(_currentstate)
            {
                case State.MoveInRange:
                    MoveInRange();
                    break;
                case State.followPlayer:
                    followPlayer();
                    break;
                case State.stopMove:
                    stopMove();
                    break;
            }
        }
    }
    void iscanchange(){_canchangeState = true; }

    public void changetomoveinrange() { _currentstate = State.MoveInRange; }
    public void changetofollowplayer() { _currentstate = State.followPlayer; }
    public void changetostopmove(){_currentstate = State.stopMove;}

    void rightNowState()
    {
        if (_currentstate == State.stopMove)
        {
            _canchangeState = false;
        }

    }
    public void SetArea()
    {
        float x = UnityEngine.Random.Range(transform.position.x - _areaSize.x / 2, transform.position.x + _areaSize.x / 2);
     
        _AreaCenter = new Vector2(x,transform.position.y);
        
    }

    public void MoveInRange()
    {
        _enemyAnim.SetBool("isStop", false);

        _rb.linearVelocity = new Vector2(-moveDirection * _moveSpeed, _rb.linearVelocity.y);
        if (transform.position.x >= _AreaCenter.x + _areaSize.x / 2)
        {
            
            //Debug.Log("change direction");
            moveDirection = 1f;
            _enmeySprite.flipX = true;
            

        }
        else if (transform.position.x <= _AreaCenter.x - _areaSize.x / 2)
        {
            //Debug.Log("change direction");
            moveDirection = -1f;
            _enmeySprite.flipX = false;
        }
        
    }


    public void followPlayer()
    {
       // Debug.Log("follow player");
        FilpFaceToPlayer();
        _enemyAnim.SetBool("isStop", false);

        if (player != null)
        {
            Vector2 direction = (player.transform.position - transform.position).normalized;
            _rb.linearVelocity = new Vector2(direction.x * _moveSpeed, _rb.linearVelocity.y);
        }
           
        
    }

    public void stopMove()
    {
        FilpFaceToPlayer();
         _enemyAnim.SetBool("isStop", true);
        if (player != null)
            {
                Vector2 direction = (player.transform.position - transform.position).normalized;
                if (direction.x > 0f)
                {
               
                //Debug.Log("Player hit from right!");
                _attack.transform.localPosition = _attackR;

                
                }
                else if (direction.x < 0f )
                {   
                
                //Debug.Log("Player hit from left!");
                _attack.transform.localPosition = _attackL;
            
                
                }
        }


    }

    void FilpFaceToPlayer()
    {
        //Debug.Log("flip face to player");
        if (player != null)
        {
            
            if (player.transform.position.x > transform.position.x)
            {
                _enmeySprite.flipX = false;
            }
            else if (player.transform.position.x < transform.position.x)
            {
                _enmeySprite.flipX = true;
            }
        }
    }

    public void hitplayer(){_attack.SetActive(true);}
    public void hitFinish(){ _attack.SetActive(false); }



  
        void OnCollisionEnter2D(Collision2D collision)
        {
        if (collision.gameObject.CompareTag("wall"))
        {
            moveDirection *= -1f;
            _enmeySprite.flipX = !_enmeySprite.flipX;
        }
        if (collision.gameObject.CompareTag("enemy"))
        {
            moveDirection *= -1f;
            _enmeySprite.flipX = !_enmeySprite.flipX;
        }
        }
}

