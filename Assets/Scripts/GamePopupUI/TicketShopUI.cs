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


    protected override void UpdateData()
    {
        base.UpdateData();

        adCountText.text = GameManager.Instance.TicketAdCount.ToString();
    }

    private void Start()
    {
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
        SystemPopupUI.Instance.OpenTwoButton(23, string.Format(200.Localization(), Values.Ticket_Price), 80, 22,
    BuyAction,
            _GamePopup.ClosePopup);
    }

    private void BuyAction()
    {
        if(!GameManager.Instance.BuyTicket(Values.Ticket_Price))
        {
            SystemPopupUI.Instance.OpenNoneTouch(79);
        }
        _GamePopup.ClosePopup();
    }

    private void OnAd()
    {
        if (_GameManager.CanUseTicket)
        {   // ¹«·á
            AddTicket(Values.Ticket_AdValue);
        }
        else if (_GameManager.ChargeTicket)
        {
            // ±¤°í
            UnityAdsManager.Instance.ShowRewardAD(() => AddTicket(Values.Ticket_AdValue)
                                                                      ,  null //_GameManager.Timer_Ticket
                                                                      , EDailyGift.Ticket);
        }
    }

    private void AddTicket(int value)
    {
        GameManager.Instance.Ticket += value;

        _GamePopup.ClosePopup();
    }
}
