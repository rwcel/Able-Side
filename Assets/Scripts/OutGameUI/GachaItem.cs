using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;

public class GachaItem : ShopItem
{
    [SerializeField] TextMeshProUGUI freeCountText;
    [SerializeField] TextMeshProUGUI adCountText;
    [SerializeField] TextMeshProUGUI delayText;

    GameManager _GameManager;

    public override void SetData(ShopData shopData)
    {
        base.SetData(shopData);

        _GameManager = GameManager.Instance;

        this.ObserveEveryValueChanged(_ => _GameManager.ItemGachaFreeCount)
            .Subscribe(value => UpdateFreeCount(value))
            .AddTo(this.gameObject);

        // Debug.Log($"{_GameManager.ItemGachaFreeCount}");

        this.ObserveEveryValueChanged(_ => _GameManager.ItemGachaAdCount)
            .Subscribe(value => UpdateAdCount(value))
            .AddTo(this.gameObject);

        this.ObserveEveryValueChanged(_ => _GameManager.ItemGachaAdDelay)
            .Skip(System.TimeSpan.Zero)
            .Subscribe(value => {
                delayText.text = value !=0 ? $"Delay : {value}" : "";
                })
            .AddTo(_GameManager.gameObject);

        //_GameManager._ItemGachaTimerReactiveProperty
        //    .Subscribe(value => delayText.text = $"Delay : {value}",
        //    () => delayText.text = "");

        delayText.text = "";
    }

    protected override void OnBuy()
    {
        base.OnBuy();

        if (delayText.text != "")
            return;

        if (_GameManager.CanUseItemGacha)
        {   // 무료
            GamePopup.Instance.OpenPopup(EGamePopup.DailyGacha);
        }
        else if (_GameManager.ChargeItemGacha)
        {
            // _GameManager.UseItemGacha();

            // 광고
            UnityAdsManager.Instance.ShowRewardAD(() => GamePopup.Instance.OpenPopup(EGamePopup.DailyGacha)
                                                                      , _GameManager.Timer_ItemGacha
                                                                      , EDailyGift.ItemGacha);
        }
    }

    void UpdateFreeCount(int value)
    {
        freeCountText.text = $"Free : {value}";

        freeCountText.gameObject.SetActive(value > 0);
        adCountText.gameObject.SetActive(value <= 0);           // Free가 꺼지면 자동으로 AD가 켜져야함
    }

    void UpdateAdCount(int value)
    {
        adCountText.text = $"Ad : {value}";
    }
}
