using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using TMPro;

public class HomeUI : DockUI
{
    [Header("Dia")]
    [SerializeField] GameObject diaTooltipObj;
    [SerializeField] Button diaTooltipButton;
    [SerializeField] Button diaAddButton;
    [SerializeField] TextMeshProUGUI diaText;
    [SerializeField] TextMeshProUGUI cashDiaText;
    [SerializeField] TextMeshProUGUI freeDiaText;

    [Header("Ticket")]
    [SerializeField] Button ticketAddButton;
    [SerializeField] TextMeshProUGUI ticketText;
    [SerializeField] TextMeshProUGUI ticketTimeText;
    [SerializeField] Transform ticketParent;                // 내부 개수 보여줄 것
    private List<GameObject> tickets = new List<GameObject>();

    [Header("Profile")]
    [SerializeField] Button profileButton;
    [SerializeField] Image profileIconImage;
    [SerializeField] TextMeshProUGUI nickNameText;
    [SerializeField] TextMeshProUGUI bestScoreText;

    [Header("보상")]
    [SerializeField] TimeReward timeReward;
    [SerializeField] PlayReward playReward;

    [Header("Start")]
    [SerializeField] Button readyButton;

    GameManager _GameManager;

    private bool isMaxTicket = false;


    public override void OnStart()
    {
        InitSet();

        AddListeners();

        AddActions();
    }

    public override void UpdateDatas()
    {
        // TimeReward 시간 갱신
        playReward.UpdateLanguage();
        timeReward.UpdateData();
    }

    private void InitSet()
    {
        _GameManager = GameManager.Instance;

        profileIconImage.sprite = _GameManager.ProfileData.sprite;

        foreach (Transform ticketItem in ticketParent)
        {
            tickets.Add(ticketItem.gameObject);
        }

        diaTooltipObj.SetActive(false);
    }

    private void AddListeners()
    {
        diaTooltipButton.onClick.AddListener(OnTooltip);
        readyButton.onClick.AddListener(OnReady);
        diaAddButton.onClick.AddListener(OnDia);
        ticketAddButton.onClick.AddListener(OnTicket);
        profileButton.onClick.AddListener(OnProfile);
    }

    private void AddActions()
    {
        _GameManager.ObserveEveryValueChanged(_ => _GameManager.CashDia)
            //.Skip(System.TimeSpan.Zero)
            .Subscribe(value => UpdateDia(true, value))
            .AddTo(this.gameObject);

        _GameManager.ObserveEveryValueChanged(_ => _GameManager.FreeDia)
            //.Skip(System.TimeSpan.Zero)
            .Subscribe(value => UpdateDia(false, value))
            .AddTo(this.gameObject);

        _GameManager.ObserveEveryValueChanged(_ => _GameManager.Ticket)
            //.Skip(System.TimeSpan.Zero)
            .Subscribe(value => UpdateTicket(value))
            .AddTo(this.gameObject);

        _GameManager.ObserveEveryValueChanged(_ => _GameManager.TicketTime)
            //.Skip(System.TimeSpan.Zero)
            .Subscribe(value => UpdateTicketTime(value))             // ** 00:00단위
            .AddTo(this.gameObject);

        _GameManager.ObserveEveryValueChanged(_ => _GameManager.NickName)
            //.Skip(System.TimeSpan.Zero)
            .Subscribe(value => UpdateNickName(value))
            .AddTo(this.gameObject);

        _GameManager.ObserveEveryValueChanged(_ => _GameManager.BestScore)
            //.Skip(System.TimeSpan.Zero)
            .Subscribe(value => UpdateBestScore(value))
            .AddTo(this.gameObject);

        _GameManager.ObserveEveryValueChanged(_ => _GameManager.ProfileData)
            .Subscribe(value => profileIconImage.sprite = value.sprite)
            .AddTo(this.gameObject);
    }

#region OnClick

    public void OnTooltip()
    {
        AudioManager.Instance.PlaySFX(ESFX.Touch);
        diaTooltipObj.SetActive(!diaTooltipObj.activeSelf);
    }

    public void OnDia()
    {
        AudioManager.Instance.PlaySFX(ESFX.Touch);
        GameUIManager.Instance.MoveDock(EDock.Shop);
    }

    public void OnTicket()
    {
        GamePopup.Instance.OpenPopup(EGamePopup.TicketShop);
    }

    public void OnReady()
    {
        if(GamePopup.Instance == null)
        {
            Debug.Log("다른 데이터?");
        }

        GamePopup.Instance.OpenPopup(EGamePopup.ItemSelect);
    }

    public void OnProfile()
    {
        GamePopup.Instance.OpenPopup(EGamePopup.ProfileInfo);
    }

    #endregion OnClick

    private void UpdateDia(bool isCash, int value)
    {
        if(isCash)
        {
            cashDiaText.text = value.CommaThousands();
        }
        else
        {
            freeDiaText.text = value.CommaThousands();
        }

        // **더한 값으로 변경
        diaText.text = _GameManager.DiaCount.CommaThousands();
    }

    private void UpdateTicket(int value)
    {
        //remainText.text = $"Ticket : {value}";   -> ticketNumText;
        ticketText.text = value.ToString();

        if (value >= Values.MaxTicket)
        {
            ticketTimeText.text = "MAX";
            // Stop

            isMaxTicket = true;
        }
        else
        {
            isMaxTicket = false;
        }

        for (int i = 0, length = tickets.Count; i < value && i < length; i++)
        {
            tickets[i].SetActive(true);
        }
        for (int i = value, length = tickets.Count; i < length; i++)
        {
            tickets[i].SetActive(false);
        }

        // addButton.gameObject.SetActive(value < Values.MaxTicket);
    }

    /// <summary>
    /// 맨처음 들어올때 계산 순서가 반대가 되어 처리 한번 필요
    /// </summary>
    private void UpdateTicketTime(int value)
    {
        if (isMaxTicket)
            return;

        ticketTimeText.text = string.Format("{0:D1}:{1:D2}", value / 60, value % 60);
    }

    private void UpdateBestScore(int value)
    {
        bestScoreText.text = value.CommaThousands();
    }

    private void UpdateNickName(string value)
    {
        nickNameText.text = value;
    }
}
