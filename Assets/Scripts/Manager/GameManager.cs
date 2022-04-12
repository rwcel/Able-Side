using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

/// <summary>
/// **GameManager�� ������ �ʹ� ����
/// PlayerManager��� ���� �д� �ʿ� : Player�� ������
/// </summary>
public class GameManager : Singleton<GameManager>
{
    public GameController GameController;
    public GameCharacter GameCharacter;

    [HideInInspector] public bool IsGameStart;             // ���� ����. 
    [HideInInspector] public int InputValue = 0;            // 3�� �غ� ���� ������ �Է�   *������ �ɸ� �� �־ intó�� 
    public bool CanInput => InputValue > 0;

    public string NickName => BackEndServerManager.Instance.NickName;
    public int Rank => BackEndServerManager.Instance.GetMyScoreRank();
    public ProfileData ProfileData => LevelData.Instance.ProfileDatas[BackEndServerManager.Instance.ProfileIcon];

    [HideInInspector] public int BestScore;
    [HideInInspector] public int BestMaxCombo;
    [HideInInspector] public int AccumulateScore;
    [HideInInspector] public int Ticket;
    [HideInInspector] public int TicketTime;

    [HideInInspector] public int PlayRewardCount;
    public DateTime TimeRewardTime;              // �д��� ǥ�� �ʿ�

    [HideInInspector] public int FreeDia;
    [HideInInspector] public int CashDia;

    public int DiaCount => FreeDia + CashDia;

    [HideInInspector] public int[] ItemsCount;         // ELobbyItem

    public Dictionary<EDailyGift, DailyGift> DailyGifts = new Dictionary<EDailyGift, DailyGift>();

    public System.Action<bool> OnGameStart;
    public System.Action<ELobbyItem> OnBuyLobbyItem;             // ������ ���� �� ���õǰ�


    // *������ �����Ͷ� ���� ������ �ذ��� �� ����
    private static readonly string _Key_TicketTime = "TicketRemainTime";
    private static readonly string _Key_DateOfExit = "DateOfExit";
    private static readonly string[] _Key_DailyGifts = 
    {
        EDailyGift.ItemGacha.ToString(), 
        EDailyGift.Ticket.ToString(), 
        EDailyGift.LobbyItem.ToString(), 
        EDailyGift.Revive.ToString(), 
        EDailyGift.DoubleReward.ToString(), 
        EDailyGift.TimeReward.ToString(), 
    };


    // **���⿡ �� �����°� �³�?
    [HideInInspector] public ELobbyItem SelectLobbyItem;
    [HideInInspector] public FPostInfo SelectPostInfo;
    [HideInInspector] public string SelectSerialCode;

    private int updateScore, updateCombo;

    protected override void AwakeInstance() 
    {
        ItemsCount = new int[Enum.GetValues(typeof(ELobbyItem)).Length];

        foreach (EDailyGift item in Enum.GetValues(typeof(EDailyGift)))
        {
            DailyGifts.Add(item, new DailyGift(0, 0));
        }

        disposables = new IDisposable[DailyGifts.Count];
    }

    protected override void DestroyInstance() { }

    private void Start()
    {
        LoadData();

        AddListeners();
    }

    public void OnLoaded()
    {
        GameController.OnStart();
        GameCharacter.OnStart();

        GamePopup.Instance.OpenPopup(EGamePopup.Notice);            // *ó�� ���� 1ȸ

        UnityAdsManager.Instance.ShowBannerAD();
    }

