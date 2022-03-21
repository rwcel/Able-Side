using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;

public class ShopUI : DockUI
{
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] TextMeshProUGUI diaText;

    [Header("�ְ�")]
    [SerializeField] GameObject weeklyItemPrefab;
    [SerializeField] Transform weeklyParent;

    [Header("���̾�")]
    [SerializeField] GameObject diaItemPrefab;
    [SerializeField] Transform diaParent;

    [Header("���� ����")]
    [SerializeField] GameObject dailyGiftItemPrefab;
    [SerializeField] Transform dailyGiftParent;


    GameManager _GameManager;
    List<ShopItem> shopItems;

    public override void OnStart()
    {
        _GameManager = GameManager.Instance;

        shopItems = new List<ShopItem>(LevelData.Instance.ShopDatas.Length);

        CreateShopItems();

        this.ObserveEveryValueChanged(_ => _GameManager.DiaCount)
        //.Skip(System.TimeSpan.Zero)
        .Subscribe(value => diaText.text = value.CommaThousands())
        .AddTo(this.gameObject);
    }

    public void CreateShopItems()
    {
        foreach (var shopData in LevelData.Instance.ShopDatas)
        {
            ShopItem shopItem = null;
            switch (shopData.type)
            {
                case EShopItem.Dia:
                    shopItem = Instantiate(diaItemPrefab, diaParent).GetComponent<ShopItem>();
                    break;
                case EShopItem.Weekly:
                    shopItem = Instantiate(weeklyItemPrefab, weeklyParent).GetComponent<ShopItem>();
                    break;
                case EShopItem.DailyGift:
                    shopItem = Instantiate(dailyGiftItemPrefab, dailyGiftParent).GetComponent<ShopItem>();
                    break;
            }
            if (shopItem != null)
            {
                shopItem.SetData(shopData);
                shopItems.Add(shopItem);
            }
        }
    }

    public override void UpdateDatas()
    {
        scrollRect.verticalNormalizedPosition = 1f;

        // DailyGift Ȯ��
        foreach (var shopItem in shopItems)
        {
            shopItem.UpdateData();
        }

    }
}
