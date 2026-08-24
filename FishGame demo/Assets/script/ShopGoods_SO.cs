using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "ShopGoods", menuName = "ScriptableObjects/ShopGoods", order = 3)]
public class ShopGoods_SO : ScriptableObject
{
    public Sprite itemSprite;
    public string goodsName;
    public float goodsPrice;
}