    private void AddListeners()
    {
        /// *���� ������ Input�� ������⶧���� 0,1�θ� �����Ǿ����
        this.ObserveEveryValueChanged(_ => IsGameStart)
            .Subscribe(isGameStart => {
                if (!isGameStart)
                {
                    InputValue = 0;
                    AudioManager.Instance.PlayLobbyBGM();
                }
                else
                {
                    // GameManager�� intro������ ���⋚���� ���� ȣ��
                    AudioManager.Instance.PlayInGameBGM();
                }

                OnGameStart?.Invoke(isGameStart);
            })
            .AddTo(this.gameObject);

        // **Skip ���ϸ� ������ 0���� �ʱ�ȭ ��
        this.ObserveEveryValueChanged(_ => Ticket)
            .Skip(System.TimeSpan.Zero)
            .Subscribe(value => UpdateTicket(value))
            .AddTo(this.gameObject);

        this.ObserveEveryValueChanged(_ => FreeDia)
            .Skip(System.TimeSpan.Zero)
            .Subscribe(value => UpdateDia(EGoods.FreeDia, value))
            .AddTo(this.gameObject);

        this.ObserveEveryValueChanged(_ => CashDia)
            .Skip(System.TimeSpan.Zero)
            .Subscribe(value => UpdateDia(EGoods.CashDia, value))
            .AddTo(this.gameObject);

        // **Dictionary �� Ŭ������ ������ Ư���� �����
        foreach (var dailyGift in DailyGifts)
        {
            this.ObserveEveryValueChanged(_ => dailyGift.Value.freeCount)
                .Skip(TimeSpan.Zero)
                .Subscribe(_ => UpdateDailyGifts(dailyGift.Key, dailyGift.Value))
                .AddTo(this.gameObject);

            this.ObserveEveryValueChanged(_ => dailyGift.Value.adCount)
                .Skip(TimeSpan.Zero)
                .Subscribe(_ => UpdateDailyGifts(dailyGift.Key, dailyGift.Value))
                .AddTo(this.gameObject);
        }

        //ReplaceObservable
        //    .Subscribe(UpdateDailyGifts)
        //    .AddTo(this.gameObject);

        for (int i = 0, length = ItemsCount.Length; i < length; i++)
        {
            int num = i;
            this.ObserveEveryValueChanged(_ => ItemsCount[num])
                .Skip(System.TimeSpan.Zero)
                .Subscribe(value =>
                {
                    // Debug.Log($"{num} {value}");
                    UpdateItem(num, value);
                })
                .AddTo(this.gameObject);
        }
    }

    public void LoadData()
    {
        BackEndServerManager.Instance.GetGameDatas(this);

        // GetProfileDatas();          // �̰͵� ������?
    }

    public void CheckTicketExitTime()
    {
        if (!PlayerPrefs.HasKey(_Key_DateOfExit) 
            || !PlayerPrefs.HasKey(_Key_TicketTime))
        {
            TicketTime = Values.TicketTime;
            UpdateTicket(Ticket);
            return;
        }

        if(Ticket >= Values.MaxTicket)
        {
            TicketTime = Values.TicketTime;
            return;
        }

        var dateOfExit = DateTime.Parse(PlayerPrefs.GetString(_Key_DateOfExit));

        double totalSeconds = DateTime.Now.Subtract(dateOfExit).TotalSeconds;
        TicketTime = PlayerPrefs.GetInt(_Key_TicketTime);

        Debug.Log($"{totalSeconds} / {TicketTime}");

        if(Ticket < Values.MaxTicket)
        {
            // ��ü���� ������ -> ������ ��ü
            if (totalSeconds >= Values.TicketTime * Values.MaxTicket)
            {
                Ticket = Values.MaxTicket;
            }
            else
            {
                if (TicketTime > (int)totalSeconds)
                {
                    TicketTime -= (int)totalSeconds;
                }
                else
                {
                    Ticket++;
                    int value = (int)totalSeconds - TicketTime;
                    while (value >= Values.TicketTime)
                    {
                        value -= Values.TicketTime;
                        if (Ticket++ >= Values.MaxTicket)
                            break;
                    }
                    TicketTime = Values.TicketTime - value;
                }
            }
        }

        if(Ticket >= Values.MaxTicket)
        {
            TicketTime = Values.TicketTime;
        }

        // **OnStart���� ��� �������� UniRx Skip�� �� ���� ����Ǿ� TicketTime�� ������� �ʴ� ����
        UpdateTicket(Ticket);
    }

