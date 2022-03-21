using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeeklyItem : ShopItem
{
    [SerializeField] Image[] iconImages;

    public override void SetData(ShopData shopData)
    {
        base.SetData(shopData);

        for (int i = 0, length = shopData.items.Length; i < length; i++)
        {
            iconImages[i].sprite = _BackEndServerManager.GetItemSprite((EItem)shopData.items[i].id);
        }

        // 구매했다면 다시 구매 못함
        if (_BackEndServerManager.BuyWeekly)
        {
            buyButton.interactable = false;
        }
    }

    protected override void OnBuy()
    {
        base.OnBuy();

        if (GameApplication.Instance.IsTestMode)
        {
            OnPurchaseComplete();
        }
        else
        {
            IAPManager.Instance.BuyProductID(shopData.productID, OnPurchaseComplete, OnPurchaseFail);
        }
    }

    public override void OnPurchaseComplete()
    {
        _BackEndServerManager.BuyWeeklyPackage(shopData.items);

        buyButton.interactable = false;

        base.OnPurchaseComplete();
    }
}
