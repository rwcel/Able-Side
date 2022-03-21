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
    [SerializeField] Button freeButton;             // 무료 버튼
    [SerializeField] Button chargeButton;     // 광고로 무료 횟수 충전
    [Header("Button Texts")]
    [SerializeField] TextMeshProUGUI diaText;
    [SerializeField] TextMeshProUGUI freeCountText;
    [SerializeField] TextMeshProUGUI chargeText;
    [SerializeField] TextMeshProUGUI chargeCountText;

    // 선택한 아이템 알아야함
    private LobbyItemData lobbyItemData;
    private int price;

    protected override void UpdateData()
    {
        base.UpdateData();
        // lobbyItemData = ?

        lobbyItemData = LevelData.Instance.LobbyItemDatas[(int)_GameManager.SelectLobbyItem];
        price = lobbyItemData.price;

        nameText.text = lobbyItemData.type.ToString();
        descText.text = string.Format(lobbyItemData.description, lobbyItemData.value);
        iconImage.sprite = lobbyItemData.sprite;

        //diaText.text = $"Buy Dia\n{price}";
        diaText.text = price.ToString();

        UpdateUIs(_GameManager.LobbyItemFreeCount > 0);
    }

    private void Start()
    {
        _GameManager = GameManager.Instance;

        // backgroundButton.onClick.AddListener(OnClose);
        buyButton.onClick.AddListener(OnBuy);
        freeButton.onClick.AddListener(OnFree);
        chargeButton.onClick.AddListener(OnCharge);

        _GameManager.ObserveEveryValueChanged(_ => _GameManager.LobbyItemFreeCount)
            .Subscribe(value => UpdateUIs(value > 0))
            .AddTo(this.gameObject);

        //_GameManager.ObserveEveryValueChanged(_ => _GameManager.LobbyItemCharge)
        //    .Subscribe(value => UpdateTicketTime(value))
        //    .AddTo(_GameManager.gameObject);
    }

    private void UpdateUIs(bool canFree)
    {
        if (!lobbyItemData.isFree)
        {
            buyButton.gameObject.SetActive(true);
            freeButton.gameObject.SetActive(false);
            chargeButton.gameObject.SetActive(false);
        }
        else
        {
            freeButton.gameObject.SetActive(canFree);
            buyButton.gameObject.SetActive(!canFree);
            chargeButton.gameObject.SetActive(!canFree);
        }

        //freeText.text = $"Buy Free\n{_GameManager.LobbyItemFreeBuy}/{Values.Free_LobbyItem}";
        freeCountText.text = _GameManager.LobbyItemFreeCount.ToString();
        chargeText.text = $"Charge {_GameManager.LobbyItemCharge}";
        chargeCountText.text = _GameManager.LobbyItemAdCount.ToString();
    }

    public void OnBuy()
    {
        SystemPopupUI.Instance.OpenTwoButton(23, string.Format(200.Localization(), price), 80, 22,
            BuyAction,
            _GamePopup.ClosePopup);
    }

    private void BuyAction()
    {
        if (!_GameManager.BuyLobbyItem_Dia(price))
        {
            SystemPopupUI.Instance.OpenNoneTouch(79);
        }
        _GamePopup.ClosePopup();
    }

    public void OnFree()
    {
        if(_GameManager.BuyLobbyItem_Free())
        {
            _GamePopup.ClosePopup();
        }
    }

    public void OnCharge()
    {
        _GameManager.ChargeLobbyItem();
    }
}
