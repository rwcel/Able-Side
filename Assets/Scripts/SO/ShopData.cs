using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct FShopItem
{
    public int id;
    public int value;
}

[CreateAssetMenu(fileName = "Shop Item", menuName = "ScriptableObject/Shop Item")]
public class ShopData : ScriptableObject
{
    public EShopItem type;
    public FShopItem[] items;
    public int price;               // *사용하지 않음 -> IAP 데이터 이용

    public Sprite sprite;
    public int nameNum;         // Language 번호
    public string productID;
}