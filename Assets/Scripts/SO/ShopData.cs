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
    public int price;               // *������� ���� -> IAP ������ �̿�

    public Sprite sprite;
    public int nameNum;         // Language ��ȣ
    public string productID;
}