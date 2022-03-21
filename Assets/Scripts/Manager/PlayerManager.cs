//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class PlayerManager : Singleton<PlayerManager>
//{
//    public bool CanInput;               // 3초 준비 이후 가능한 입력

//    public int BestScore;
//    public int BestMaxCombo;
//    public int Ticket { get; private set; }
//    public int TicketTime;

//    public int[] ItemsCount;         // ELobbyItem

//    private static readonly string _Key_Ticket = "TicketCount";
//    private static readonly string _Key_TicketTime = "TicketRemainTime";
//    private static readonly string _Key_DateOfExit = "DateOfExit";
//    private static readonly string[] _Key_ItemCounts = { "ItemCount1", "ItemCount2", "ItemCount3", "ItemCount4", "ItemCount5", "ItemCount6" };

//    private void Awake()
//    {
//        ItemsCount = new int[Enum.GetValues(typeof(ELobbyItem)).Length];
//    }

//    private void Start()
//    {
//        AddListeners();

//        LoadData();
//    }

//    private void Update()
//    {
//        if (Input.GetKeyDown(KeyCode.T))
//        {
//            Ticket++;
//        }
//    }

//    private void AddListeners()
//    {
//        this.ObserveEveryValueChanged(_ => IsGameStart)
//            //.Skip(System.TimeSpan.Zero)
//            .Subscribe(isGameStart => {
//                if (isGameStart)
//                {
//                    // Debug.Log(isGameStart);
//                    CanInput = true;            // **Ready ~ Start 이후 적용 필요
//                }
//                OnGameStart?.Invoke(isGameStart);
//            })
//            .AddTo(this.gameObject);

//        this.ObserveEveryValueChanged(_ => Ticket)
//            .Skip(System.TimeSpan.Zero)
//            .Subscribe(value => UpdateTicket(value))
//            .AddTo(this.gameObject);

//        for (int i = 0, length = ItemsCount.Length; i < length; i++)
//        {
//            int num = i;
//            this.ObserveEveryValueChanged(_ => ItemsCount[num])
//                .Skip(System.TimeSpan.Zero)
//                .Subscribe(value =>
//                {
//                    // Debug.Log($"{num} {value}");
//                    UpdateItem(num, value);
//                })
//                .AddTo(this.gameObject);
//        }
//    }

//    private void LoadData()
//    {
//        if (PlayerPrefs.HasKey(_Key_Ticket))
//        {
//            Ticket = PlayerPrefs.GetInt(_Key_Ticket);
//        }
//        else
//        {
//            Ticket = LevelData.Instance.MaxTicket;
//        }

//        for (int i = 0, length = _Key_ItemCounts.Length; i < length; i++)
//        {
//            if (PlayerPrefs.HasKey(_Key_ItemCounts[i]))
//            {
//                ItemsCount[i] = PlayerPrefs.GetInt(_Key_ItemCounts[i]);
//                if (ItemsCount[i] == 0)
//                    ItemsCount[i] = 5;          // Test용 리셋;
//            }
//            else
//            {   // Base 0개?
//                ItemsCount[i] = 5;
//            }
//        }

//        // **DateOfExit으로 계산 필요 : 나갔다 온 시간에 맞춰 티켓 갯수 증가
//        CheckDateOfExitTime();

//        UpdateTicket(Ticket);
//    }

//    private void CheckDateOfExitTime()
//    {
//        LevelData levelData = LevelData.Instance;

//        if (!PlayerPrefs.HasKey(_Key_DateOfExit)
//            || !PlayerPrefs.HasKey(_Key_TicketTime))
//        {
//            TicketTime = levelData.TicketTime;
//            return;
//        }

//        var dateOfExit =
//        (PlayerPrefs.GetString(_Key_DateOfExit));

//        double totalSeconds = DateTime.Now.Subtract(dateOfExit).TotalSeconds;
//        TicketTime = PlayerPrefs.GetInt(_Key_TicketTime);

//        Debug.Log($"{totalSeconds} / {TicketTime}");

//        // 전체보다 많으면 -> 무조건 전체
//        if (totalSeconds >= levelData.TicketTime * levelData.MaxTicket)
//        {
//            Ticket = levelData.MaxTicket;
//        }
//        else
//        {
//            if (TicketTime > (int)totalSeconds)
//            {
//                TicketTime -= (int)totalSeconds;
//            }
//            else
//            {
//                int value = (int)totalSeconds - TicketTime;
//                while (value >= levelData.TicketTime)
//                {
//                    if (++Ticket >= levelData.MaxTicket)
//                        break;
//                    value -= levelData.TicketTime;
//                }

//                TicketTime = levelData.TicketTime - value;
//            }
//        }
//    }

//    private void UpdateTicket(int value)
//    {
//        if (value < LevelData.Instance.MaxTicket)
//        {
//            // 시간 계산
//            StopCoroutine(nameof(CoTicketTime));
//            StartCoroutine(nameof(CoTicketTime));
//        }
//        else
//        {
//            // 개수 표기
//            StopCoroutine(nameof(CoTicketTime));
//        }

//        Debug.Log("티켓 수 갱신 : " + value);
//        PlayerPrefs.SetInt(_Key_Ticket, value);
//    }

//    /// <summary>
//    /// 1초마다 시간 감소시켜서 Ticket 개수 증가
//    /// </summary>
//    /// <returns></returns>
//    IEnumerator CoTicketTime()
//    {
//        // 남은 시간 계산
//        while (true)
//        {
//            yield return Values.Delay1;
//            if (--TicketTime <= 0)
//            {
//                TicketTime = LevelData.Instance.TicketTime;
//                Ticket++;
//            }
//        }
//    }

//    private void UpdateItem(int num, int value)
//    {
//        PlayerPrefs.SetInt(_Key_ItemCounts[num], value);
//    }

//    public void GameOver(int score, int maxCombo)
//    {
//        CanInput = false;

//        if (BestScore < score)
//            BestScore = score;

//        if (BestMaxCombo < maxCombo)
//            BestMaxCombo = maxCombo;
//    }

//    public bool CanGameStart()
//    {
//        if (Ticket <= 0)
//        {
//            return false;
//        }

//        --Ticket;
//        return true;
//    }

//    private void OnApplicationQuit()
//    {
//        PlayerPrefs.SetString(_Key_DateOfExit, DateTime.Now.ToString());
//        PlayerPrefs.SetInt(_Key_TicketTime, TicketTime);
//    }
//}
