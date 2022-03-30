using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TicketShopUI : PopupUI
{
    [Header("Texts")]
    [SerializeField] TextMeshProUGUI priceText;
    [SerializeField] TextMeshProUGUI adCountText;

    [Header("Buttons")]
    [SerializeField] Button mailButton;
    [SerializeField] Button buyButton;
    [SerializeField] Button adButton;
    [SerializeField] Button exitButton;


    //protected override void UpdateData()
    //{
    //    base.UpdateData();

    //    adCountText.text = GameManager.Instance.TicketAdCount.ToString();
    //}

    protected override void Start()
    {
        base.Start();

        priceText.text = Values.Ticket_Price.ToString();

        AddListeners();
    }

    void AddListeners()
    {
        mailButton.onClick.AddListener(() => GameUIManager.Instance.MoveDock(EDock.Mail));
        buyButton.onClick.AddListener(OnBuy);
        adButton.onClick.AddListener(OnAd);
        exitButton.onClick.AddListener(_GamePopup.ClosePopup);
    }

    private void OnBuy()
    {
        SystemPopupUI.Instance.OpenTwoButton(7, string.Format(214.Localization(), Values.Ticket_Price), 0, 1,
            BuyAction,
            _GamePopup.ClosePopup);
    }

    private void BuyAction()
    {
        if(!GameManager.Instance.BuyTicket(Values.Ticket_Price))
        {
            SystemPopupUI.Instance.OpenNoneTouch(52);
        }
        _GamePopup.ClosePopup();
    }

    private void OnAd()
    {
        _GameManager.UseDailyGift(EDailyGift.Ticket, () => AddTicket(Values.Ticket_AdValue));
    }

    private void AddTicket(int value)
    {
        GameManager.Instance.Ticket += value;

        _GamePopup.ClosePopup();
    }
}
