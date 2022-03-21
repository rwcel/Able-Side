using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiaItem : ShopItem
{
    [SerializeField] Image iconImage;
    [SerializeField] GameObject bonusSlot;
    [SerializeField] TextMeshProUGUI bonusText;

    public override void SetData(ShopData shopData)
    {
        base.SetData(shopData);

        iconImage.sprite = shopData.sprite;

        bonusSlot.SetActive(shopData.items.Length >= 2);            // 2�� �̻��̸� ���� ���̾� ����
        // bonusText.SetText();
    }

    /// <summary>
    /// ����, ���� ���̾� ǥ��
    /// *items[0]�� ������ ����, items[1]�� ������ ���Ῡ�߸���
    /// </summary>
    public override void UpdateData()
    {
        base.UpdateData();

        nameText.text = string.Format(65.Localization(), shopData.items[0].value);
        if(shopData.items.Length > 1)
        {
            bonusText.text = string.Format(66.Localization(), shopData.items[1].value);
        }
    }

    protected override void OnBuy()
    {
        base.OnBuy();

        if(GameApplication.Instance.IsTestMode)
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
        // ������ �׸� ���� �ٸ�
        foreach (var item in shopData.items)
        {
            // Backend?
            _BackEndServerManager.AddItem((EItem)item.id, item.value);
        }

        _BackEndServerManager.ShopItemyLog(EShopItem.Dia);

        base.OnPurchaseComplete();
    }
}