    public void CheckDailyGiftAdExitTime(EDailyGift dailyGift)
    {
        if (!PlayerPrefs.HasKey(_Key_DateOfExit)
            || !PlayerPrefs.HasKey(dailyGift.ToString()))
        {
            return;
        }

        // ���
        var dateOfExit = DateTime.Parse(PlayerPrefs.GetString(_Key_DateOfExit));

        double totalSeconds = DateTime.Now.Subtract(dateOfExit).TotalSeconds;
        DailyGifts[dailyGift].adDelay = PlayerPrefs.GetInt(dailyGift.ToString()) - (int)totalSeconds;      // �����ð� : ���������ð� - �����ð�

        if (DailyGifts[dailyGift].adDelay > 0)
        {
            Debug.Log($"Ÿ�̸� ��� : {dailyGift} => {PlayerPrefs.GetInt(dailyGift.ToString())} - {totalSeconds}");
            SetAdTimer(dailyGift, DailyGifts[dailyGift].adDelay);
        }
        else
        {
            DailyGifts[dailyGift].adDelay = 0;
        }
    }

    /// <summary>
    /// ���� ��ȭ ���� ���
    /// </summary>
    public bool UseDia(int value)
    {
        if (DiaCount < value)
            return false;

        if (CashDia >= value)
        {
            CashDia -= value;
        }
        else
        {
            FreeDia -= (value - CashDia);
            CashDia = 0;
        }

        return true;
    }

    public bool BuyTicket(int value)
    {
        // ���̾� ��ȭ�� �α׿� ���
        int cashDia = CashDia;
        int freeDia = FreeDia;

        // ���̾� ���� Ȯ�� + �Ҹ�
        if (!UseDia(value))
            return false;

        Ticket += Values.Ticket_BuyValue;

        BackEndServerManager.Instance.TicketDiaLog(cashDia - CashDia, freeDia - FreeDia, CashDia, FreeDia);

        return true;
    }

    public bool BuyLobbyItem_Dia(int value)
    {
        // ���̾� ��ȭ�� �α׿� ���
        int cashDia = CashDia;
        int freeDia = FreeDia;

        // ���̾� ���� Ȯ�� + �Ҹ�
        if (!UseDia(value))
            return false;

        ItemsCount[(int)SelectLobbyItem]++;
        OnBuyLobbyItem?.Invoke(SelectLobbyItem);

        BackEndServerManager.Instance.LobbyItemDiaLog(SelectLobbyItem, cashDia - CashDia, freeDia - FreeDia, CashDia, FreeDia);

        return true;
    }

    public bool BuyLobbyItem_Free()
    {
        ItemsCount[(int)SelectLobbyItem]++;
        LobbyItemGift.UseFreeCount();        // ����

        OnBuyLobbyItem?.Invoke(SelectLobbyItem);        // Select UI ����

        BackEndServerManager.Instance.LobbyItemFreeLog(SelectLobbyItem);

        return true;
    }

    #region DailyGiftData ȣ��

    private IDisposable[] disposables;

    public DailyGift LobbyItemGift => DailyGifts[EDailyGift.LobbyItem];

    public int LobbyItemFreeCount => DailyGifts[EDailyGift.LobbyItem].freeCount;

    public void UseDailyGift(EDailyGift type, Action useAction)
    {
        if(DailyGifts[type].CanUse())
        {   // ����
            useAction?.Invoke();
        }
        else
        {   // ����
            int titleNum = 0;         // ������ ����?
            switch (type)
            {
                case EDailyGift.TimeReward:
                    titleNum = 125;
                    break;
                case EDailyGift.Revive:
                    titleNum = 124;
                    break;
                case EDailyGift.LobbyItem:
                case EDailyGift.ItemGacha:
                case EDailyGift.Ticket:
                case EDailyGift.DoubleReward:
                    titleNum = 120;
                    break;
            }

            Firebase.Analytics.FirebaseAnalytics.LogEvent("Reward_Ad", "Ad_Name", type.ToString());

            if (type == EDailyGift.LobbyItem)
            {   // �κ� �������� ������
                useAction += () => DailyGifts[type].freeCount += LevelData.Instance.DailyGiftDatas[(int)EDailyGift.LobbyItem].chargeValue;
            }
            else
            {   // �κ� �������� ���� �� ���
                useAction += () => DailyGifts[type].UseAdCount();
            }

            SystemPopupUI.Instance.OpenAdvertise(type, titleNum, useAction);

            // ���� �Ҹ� �ʿ�
        }
    }

