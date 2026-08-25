using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI.Table;

public class ShopManage : MonoBehaviour
{
    public RectTransform _ShopBackground;
    public Button _buttonPrefab;
    public int _totalButtonCount = 0; 
    public int _columnCount = 3;    
    public Vector2 _buttonSize = new(100, 100); 
    public float _spacingX = 20f;    
    public float _spacingY = 20f;
    public GameObject _AlarmUI;
   

    public ShopGoods_SO[] _goodsList;
    private void Awake()
    {
        gameObject.SetActive(false);
        _totalButtonCount = _goodsList.Length;
    }
    private void Start()
    {
        SpawnButton();
    }

        
    
    void SpawnButton()
    {
        int _rowCount = Mathf.CeilToInt((float)_totalButtonCount / _columnCount);
        float _ShopWidth = _ShopBackground.rect.width;
        float _ShopHeight = _ShopBackground.rect.height;

        float _totalButtonWidth = _columnCount * _buttonSize.x + (_columnCount - 1)* _spacingX;
        float _totalButtonHeight = _rowCount * _buttonSize.y + (_columnCount - 1)* _spacingY;
        float _XStartPos = -_ShopWidth / 2 + (_ShopWidth - _totalButtonWidth) / 2;
        float _YStartPos = _ShopHeight / 2 - (_ShopHeight - _totalButtonHeight) / 2;


        for (int i = 0; i < _totalButtonCount; i++)
        {
            int _col = i % _columnCount;
            int _row = i / _columnCount;

            float _posX = _XStartPos + _col * (_buttonSize.x + _spacingX) + _buttonSize.x / 2;
            float _posY = _YStartPos - _row * (_buttonSize.y + _spacingY) - _buttonSize.y / 2;

            Button newBtn = Instantiate(_buttonPrefab, _ShopBackground);
            RectTransform btnRect = newBtn.GetComponent<RectTransform>();
            Image btnImage = newBtn.GetComponent<Image>();
            btnImage.sprite = _goodsList[i].itemSprite;
            btnRect.sizeDelta = _buttonSize;
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = new Vector2(_posX, _posY);
            TMP_Text btnText = newBtn.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = _goodsList[i].goodsName + " "+ _goodsList[i].goodsPrice + "Coins";
            }
          

            int index = i;
            newBtn.onClick.AddListener(() => OnButtonClick(index));
        }


    }

    private IEnumerator NoMoneyAlerm()
    {
        _AlarmUI.SetActive(true);
        yield return new WaitForSeconds(1f);
        _AlarmUI.SetActive(false);
    }

    public void closeShop()
    {
        gameObject.SetActive(false);
        GameController.instance.NPC._isShopOpean = false;
        GameController.instance.playerController.enabled = true;
        
    }
    void OnButtonClick(int btnIndex)
    {
        if (GameController.instance.player._Coins >= _goodsList[btnIndex].goodsPrice)
        {

            switch (_goodsList[btnIndex].goodsName)
            {
                case "upgrade":

                    GameController.instance.player._Coins -= _goodsList[btnIndex].goodsPrice;
                    GameController.instance.weapon.LevelUp();


                    break;
                case "healing":
                    GameController.instance.player._Coins -= _goodsList[btnIndex].goodsPrice;
                    GameController.instance.player.healing();
                    break;
            }
        }
        else
        {
            StartCoroutine(NoMoneyAlerm());
        }

    }


    }
