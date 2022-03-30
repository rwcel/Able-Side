using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;

public class GachaItem : ShopItem
{
    [SerializeField] TextMeshProUGUI delayText;
    [SerializeField] GameObject adObj;

    GameManager _GameManager;

    public override void SetData(ShopData shopData)
    {
        base.SetData(shopData);

        _GameManager = GameManager.Instance;

        this.ObserveEveryValueChanged(_ => _GameManager.ItemGachaFreeCount)
            .Subscribe(value => adObj.SetActive(value <= 0))
            .AddTo(this.gameObject);
    }

    protected override void OnBuy()
    {
        base.OnBuy();

        _GameManager.UseDailyGift(EDailyGift.ItemGacha, () => BackEndServerManager.Instance.Gacha(EGacha.ShopItem));
    }
}
