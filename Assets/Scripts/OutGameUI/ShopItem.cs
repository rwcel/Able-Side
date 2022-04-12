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
    [SerializeField] protected GameObject saleObj;
    [SerializeField] protected TextMeshProUGUI saleText;
    [SerializeField] protected TextMeshProUGUI beforePriceText;

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

        if(saleObj != null)
            saleObj.SetActive(shopData.salePercent > 0);

        UpdateData();
    }

    public virtual void UpdateData()
    {
        nameText.text = shopData.nameNum.Localization();

        // *ShopItem은 역산을 통해 원가를 산정하기
        if (shopData.salePercent > 0)
        {
            // saleText.text = $"{shopData.salePercent}%";
#if UNITY_EDITOR
            beforePriceText.text = $"<s>\\{shopData.price.CommaThousands()}</s>";

            int price = shopData.price - (shopData.price * shopData.salePercent / 100);
            priceText.text = $"\\{price.CommaThousands()}";
#else
            if (shopData.productID != "")
            {
                priceText.text = IAPManager.Instance.GetPrice(shopData.productID);
                int beforePrice = (int)(IAPManager.Instance.GetPriceToDecimal(shopData.productID)
                                        * 100 / (100 - shopData.salePercent));
                beforePriceText.text = $"<s>{beforePrice.CommaThousands()}</s>";                     // **숫자만 나옴
            }
#endif
        }
        else
        {
            if(beforePriceText != null)
                beforePriceText.text = "";
#if UNITY_EDITOR
            priceText.text = $"\\{shopData.price.CommaThousands()}";
#else
    if(shopData.productID != "")
        priceText.text = IAPManager.Instance.GetPrice(shopData.productID);
#endif
        }
    }

    protected virtual void OnBuy()
    {
        _AudioManager.PlaySFX(ESFX.Touch);
    }

    /// <summary>
    /// Gacha Item은 사용하지 않음
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
