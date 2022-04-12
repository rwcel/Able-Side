using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;

public class ItemBuyUI : PopupUI
{
    [Header("Item")]
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI descText;
    [SerializeField] Image iconImage;
    [Header("Buttons")]
    [SerializeField] Button buyButton;              // 다이아로 구매
    [SerializeField] Button freeButton;                 // 무료 버튼
    [SerializeField] Button chargeButton;           // 광고로 무료 횟수 충전
    [Header("Button Texts")]
    [SerializeField] TextMeshProUGUI diaText;
    [SerializeField] TextMeshProUGUI beforeDiaText;
    [SerializeField] TextMeshProUGUI freeCountText;
    [SerializeField] TextMeshProUGUI chargeCountText;
    [Header("Sale")]
    [SerializeField] GameObject saleObj;
    [SerializeField] TextMeshProUGUI saleText;

    // 선택한 아이템 알아야함
    private LobbyItemData lobbyItemData;
    private int price;

    protected override void Start()
    {
        base.Start();

        _GameManager = GameManager.Instance;

        buyButton.onClick.AddListener(OnBuy);
        freeButton.onClick.AddListener(OnFree);
        chargeButton.onClick.AddListener(OnCharge);

        _GameManager.ObserveEveryValueChanged(_ => _GameManager.LobbyItemFreeCount)
            .Subscribe(value => UpdateUIs(value > 0))
            .AddTo(this.gameObject);

    }

    protected override void UpdateData()
    {
        base.UpdateData();
        // lobbyItemData = ?

        lobbyItemData = LevelData.Instance.LobbyItemDatas[(int)_GameManager.SelectLobbyItem];

        nameText.text = lobbyItemData.nameLanguageNum.Localization();
        descText.text = string.Format(lobbyItemData.descLanguageNum.Localization(), lobbyItemData.value);

        iconImage.sprite = lobbyItemData.sprite;

        //diaText.text = $"Buy Dia\n{price}";
        saleObj.SetActive(lobbyItemData.salePercent > 0);
        if(lobbyItemData.salePercent > 0)
        {
            // saleText.text = $"{lobbyItemData.salePercent}%";
            beforeDiaText.text = $"<s>{lobbyItemData.price}</s>";
            price = lobbyItemData.price - (lobbyItemData.price * lobbyItemData.salePercent / 100);
        }
        else
        {
            beforeDiaText.text = "";
            price = lobbyItemData.price;
        }
        diaText.text = price.ToString();

        UpdateUIs(_GameManager.LobbyItemFreeCount > 0);
    }

    private void UpdateUIs(bool canFree)
    {
        buyButton.gameObject.SetActive(true);
        if (!lobbyItemData.isFree)
        {
            freeButton.gameObject.SetActive(false);
            chargeButton.gameObject.SetActive(false);
        }
        else
        {
            freeButton.gameObject.SetActive(canFree);
            chargeButton.gameObject.SetActive(!canFree);
        }

        var giftData = LevelData.Instance.DailyGiftDatas[(int)EDailyGift.LobbyItem];

        freeCountText.text = string.Format(212.Localization(), _GameManager.LobbyItemFreeCount,
                                                                            giftData.freeCount);
        chargeCountText.text = string.Format(213.Localization(), giftData.adCount.ToString());
    }

    public void OnBuy()
    {
        SystemPopupUI.Instance.OpenTwoButton(7, string.Format(214.Localization(), price), 0, 1,
            BuyAction,
            _GamePopup.ClosePopup);
    }

    private void BuyAction()
    {
        if (!_GameManager.BuyLobbyItem_Dia(price))
        {
            SystemPopupUI.Instance.OpenNoneTouch(52);
        }

        _GamePopup.ClosePopup();
    }

    public void OnFree()
    {
        AudioManager.Instance.PlaySFX(ESFX.Touch);
        if (_GameManager.BuyLobbyItem_Free())
        {
            _GamePopup.ClosePopup();
        }
    }

    public void OnCharge()
    {
        AudioManager.Instance.PlaySFX(ESFX.Touch);
        _GameManager.UseDailyGift(EDailyGift.LobbyItem, null);
    }
}
