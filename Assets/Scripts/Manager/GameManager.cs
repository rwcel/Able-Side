using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

/// <summary>
/// **GameManager의 역할이 너무 많음
/// PlayerManager등에게 역할 분담 필요 : Player의 정보들
/// </summary>
public class GameManager : Singleton<GameManager>
{
    public GameController GameController;
    public GameCharacter GameCharacter;

    [HideInInspector] public bool IsGameStart;             // 게임 시작. 
    [HideInInspector] public int InputValue = 0;            // 3초 준비 이후 가능한 입력   *여러번 걸릴 수 있어서 int처리 
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
    public System.Action<ELobbyItem> OnBuyLobbyItem;             // 아이템 구입 시 선택되게


    // *나갈때 데이터라서 서버 관리로 해결할 수 없다
    private static readonly string _Key_TicketTime = "TicketRemainTime";
    private static readonly string _Key_DateOfExit = "DateOfExit";

    // **여기에 다 들어오는게 맞나?
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

        GamePopup.Instance.OpenPopup(EGamePopup.Notice);            // *처음 접속 1회

        UnityAdsManager.Instance.ShowBannerAD();
    }

    private void AddListeners()
    {
        /// *게임 시작은 Input과 관계없기때문에 0,1로만 구성되어야함
        this.ObserveEveryValueChanged(_ => IsGameStart)
            .Subscribe(isGameStart => {
                if (!isGameStart)
                {
                    InputValue = 0;
                    AudioManager.Instance.PlayLobbyBGM();
                }
                else
                {   // GameManager가 intro씬에서 없기떄문에 직접 호출
                    AudioManager.Instance.PlaySFX(ESFX.ReadyGo);
                    AudioManager.Instance.PlayInGameBGM();
                    //AudioManager.Instance.StopBGM();
                }
                OnGameStart?.Invoke(isGameStart);
            })
            .AddTo(this.gameObject);

        // **Skip 안하면 데이터 0으로 초기화 됨
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

        // **Dictionary 안 클래스라 데이터 특정이 어려움
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

        // GetProfileDatas();          // 이것도 서버로?
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
            // 전체보다 많으면 -> 무조건 전체
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

        // **OnStart이후 방식 변경으로 UniRx Skip이 더 빨리 적용되어 TicketTime이 적용되지 않는 문제
        UpdateTicket(Ticket);
    }

    /// <summary>
    /// 유료 재화 먼저 사용
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
        // 다이아 변화량 로그에 기록
        int cashDia = CashDia;
        int freeDia = FreeDia;

        // 다이아 개수 확인 + 소모
        if (!UseDia(value))
            return false;

        Ticket += Values.Ticket_BuyValue;

        BackEndServerManager.Instance.TicketDiaLog(cashDia - CashDia, freeDia - FreeDia, CashDia, FreeDia);

        return true;
    }

    public bool BuyLobbyItem_Dia(int value)
    {
        // 다이아 변화량 로그에 기록
        int cashDia = CashDia;
        int freeDia = FreeDia;

        // 다이아 개수 확인 + 소모
        if (!UseDia(value))
            return false;

        ItemsCount[(int)SelectLobbyItem]++;
        OnBuyLobbyItem?.Invoke(SelectLobbyItem);

        BackEndServerManager.Instance.LobbyItemDiaLog(SelectLobbyItem, cashDia - CashDia, freeDia - FreeDia, CashDia, FreeDia);

        return true;
    }

    #region DailyGiftData 호출

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
                                                                , Timer_LobbyItem                              // 타이머
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
        // Debug.Log($"티켓 : {value}, 시간 : {TicketTime}");
        if(value < Values.MaxTicket)
        {
            // 시간 계산
            StopCoroutine(nameof(CoTicketTime));
            StartCoroutine(nameof(CoTicketTime));
        }
        else
        {
            // 개수 표기
            StopCoroutine(nameof(CoTicketTime));
        }

        BackEndServerManager.Instance.Update_Ticket(value);
    }

    /// <summary>
    /// 1초마다 시간 감소시켜서 Ticket 개수 증가
    /// </summary>
    /// <returns></returns>
    IEnumerator CoTicketTime()
    {
        // 남은 시간 계산
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
    /// 아이템을 샀을때만 로그 남기게 하기
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
    /// UI에 먼저 표시해야하기때문에 서버는 게임 나갈때 보내주기
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

        //Debug.Log($"결과1 : {BestScore} - {score} == {updateScore}");
        //Debug.Log($"결과2 : {BestMaxCombo} - {maxCombo} == {updateCombo}");
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
        // 서버에 보낼 방법이 없음. -> Player가 가지고 있어야?

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
