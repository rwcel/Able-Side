using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Purchasing;

public class ShopItem : MonoBehaviour
{
    [SerializeField] protected TextMeshProUGUI nameText;
    [SerializeField] protected TextMeshProUGUI priceText;
    [SerializeField] protected Button buyButton;

    protected ShopData shopData;

    protected BackEndServerManager _BackEndServerManager;
    protected AudioManager _AudioManager;

    private void Start()
    {
        buyButton.onClick.AddListener(OnBuy);

        _AudioManager = AudioManager.Instance;
    }

    public virtual void SetData(ShopData shopData)
    {
        this.shopData = shopData;
        _BackEndServerManager = BackEndServerManager.Instance;

        UpdateData();
    }

    public virtual void UpdateData()
    {
        nameText.text = shopData.nameNum.Localization();
#if UNITY_EDITOR
        priceText.text = $"\\{shopData.price.CommaThousands()}";
#else
    if(shopData.productID != "")
        priceText.text = IAPManager.Instance.GetPrice(shopData.productID);
#endif
    }

    protected virtual void OnBuy()
    {
        _AudioManager.PlaySFX(ESFX.Touch);
    }

    /// <summary>
    /// Gacha Item은 사용하지 않음
    /// 
    /// base.OnPUrchaseComplete를 마지막에 실행하게 하기
    /// *RewardUI가 AddItem보다 뒤에 밀려야함
    /// </summary>
    public virtual void OnPurchaseComplete()
    {
        _AudioManager.PlaySFX(ESFX.BuyItem);
        // SystemPopupUI.Instance.OpenNoneTouch(3);

        GamePopup.Instance.OpenPopup(EGamePopup.Reward,
            null,
            () => GamePopup.Instance.AllClosePopup(null));
    }

    public virtual void OnPurchaseFail()
    {
        SystemPopupUI.Instance.OpenNoneTouch(50);
    }
}