    public int ItemGachaFreeCount => DailyGifts[EDailyGift.ItemGacha].freeCount;
    public bool UseItemGacha() => DailyGifts[EDailyGift.ItemGacha].UseFreeCount();

    #endregion


    private void UpdateTicket(int value)
    {
        // Debug.Log($"Ƽ�� : {value}, �ð� : {TicketTime}");
        if(value < Values.MaxTicket)
        {
            // �ð� ���
            StopCoroutine(nameof(CoTicketTime));
            StartCoroutine(nameof(CoTicketTime));
        }
        else
        {
            // ���� ǥ��
            StopCoroutine(nameof(CoTicketTime));
        }

        BackEndServerManager.Instance.Update_Ticket(value);
    }

    /// <summary>
    /// 1�ʸ��� �ð� ���ҽ��Ѽ� Ticket ���� ����
    /// </summary>
    /// <returns></returns>
    IEnumerator CoTicketTime()
    {
        // ���� �ð� ���
        while(true)
        {
            yield return Values.Delay1;
            if(--TicketTime <= 0)
            {
                TicketTime = Values.TicketTime;
                Ticket++;
            }
        }
    }

    private void UpdateItem(int num, int value)
    {
        BackEndServerManager.Instance.Update_LobbyItem(num, value);
        //PlayerPrefs.SetInt(_Key_ItemCounts[num], value);
    }

    /// <summary>
    /// �������� �������� �α� ����� �ϱ�
    /// </summary>
    private void UpdateDia(EGoods goods, int value)
    {
        switch (goods)
        {
            case EGoods.FreeDia:
                BackEndServerManager.Instance.Update_FreeDia(value);
                break;
            case EGoods.CashDia:
                BackEndServerManager.Instance.Update_CashDia(value);
                break;
        }
    }

    private void UpdateDailyGifts(EDailyGift type, DailyGift dailyGift)
    {
        Debug.Log($"{type} - {dailyGift.freeCount} - {dailyGift.adCount}");
        BackEndServerManager.Instance.Update_DailyGift(type, dailyGift);
    }

    /// <summary>
    /// UI�� ���� ǥ���ؾ��ϱ⶧���� ������ ���� ������ �����ֱ�
    /// </summary>
    public void GameOver(int score, int maxCombo)
    {
        InputValue = 0;

        updateScore = score;
        updateCombo = maxCombo;

        if (BestScore < score)
        {
            BestScore = score;
        }

        if (BestMaxCombo < maxCombo)
        {
            BestMaxCombo = maxCombo;
        }

        //Debug.Log($"���1 : {BestScore} - {score} == {updateScore}");
        //Debug.Log($"���2 : {BestMaxCombo} - {maxCombo} == {updateCombo}");
    }

    public void EndGame()
    {
        IsGameStart = false;

        BackEndServerManager.Instance.Update_Result(updateScore, updateCombo);

        updateScore = 0;
        updateCombo = 0;
    }

    public bool CanGameStart => Ticket > 0;

    public void GameStart()
    {
        //if (!CanGameStart)
        //    return;

        --Ticket;
        IsGameStart = true;
    }

