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

    public int BestScore;
    public int BestMaxCombo;
    public int AccumulateScore;
    public int Ticket;
    [HideInInspector] public int TicketTime;

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

    // **���⿡ �� �����°� �³�?
    public ELobbyItem SelectLobbyItem;
    public FPostInfo SelectPostInfo;
    public string SelectSerialCode;

    private int updateScore, updateCombo;

    protected override void AwakeInstance() 
    {
        ItemsCount = new int[Enum.GetValues(typeof(ELobbyItem)).Length];

        foreach (EDailyGift item in Enum.GetValues(typeof(EDailyGift)))
        {
            DailyGifts.Add(item, new DailyGift(0, 0, 0, 0));
        }
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
                {   // GameManager�� intro������ ���⋚���� ���� ȣ��
                    AudioManager.Instance.PlaySFX(ESFX.ReadyGo);
                    AudioManager.Instance.PlayInGameBGM();
                    //AudioManager.Instance.StopBGM();
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
        BackEndServerManager.Instance.GetChartLists();

        BackEndServerManager.Instance.GetGameDatas(this);

        // GetProfileDatas();          // �̰͵� ������?
    }

    public void CheckDateOfExitTime()
    {
        if (!PlayerPrefs.HasKey(_Key_DateOfExit) 
            || !PlayerPrefs.HasKey(_Key_TicketTime))
        {
            TicketTime = Values.TicketTime;
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
                        if (++Ticket >= Values.MaxTicket)
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

    #region DailyGiftData ȣ��

    public DailyGift LobbyItemGift => DailyGifts[EDailyGift.LobbyItem];

    public int LobbyItemFreeCount => DailyGifts[EDailyGift.LobbyItem].freeCount;
    public int LobbyItemAdCount => DailyGifts[EDailyGift.LobbyItem].adCount;
    public int LobbyItemCharge => DailyGifts[EDailyGift.LobbyItem].chargeValue;
    [HideInInspector] public int LobbyItemAdDelay = 0;

    public bool BuyLobbyItem_Free()
    {
        if (!LobbyItemGift.CanUse())
            return false;

        ItemsCount[(int)SelectLobbyItem]++;
        OnBuyLobbyItem?.Invoke(SelectLobbyItem);

        LobbyItemGift.UseGift();

        BackEndServerManager.Instance.LobbyItemFreeLog(SelectLobbyItem);

        return true;
    }

    public void ChargeLobbyItem()
    {
        if (!LobbyItemGift.CanCharge())
            return;

        UnityAdsManager.Instance.ShowRewardAD(() => LobbyItemGift.Charge()
                                                                , Timer_LobbyItem                              // Ÿ�̸�
                                                                , EDailyGift.LobbyItem);
    }

    public bool CanUseItemGacha => DailyGifts[EDailyGift.ItemGacha].CanUse();
    public int ItemGachaFreeCount => DailyGifts[EDailyGift.ItemGacha].freeCount;
    public int ItemGachaAdCount => DailyGifts[EDailyGift.ItemGacha].adCount;
    public bool UseItemGacha() => DailyGifts[EDailyGift.ItemGacha].UseGift();
    public bool ChargeItemGacha => DailyGifts[EDailyGift.ItemGacha].Charge();
    [HideInInspector] public int ItemGachaAdDelay = 0;
    // [HideInInspector] public ReactiveProperty<int> _ItemGachaTimerReactiveProperty;

    public int ReviveCount => DailyGifts[EDailyGift.Revive].adCount;
    public int DoubleRewardCount => DailyGifts[EDailyGift.DoubleReward].adCount;
    public bool CanRevive => DailyGifts[EDailyGift.Revive].CanUse() || DailyGifts[EDailyGift.Revive].CanCharge();
    public bool CanDoubleReward => DailyGifts[EDailyGift.DoubleReward].CanUse() || DailyGifts[EDailyGift.DoubleReward].CanCharge();
    public bool CanUseRevive => DailyGifts[EDailyGift.Revive].CanUse();
    public bool CanUseDoubleReward => DailyGifts[EDailyGift.DoubleReward].CanUse();
    [HideInInspector] public int ReviveAdDelay = 0; //=> DailyGifts[EDailyGift.Revive].adDelay;
    [HideInInspector] public int DoubleRewardAdDelay = 0; // => DailyGifts[EDailyGift.DoubleReward].adDelay;
    public bool ChargeRevive()
    {
        if (ReviveAdDelay > 0)
            return false;

        if (DailyGifts[EDailyGift.Revive].Charge())
        {
            DailyGifts[EDailyGift.Revive].UseGift();
            return true;
        }
        return false;
    }
    public bool ChargeDoubleReward()
    {
        if (DoubleRewardAdDelay > 0)
            return false;

        if (DailyGifts[EDailyGift.DoubleReward].Charge())
        {
            DailyGifts[EDailyGift.DoubleReward].UseGift();
            return true;
        }
        return false;
    }

    public bool CanUseTicket => DailyGifts[EDailyGift.Ticket].CanUse();
    public int TicketAdCount => DailyGifts[EDailyGift.Ticket].adCount;
    public int TicketAdDelay => DailyGifts[EDailyGift.Ticket].adDelay;
    public bool UseTicket() => DailyGifts[EDailyGift.Ticket].UseGift();
    public bool ChargeTicket => DailyGifts[EDailyGift.Ticket].ChargeAndUse();

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

    private void OnApplicationQuit()
    {
        // ������ ���� ����� ����. -> Player�� ������ �־��?

        PlayerPrefs.SetString(_Key_DateOfExit, DateTime.Now.ToString());
        PlayerPrefs.SetInt(_Key_TicketTime, TicketTime);
    }


    public void Timer_ItemGacha()
    {
        ItemGachaAdDelay = DailyGifts[EDailyGift.ItemGacha].adDelay;
        Observable.FromCoroutine<int>(observer => CoTimerObserver(observer, ItemGachaAdDelay))
            .Subscribe(value => ItemGachaAdDelay = value)
            .AddTo(gameObject);

        //_ItemGachaTimerReactiveProperty = new ReactiveProperty<int>(ItemGachaAdDelay);

        ////_ItemGachaTimerReactiveProperty.Subscribe(value => Debug.Log($"Timer : {value}"));

        //Observable.Interval(TimeSpan.FromSeconds(1))
        //    .Subscribe(_ => 
        //    {
        //        if(_ItemGachaTimerReactiveProperty.Value > 0)
        //            _ItemGachaTimerReactiveProperty.Value--;
        //    })
        //    .AddTo(gameObject);
    }

    public void Timer_LobbyItem()
    {
        LobbyItemAdDelay = DailyGifts[EDailyGift.LobbyItem].adDelay;
        Observable.FromCoroutine<int>(observer => CoTimerObserver(observer, LobbyItemAdDelay))
            .Subscribe(value => LobbyItemAdDelay = value)
            .AddTo(gameObject);
    }

    public void Timer_Revive()
    {
        ReviveAdDelay = DailyGifts[EDailyGift.Revive].adDelay;
        Observable.FromCoroutine<int>(observer => CoTimerObserver(observer, ReviveAdDelay))
            .Subscribe(value => { ReviveAdDelay = value; 
                //Debug.Log($"ReviveAdDelay : {value}");
                })
            .AddTo(gameObject);
    }

    public void Timer_DoubleReward()
    {
        DoubleRewardAdDelay = DailyGifts[EDailyGift.Revive].adDelay;
        Observable.FromCoroutine<int>(observer => CoTimerObserver(observer, DoubleRewardAdDelay))
            .Subscribe(value => { DoubleRewardAdDelay = value;
                //Debug.Log($"DoubleRewardAdDelay : {value}");
            })
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
}
