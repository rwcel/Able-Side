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

    [HideInInspector] public int BestScore;
    [HideInInspector] public int BestMaxCombo;
    [HideInInspector] public int AccumulateScore;
    [HideInInspector] public int Ticket;
    [HideInInspector] public int TicketTime;

    [HideInInspector] public int PlayRewardCount;
    public DateTime TimeRewardTime;              // 분단위 표시 필요

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
    private static readonly string[] _Key_DailyGifts = 
    {
        EDailyGift.ItemGacha.ToString(), 
        EDailyGift.Ticket.ToString(), 
        EDailyGift.LobbyItem.ToString(), 
        EDailyGift.Revive.ToString(), 
        EDailyGift.DoubleReward.ToString(), 
        EDailyGift.TimeReward.ToString(), 
    };


    // **여기에 다 들어오는게 맞나?
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
                {
                    // GameManager가 intro씬에서 없기떄문에 직접 호출
                    AudioManager.Instance.PlayInGameBGM();
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
        BackEndServerManager.Instance.GetGameDatas(this);

        // GetProfileDatas();          // 이것도 서버로?
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

        // **OnStart이후 방식 변경으로 UniRx Skip이 더 빨리 적용되어 TicketTime이 적용되지 않는 문제
        UpdateTicket(Ticket);
    }

    public void CheckDailyGiftAdExitTime(EDailyGift dailyGift)
    {
        if (!PlayerPrefs.HasKey(_Key_DateOfExit)
            || !PlayerPrefs.HasKey(dailyGift.ToString()))
        {
            return;
        }

        // 계산
        var dateOfExit = DateTime.Parse(PlayerPrefs.GetString(_Key_DateOfExit));

        double totalSeconds = DateTime.Now.Subtract(dateOfExit).TotalSeconds;
        DailyGifts[dailyGift].adDelay = PlayerPrefs.GetInt(dailyGift.ToString()) - (int)totalSeconds;      // 남은시간 : 기존남은시간 - 나간시간

        if (DailyGifts[dailyGift].adDelay > 0)
        {
            Debug.Log($"타이머 재생 : {dailyGift} => {PlayerPrefs.GetInt(dailyGift.ToString())} - {totalSeconds}");
            SetAdTimer(dailyGift, DailyGifts[dailyGift].adDelay);
        }
        else
        {
            DailyGifts[dailyGift].adDelay = 0;
        }
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

    public bool BuyLobbyItem_Free()
    {
        ItemsCount[(int)SelectLobbyItem]++;
        LobbyItemGift.UseFreeCount();        // 감소

        OnBuyLobbyItem?.Invoke(SelectLobbyItem);        // Select UI 설정

        BackEndServerManager.Instance.LobbyItemFreeLog(SelectLobbyItem);

        return true;
    }

    #region DailyGiftData 호출

    private IDisposable[] disposables;

    public DailyGift LobbyItemGift => DailyGifts[EDailyGift.LobbyItem];

    public int LobbyItemFreeCount => DailyGifts[EDailyGift.LobbyItem].freeCount;

    public void UseDailyGift(EDailyGift type, Action useAction)
    {
        if(DailyGifts[type].CanUse())
        {   // 무료
            useAction?.Invoke();
        }
        else
        {   // 광고
            int titleNum = 0;         // 광고보고 보상?
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
            {   // 로비 아이템은 충전만
                useAction += () => DailyGifts[type].freeCount += LevelData.Instance.DailyGiftDatas[(int)EDailyGift.LobbyItem].chargeValue;
            }
            else
            {   // 로비 아이템은 충전 후 사용
                useAction += () => DailyGifts[type].UseAdCount();
            }

            SystemPopupUI.Instance.OpenAdvertise(type, titleNum, useAction);

            // 개수 소모 필요
        }
    }

    public int ItemGachaFreeCount => DailyGifts[EDailyGift.ItemGacha].freeCount;
    public bool UseItemGacha() => DailyGifts[EDailyGift.ItemGacha].UseFreeCount();

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

    // **Pause, Focus에서도 진행
    /// <summary>
    /// 서버에 보낼 방법이 없어서 PlayerPrefs에 저장
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
    /// 모바일에서는 홈버튼으로 나가서 삭제하는 경우 Quit을 불러오지 못하기 때문에 Pause가 필요
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
    /// **Pause, Focus에서 발생 시 초기화해줘야하기때문에 변수명을 저장할 필요가 있음
    /// (기존에 진행중인 ObserverCoroutine을 종료 시켜야 하기 때문)
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