    // **Pause, Focus������ ����
    /// <summary>
    /// ������ ���� ����� ��� PlayerPrefs�� ����
    /// </summary>
    private void OnApplicationQuit()
    {
        PlayerPrefs.SetString(_Key_DateOfExit, DateTime.Now.ToString());
        PlayerPrefs.SetInt(_Key_TicketTime, TicketTime);
        foreach (var keyValuePair in DailyGifts)
        {
            PlayerPrefs.SetInt(keyValuePair.Key.ToString(), keyValuePair.Value.adDelay > 0 ? keyValuePair.Value.adDelay : 0);
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// ����Ͽ����� Ȩ��ư���� ������ �����ϴ� ��� Quit�� �ҷ����� ���ϱ� ������ Pause�� �ʿ�
    /// </summary>
    /// <param name="pause"></param>
    private void OnApplicationPause(bool pause)
    {
        if(pause)
        {
            PlayerPrefs.SetString(_Key_DateOfExit, DateTime.Now.ToString());
            PlayerPrefs.SetInt(_Key_TicketTime, TicketTime);
            foreach (var keyValuePair in DailyGifts)
            {
                PlayerPrefs.SetInt(keyValuePair.Key.ToString(), keyValuePair.Value.adDelay > 0 ? keyValuePair.Value.adDelay : 0);
            }

            PlayerPrefs.Save();
        }
        else
        {
            CheckTicketExitTime();

            foreach (var keyValuePair in DailyGifts)
            {
                CheckDailyGiftAdExitTime(keyValuePair.Key);
            }
        }
    }

    /// <summary>
    /// **Pause, Focus���� �߻� �� �ʱ�ȭ������ϱ⶧���� �������� ������ �ʿ䰡 ����
    /// (������ �������� ObserverCoroutine�� ���� ���Ѿ� �ϱ� ����)
    /// </summary>
    /// <param name="dailyGift"></param>
    public void SetAdTimer(EDailyGift dailyGift, int time = -1)
    {
        DailyGifts[dailyGift].adDelay = (time == -1) ? LevelData.Instance.DailyGiftDatas[(int)dailyGift].adDelay : time;

        if(disposables[(int)dailyGift] != null)
        {
            disposables[(int)dailyGift].Dispose();
        }

        disposables[(int)dailyGift] = 
            Observable.FromCoroutine<int>(observer => CoTimerObserver(observer, DailyGifts[dailyGift].adDelay))
                .Subscribe(value => DailyGifts[dailyGift].adDelay = value)
                .AddTo(gameObject);
    }

    IEnumerator CoTimerObserver(IObserver<int> observer, int delay)
    {
        while (delay > 0)
        {
            observer.OnNext(delay--);

            yield return Values.Delay1;
        }
        observer.OnNext(0);
        observer.OnCompleted();
    }

    //public void Timer_ItemGacha()
    //{
    //    Observable.FromCoroutine<int>(observer => CoTimerObserver(observer, DailyGifts[EDailyGift.ItemGacha].curDelay))
    //        .Subscribe(value => DailyGifts[EDailyGift.ItemGacha].curDelay = value)
    //        .AddTo(gameObject);
    //}

    //public void Timer_LobbyItem()
    //{
    //    LobbyItemAdDelay = DailyGifts[EDailyGift.LobbyItem].adDelay;
    //    Observable.FromCoroutine<int>(observer => CoTimerObserver(observer, LobbyItemAdDelay))
    //        .Subscribe(value => LobbyItemAdDelay = value)
    //        .AddTo(gameObject);
    //}

    //public void Timer_Revive()
    //{
    //    ReviveAdDelay = DailyGifts[EDailyGift.Revive].adDelay;
    //    Observable.FromCoroutine<int>(observer => CoTimerObserver(observer, ReviveAdDelay))
    //        .Subscribe(value => { ReviveAdDelay = value; 
    //            //Debug.Log($"ReviveAdDelay : {value}");
    //            })
    //        .AddTo(gameObject);
    //}

    //public void Timer_DoubleReward()
    //{
    //    DoubleRewardAdDelay = DailyGifts[EDailyGift.Revive].adDelay;
    //    Observable.FromCoroutine<int>(observer => CoTimerObserver(observer, DoubleRewardAdDelay))
    //        .Subscribe(value => { DoubleRewardAdDelay = value;
    //            //Debug.Log($"DoubleRewardAdDelay : {value}");
    //        })
    //        .AddTo(gameObject);
    //}
}
