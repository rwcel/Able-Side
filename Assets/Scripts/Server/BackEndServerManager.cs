using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Facebook.Unity;
#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#elif UNITY_IOS
using UnityEngine.SignInWithApple;
#endif
using BackEnd;
using static BackEnd.SendQueue;
using BackEnd.GlobalSupport;
using LitJson;
using UniRx;

public class DailyGift
{
    public int freeCount;
    public int adCount;
    public int adDelay;

    public DailyGift() { }

    public DailyGift(int freeCount, int adCount)
    {
        this.freeCount = freeCount;
        this.adCount = adCount;
        this.adDelay = 0;           // 접속 시 계산 처리
    }

    public bool CanUse() => freeCount > 0;
    public bool CanCharge() => adCount > 0;

    public bool UseFreeCount()
    {
        if (!CanUse())
            return false;

        --freeCount;

        return true;
    }

    public bool UseAdCount()
    {
        if (!CanCharge())
            return false;

        --adCount;

        // FreeCount += ChargeValue

        return true;
    }

    public bool ChargeAndUse()
    {
        if (UseAdCount())
        {
            if(UseFreeCount())
            {
                return true;
            }
        }

        return false;
    }
}

public struct FItemInfo
{
    public EItem item;
    public Sprite icon;
    public int count;           // *Value는 필요없어서 제거

    public FItemInfo(EItem item, Sprite icon, int count)
    {
        this.item = item;
        this.icon = icon;
        this.count = count;
    }
}


public class BackEndServerManager : Singleton<BackEndServerManager>
{
    [SerializeField] LoginUI _LoginUI;

    private static readonly int _Code_SignUp = 201;
    private static readonly int _Code_Login = 200;
    private static readonly int _Code_DuplicateLogin = 409;

    #region 게임 데이터

    private string currentVersion = "";

    private string nickName = "";                       // 닉네임
    private string gamerID = "";                        // UUID
    private ELogin loginType;
    private bool isProgressLogin;
    private string serverDate;
    private bool isDailyReset = false;
    private int profileIcon = 0;
    private bool canSerial = false;
    private ELanguage language;
    private Dictionary<string, int> serialScoreList = new Dictionary<string, int>();
    private Dictionary<string, string> serialCodeList = new Dictionary<string, string>();
    private bool buyWeekly = false;
    private DateTime serverDateTime;
    private List<FItemInfo> recvItems;

    public DateTime GameStartTime;

    public string NickName => nickName;//Backend.UserNickName;
    public string UUID => gamerID;
    public ELogin LoginType => loginType;
    public string UserIndate => Backend.UserInDate;               // UserInDate가 SDK용. Gamer_id는 조회 불가
    public int ProfileIcon => profileIcon;
    public ELanguage Language => language;
    public Dictionary<string, int> SerialScoreList => serialScoreList;
    public int SerialScore(string key) => serialScoreList[key];
    public bool BuyWeekly => buyWeekly;
    public string ServerDate => serverDate;

    public List<FItemInfo> RecvItems => recvItems;

    #endregion 게임 데이터

    #region GM Data

    private static readonly string gm_nickname = "player";          // *GM지울경우 꼭 설정을 다시 해야함
    private string gm_ownerInDate;
    private string gm_inDate;

    #endregion

    protected override void AwakeInstance()
    {
        DontDestroyOnLoad(gameObject);
        //var obj = FindObjectsOfType<BackEndServerManager>();
        //if (obj.Length == 1)
        //    DontDestroyOnLoad(gameObject);
        //else
        //{
        //    Destroy(gameObject);
        //}

        recvItems = new List<FItemInfo>(Values.Max_Reward_Items);
    }

    protected override void DestroyInstance() { }

    private void Start()
    {
        // *LocalizationManager가 CI 씬에 있기 때문에 여기서 처리를 해줘야함. 거기서 처리하는 순간 생성때문에 _LoginUI가 사라짐
        this.ObserveEveryValueChanged(_ => Language)
            .Subscribe(value => LocalizationManager.Instance.OnLocalize(value))
            .AddTo(this.gameObject);

        // 인터넷 문제
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            _LoginUI.ShowUpdateUI();
            return;
        }

#if UNITY_ANDROID
        // GPGS 플러그인 설정
        var config = new PlayGamesClientConfiguration
            .Builder()
            .RequestServerAuthCode(false)
            .RequestEmail()                     // 이메일 권한
            .RequestIdToken()
            .Build();

        PlayGamesPlatform.InitializeInstance(config);
        PlayGamesPlatform.DebugLogEnabled = true;           // 디버그로그 확인
        PlayGamesPlatform.Activate();
#endif
#if !UNITY_EDITOR
        // Facebook 설정
        if(!FB.IsInitialized)
        {
            FB.Init(InitCallback, OnHideUnity);
        }
        else
        {
            FB.ActivateApp();
        }
#endif

        // *처음시작단계에서 profile을 못가져 오기때문에 시스템 언어 사용
        switch (Application.systemLanguage)
        {
            case SystemLanguage.Korean:
                language = ELanguage.Korean;
                break;
            default:
                language = ELanguage.English;
                break;
        }

        currentVersion = Application.version;

        var bro = Backend.Initialize(true);
        if(bro.IsSuccess())
        {
            if (!string.IsNullOrEmpty(Backend.Utils.GetGoogleHash()))
                Debug.Log("HashKey : " + Backend.Utils.GetGoogleHash());
            // Debug.Log(Backend.Utils.GetGoogleHash());

            var servertime = Backend.Utils.GetServerTime();

            serverDateTime = DateTime.Parse(servertime.GetReturnValuetoJSON()["utcTime"].ToString());
            serverDate = serverDateTime.ToString("yyyy-MM-dd");
            //Debug.Log("서버타임 : " + serverDateTime);

            // ShowPolicy();
            CheckApplicationVersion();
        }
        else
        {
            Debug.Log("초기화 실패 - " + bro);               // 인터넷 연결 등의 문제이므로 게임 종료
            SystemPopupUI.Instance.OpenOneButton(14, 53, 2, GameApplication.Instance.Quit);
        }
    }

    private void Update()
    {
        Dispatcher.Instance.InvokePending();
        if (Backend.IsInitialized)
        {
            Backend.AsyncPoll();
        }
    }

    // Dispatcer에서 action 실행 (메인스레드)
    private void DispatcherAction(Action action)
    {
        Dispatcher.Instance.Invoke(action);
    }

    #region 접속

    private void CheckApplicationVersion()
    {
        Backend.Utils.GetLatestVersion(versionBRO =>
        {
            if (versionBRO.IsSuccess())         // 에디터에서는 안됨
            {
                string latest = versionBRO.GetReturnValuetoJSON()["version"].ToString();
                Debug.Log("version info - current: " + currentVersion + " latest: " + latest);
                if (currentVersion.VersionCheck() < latest.VersionCheck())
                {
                    Debug.Log("버전 낮음");
                    // 버전이 다른 경우
                    int type = (int)versionBRO.GetReturnValuetoJSON()["type"];
                    // type = 1 : 선택, type = 2 : 강제
                    if(type == 2)
                    {
                        DispatcherAction(_LoginUI.ShowUpdateUI);
                    }
                }
            }
        });
    }

    /// <summary>
    /// 버튼 클릭 시 로그인 확인
    /// </summary>
    public void LoginWithTheBackendToken()
    {
        if (_LoginUI == null)
        {   // 씬 재로드 시 발생하는 문제
            _LoginUI = FindObjectOfType<LoginUI>();
        }

        var bro = Backend.BMember.LoginWithTheBackendToken();
        if (bro.IsServerError())
        {
            Debug.LogWarning("서버 상태 불안정");
        }

        if (bro.IsSuccess())
        {
            OnBackendAuthorized();
        }
        else
        {
            Debug.Log("로그인 실패 - " + bro.ToString());
            DispatcherAction(_LoginUI.ShowLoginUI);
        }
    }

    public void OnBackendAuthorized()
    {
        GetUserInfo();  // 유저정보 가져오기 후 변수에 저장
    }

    public void GetIntroDatas()
    {
        GetChartLists();            // Time Reset보다 빨리 불러와야함
        GetData_Time();
        GetGMData();

        DispatcherAction(_LoginUI.AnimStart);
    }

    public void GetUserInfo()
    {
        Backend.BMember.GetUserInfo(userInfoBro =>
        {
            if (userInfoBro.IsSuccess())
            {
                JsonData Userdata = userInfoBro.GetReturnValuetoJSON()["row"];

                var subscriptionType = Userdata["subscriptionType"].ToString();
                if (subscriptionType == "google")
                {
                    loginType = ELogin.Google;
                }
                else if (subscriptionType == "facebook")
                {
                    loginType = ELogin.Facebook;
                }
                else if (subscriptionType == "customSignUp")
                {
                    loginType = ELogin.Guest;
                }

                Debug.Log("LoginType : " + loginType);

                JsonData nicknameJson = Userdata["nickname"];
                if (nicknameJson != null)
                {
                    nickName = nicknameJson.ToString();
                    gamerID = Userdata["gamerId"].ToString();

                    DispatcherAction(_LoginUI.CloseAll);
                    isProgressLogin = false;

                    GetIntroDatas();
                    // GameSceneManager.Instance.SceneChange(GameSceneManager.EScene.InGame);
                }
                else
                {
                    Debug.Log("Not Nickname");
                    DispatcherAction(_LoginUI.ShowPrivacyUI);           // 약관동의부터 다시 시작하게
                }
            }
            else
            {
                Debug.Log("[X]유저 정보 가져오기 실패 - " + userInfoBro);
            }
        });
    }

    #endregion 접속

    #region 회원가입

    public void GoogleLogin()
    {
        if (isProgressLogin)
            return;

        isProgressLogin = true;
        loginType = ELogin.Google;
        if(Social.localUser.authenticated)
        {
            var bro = Backend.BMember.AuthorizeFederation(GetTokens(ELogin.Google), FederationType.Google, "gpgs");
            OnBackendAuthorized();
        }
        else
        {
            Social.localUser.Authenticate((bool success) => 
            {
                if (success)
                {
                    var bro = Backend.BMember.AuthorizeFederation(GetTokens(ELogin.Google), FederationType.Google, "gpgs");
                    if (bro.IsSuccess())
                    {
                        if (int.Parse(bro.GetStatusCode()) == _Code_SignUp)
                        {
                            // 회원가입
                            _LoginUI.ShowPrivacyUI();
                            isProgressLogin = false;
                        }
                        else if (int.Parse(bro.GetStatusCode()) == _Code_Login)
                        {
                            // 로그인
                            OnBackendAuthorized();
                        }
                    }
                }
                else
                {
                    Debug.LogError("로그인 실패");
                    isProgressLogin = false;
                }
            });
        }
    }

    private string GetTokens(ELogin type)
    {
        if(type == ELogin.Google)
        {
            if (PlayGamesPlatform.Instance.localUser.authenticated)
            {
                Debug.Log("토큰 ID : " + PlayGamesPlatform.Instance.GetIdToken());
                return PlayGamesPlatform.Instance.GetIdToken();
            }
            else
            {
                Debug.LogError("접속되어있지 않습니다!");
                return null;
            }
        }
        else if(type == ELogin.Facebook)
        {
            var aToken = Facebook.Unity.AccessToken.CurrentAccessToken;
            //string facebookToken = aToken.TokenString;

            return aToken.TokenString;
        }

        return null;
    }

    // 페이스북 회원가입
    private void InitCallback()
    {
        if(FB.IsInitialized)
        {
            FB.ActivateApp();
        }
        else
        {
            Debug.LogWarning("Facebook SDK 여는데 실패");
        }
    }

    private void OnHideUnity(bool isGameShown)
    {
        Time.timeScale = !isGameShown ? 0 : 1;
    }

    public void FacebookLogin()
    {
        if (isProgressLogin)
            return;

        var perms = new List<string>() { "public_profile", "email" };
        FB.LogInWithReadPermissions(perms, AuthCallback);
    }

    private void AuthCallback(ILoginResult result)
    {
        if (FB.IsLoggedIn)
        {
            // 뒤끝 서버로 가입요청
            isProgressLogin = true;
            loginType = ELogin.Facebook;
            if (Social.localUser.authenticated)
            {
                var bro = Backend.BMember.AuthorizeFederation(GetTokens(ELogin.Facebook), FederationType.Facebook);
                OnBackendAuthorized();
            }
            else
            {
                Social.localUser.Authenticate((bool success) =>
                {
                    if (success)
                    {
                        var bro = Backend.BMember.AuthorizeFederation(GetTokens(ELogin.Facebook), FederationType.Facebook);
                        if (bro.IsSuccess())
                        {
                            if (int.Parse(bro.GetStatusCode()) == _Code_SignUp)
                            {
                                // 회원가입
                                _LoginUI.ShowPrivacyUI();
                                isProgressLogin = false;
                            }
                            else if (int.Parse(bro.GetStatusCode()) == _Code_Login)
                            {
                                // 로그인
                                OnBackendAuthorized();
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("로그인 실패");
                        isProgressLogin = false;
                    }
                });
            }
        }
    }


    /// <summary>
    /// 게스트 회원가입
    /// *동기 처리
    /// </summary>
    public void GuestLogin()
    {
        if (isProgressLogin)
            return;

        isProgressLogin = true;
        loginType = ELogin.Guest;
        var bro = Backend.BMember.GuestLogin();
        if (bro.IsSuccess())
        {
            if(int.Parse(bro.GetStatusCode()) == _Code_SignUp)
            {
                // 회원가입
                _LoginUI.ShowPrivacyUI();
                isProgressLogin = false;
            }
            else if(int.Parse(bro.GetStatusCode()) == _Code_Login)
            {
                // 로그인
                OnBackendAuthorized();
                Debug.Log("게스트 로그인 성공");
            }
        }
        else
        {
            // **로그 남기기 불가능 : 로그인을 해야 그 뒤에 InsertLog가 가능

            Debug.LogError("게스트 로그인 실패 : " + bro);

            // 정보 삭제
            Backend.BMember.DeleteGuestInfo();

            SystemPopupUI.Instance.OpenOneButton(14, 114, 2, GameSceneManager.Instance.Restart);
            // 기존 : GameApplication.Instance.Quit

            isProgressLogin = false;

        }
    }

#endregion 회원가입

    #region 닉네임

    public string GetNickName()
    {
        return nickName;
    }

    public int DuplicateNickNameCheck(string _newNickName)
    {
        // *닉네임 중복 : 동기
        BackendReturnObject bro = Backend.BMember.CheckNicknameDuplication(_newNickName);
        if (bro.IsSuccess())
        {
            return 0;
        }
        return int.Parse(bro.GetStatusCode());
    }

    public void UpdateNickname(string _newNickName)
    {
        var before = nickName;
        Backend.BMember.UpdateNickname(_newNickName, nickNameBro =>
        {
            if (nickNameBro.IsSuccess())
            {
                nickName = _newNickName;
                // DispatcherAction(BackEndUIManager.instance.CloseAll);

                if(before != "")
                {
                    Param param = new Param();
                    param.Add("기존 닉네임", before);
                    param.Add("현재 닉네임", _newNickName);
                    InsertLog("닉네임 변경 완료", param);
                }
            }
            else
            {
                Debug.Log("닉네임 변경 실패 : " + nickNameBro);
            }
        });
    }

#endregion 닉네임

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region 테이블 

    private string LobbyItemTableName = "LobbyItems";           // column은 enum값으로 search

    private string TicketTableName = "Ticket";
    private string TicketColumnName = "ticket", TicketTimeColumnName = "ticketTime";

    private string DiaTableName = "Dia";
    private string CashDiaColumnName = "cashDia", FreeDiaColumnName = "freeDia";

    private string FeverTableName = "Fever";
    private string FeverColumnName = "fever";

    private string DailyGiftTableName = "DailyGift";
    private string FreeCountColumnName = "freeCount", AdCountColumnName = "adCount";
    private string ChargeValueColumnName = "chargeValue", AdDelayColumnName = "adDelay";

    private string ResultTableName = "Result";
    private string ScoreColumnName = "score", ComboColumnName = "maxCombo", AccumulateScoreColumnName = "acScore";
    // **누적점수 초기화 하면 안됨. -> 프로필 조건으로 사용할 예정

    private string TimeTableName = "Time";
    private string LastLoginColumnName = "lastLoginTime";

    private string ProfileTableName = "Profile";
    private string IconColumnName = "SelectIcon";
    private string LanguageColumnName = "Language";
    private string UnlockSerialColumnName = "unlockSerial";
    private string BuyWeeklyColumnName = "buyWeekly";
    private string PushAlarmColumnName = "pushAlarm";

    private string SerialTableName = "Serial";
    private string SerialScoreColumnName = "serialScore";
    private string SerialCodeColumnName = "serialCode";

    private string SerialChartTableName = "SerialChart";
    private string SerialChartColumnName = "num";
    private string key_SerialChart = "";

    private string RewardTableName = "Reward";
    private string PlayRewardColumnName = "playReward";
    private string EndTimeRewardColumnName = "endTimeReward";                    // 받을 수 있는 시간

    //private static readonly string resultRankUUID = "12e8c840-a405-11ec-afae-c5cd66ba001b";                   // 통합 월간 랭킹
    private static readonly string resultRankUUID = "ffc6cda0-b158-11ec-84cc-158e3b4dbc64";                   // 데이터 초기화 x

    private static readonly string key_ItemGacha_Probability = "4392";
    private static readonly string key_PlayReward_Probability = "4472";
    private static readonly string key_TimeReward_Probability = "4469";

    private bool isAlarm;           // 푸시알림
    public bool IsAlarm => isAlarm;

    private string userInDate_Result;                // 유저 데이터
    private string userInDate_Profile;

    private bool isUpdatedRank;

    #endregion

    #region 게임 데이터 세팅

    /* 데이터 사용 시 ["N"] ["S"] 등의 값에 대해
     BOOL	bool	        boolean 형태의 데이터가 이에 해당됩니다.
     N	        numbers	    int, float, double 등 모든 숫자형 데이터는 이에 해당됩니다.
     S	        string	        string 형태의 데이터가 이에 해당됩니다.
     L	        list            	list 형태의 데이터가 이에 해당됩니다.
     M	        map	        map, dictionary 형태의 데이터가 이에 해당됩니다.
     NULL   	null	        값이 존재하지 않는 경우 이에 해당됩니다.
    https://developer.thebackend.io/unity3d/guide/gameDataV3/getMyData/where/
     */

    GameManager _GameManager;
    private int loadCount;

    public void GetGameDatas(GameManager gameManager)
    {
        _GameManager = gameManager;
        loadCount = 0;

        //GetData_Time();             // 이것에 따라 DailyGift Reset

        // 데이터들 세팅
        GetData_Profile();
        GetData_Ticket();            // 게임 플레이 요소
        GetData_Dia();               // 재화
        GetData_Fever();            // 인게임 아이템 요소
        GetData_LobbyItem();     // 로비 아이템 요소
        GetData_DailyGift();
        GetData_Result();
        GetData_Serial();
        GetData_SerialChart();
        GetData_Reward();

        StartCoroutine(nameof(CoGetDatas));
        // GameSceneManager.Instance.SceneChange(GameSceneManager.EScene.InGame, GameManager.Instance.OnStart);
    }

    IEnumerator CoGetDatas()
    {
        while(loadCount > 0)
        {
            yield return null;
            //Debug.Log($"loadCount : {loadCount}");
        }

        Debug.Log("GameManager Load");
        _GameManager.OnLoaded();
    }

    #region 시간

    public void GetData_Time()
    {
        var bro = Backend.GameData.GetMyData(TimeTableName, new Where());

        if (!bro.IsSuccess())
            return;

        if (bro.GetReturnValuetoJSON()["rows"].Count <= 0)
        {
            Insert_Time();
            return;
        }

        var rows = bro.FlattenRows();

        var lastLoginTime = DateTime.Parse(rows[0][LastLoginColumnName].ToString()).ToString("yyyy-MM-dd");
        //"2022-02-16";
        Debug.Log($"{serverDate} != {lastLoginTime} ?");
        if (lastLoginTime != serverDate)
        {
            isDailyReset = true;
            // 시간 업데이트
            Param param = new Param();
            param.Add(LastLoginColumnName, serverDate);

            Enqueue(Backend.GameData.Update, TimeTableName, new Where(), param, updateTimeBro =>
            {
                Debug.Log("UpdateTimeBro : " + updateTimeBro);
            });

            GiftReset();

            // Serial받기  --> GetData_Serial쪽에서 진행       **serialList가 깨지기 때문

            // weeklyGift 초기화
            if (serverDateTime.DayOfWeek == DayOfWeek.Monday)
            {
                ResetWeeklyPackage();
            }
        }
    }

    private void Insert_Time()
    {
        Param param = new Param();
        param.Add(LastLoginColumnName, serverDate);

        Backend.GameData.Insert(TimeTableName, param, bro =>
        {
            if (!bro.IsSuccess())
                return;
        });
    }

    #endregion 시간

    private void GetGMData()
    {
        Backend.Social.GetUserInfoByNickName(gm_nickname, bro =>
        {
            if (!bro.IsSuccess())
            {
                Debug.LogWarning("Not GM!!");
                return;
            }

            gm_ownerInDate = bro.GetReturnValuetoJSON()["row"]["inDate"].ToString();

            Where where = new Where();
            where.Equal("owner_inDate", gm_ownerInDate);

            var gmBro = Backend.GameData.Get(SerialChartTableName, where);
            if (!gmBro.IsSuccess())
                return;

            gm_inDate = gmBro.FlattenRows()[0]["inDate"].ToString();
            //Debug.Log($"{gm_inDate} / {gm_ownerInDate}");
        });
    }

    #region 프로필

    /// <summary>
    /// CanSerial로 인해 동기
    /// </summary>
    public void GetData_Profile()
    {
        loadCount++;

        // 선택한 프로필 아이콘 번호
        var bro = Backend.GameData.GetMyData(ProfileTableName, new Where());

        if (!bro.IsSuccess())
            return;

        if (bro.GetReturnValuetoJSON()["rows"].Count <= 0)
        {
            Insert_Profile();
            return;
        }

        var rows = bro.FlattenRows();

        profileIcon = int.Parse(rows[0][IconColumnName].ToString());

        userInDate_Profile = rows[0]["inDate"].ToString();
        canSerial = rows[0][UnlockSerialColumnName].ToString() == "True";
        buyWeekly = rows[0][BuyWeeklyColumnName].ToString() == "True";
        isAlarm = rows[0][PushAlarmColumnName].ToString() == "True";

        language = (ELanguage)Enum.Parse(typeof(ELanguage), rows[0][LanguageColumnName].ToString());

        --loadCount;
        Debug.Log($"Profile : {bro}");
    }

    private void Insert_Profile()
    {
        Param param = new Param();
        param.Add(IconColumnName, 0);
        switch (Application.systemLanguage)
        {
            case SystemLanguage.Korean:
                param.Add(LanguageColumnName, "Korean");
                language = ELanguage.Korean;
                break;
            default:
                param.Add(LanguageColumnName, "English");
                language = ELanguage.English;
                break;
        }
        param.Add(UnlockSerialColumnName, false);
        param.Add(BuyWeeklyColumnName, false);
        param.Add(PushAlarmColumnName, isAlarm);                // *알람은 그 앞단계에서 이미 정해짐

        Backend.GameData.Insert(ProfileTableName, param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            userInDate_Profile = bro.GetInDate();
            // Debug.Log(userInDate_Profile);

            profileIcon = 0;
            canSerial = false;
            buyWeekly = false;

            --loadCount;
        });
    }

    public void Update_ProfileIcon(int value)
    {
        Param param = new Param();
        param.Add(IconColumnName, value);

        Backend.GameData.Update(ProfileTableName, new Where(), param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            profileIcon = value;
            Debug.Log($"프로필 갱신 : {profileIcon}");

            Enqueue(Backend.GameData.Update, ResultTableName, new Where(), param, resultBro => 
            {
                isUpdatedRank = false;
            });
        });
    }

    /// <summary>
    /// 두가지일 경우 변경만
    /// </summary>
    public void ChangeLanguage()
    {
        if (language == ELanguage.English)
            language = ELanguage.Korean;
        else
            language = ELanguage.English;


        Param param = new Param();
        param.Add(LanguageColumnName, language.ToString());

        Backend.GameData.Update(ProfileTableName, new Where(), param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            // **변경 완료 : 씬 재시작?
        });
    }

    public void ResetWeeklyPackage()
    {
        Param param = new Param();
        param.Add(BuyWeeklyColumnName, false);

        Backend.GameData.Update(ProfileTableName, new Where(), param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            buyWeekly = false;
        });
    }

    /// <summary>
    /// 주간 패키지 구매 시
    /// **하나밖에 없다고 가정했을 때 적용 가능
    /// </summary>
    public void BuyWeeklyPackage(FShopItem[] shopItems)
    {
        // 이미 구매했다면 못함      **그전에 검사하기
        if (buyWeekly)
            return;

        Param param = new Param();
        param.Add(BuyWeeklyColumnName, true);

        var bro = Backend.GameData.Update(ProfileTableName, new Where(), param);

        if (!bro.IsSuccess())
            return;

        buyWeekly = true;

        foreach (var item in shopItems)
        {
            AddItem((EItem)item.id, item.value);
        }

        ShopItemyLog(EShopItem.Weekly);
    }

#endregion 프로필

    #region 티켓

    public void GetData_Ticket()
    {
        loadCount++;

        Backend.GameData.GetMyData(TicketTableName, new Where(), bro =>
        {
            if (!bro.IsSuccess())
                return;

            if (bro.GetReturnValuetoJSON()["rows"].Count <= 0)
            {
                Insert_Ticket();
                return;
            }

            var rows = bro.FlattenRows();

            _GameManager.Ticket = int.Parse(rows[0][TicketColumnName].ToString());
            //_GameManager.TicketTime = int.Parse(rows[0][TicketTimeColumnName].ToString());

            // Debug.Log($"{_GameManager.Ticket}");

            _GameManager.CheckTicketExitTime();

            --loadCount;
            Debug.Log($"Ticket : {bro}");
        });
    }

    // 최초 1회만 실행
    private void Insert_Ticket()
    {
        Param param = new Param();

        param.Add(TicketColumnName, Values.MaxTicket);
        param.Add(TicketTimeColumnName, Values.TicketTime);

        Backend.GameData.Insert(TicketTableName, param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            _GameManager.TicketTime = Values.TicketTime;
            _GameManager.Ticket = Values.MaxTicket;

            _GameManager.CheckTicketExitTime();

            --loadCount;
        });
    }

    public void Update_Ticket(int value)
    {
        Param param = new Param();
        param.Add(TicketColumnName, value);

        Backend.GameData.Update(TicketTableName, new Where(), param, bro =>
        {
            if (!bro.IsSuccess())
                return;
        });
    }

    /// <summary>
    /// 게임 종료 전에 할 수 있나?
    /// *TicketTime은 사실상 불가능하다고 판단
    /// https://community.thebackend.io/t/topic/1288
    /// </summary>
    /// <param name="value"></param>
    public void Update_TicketTime(int value)
    {
        Param param = new Param();
        param.Add(TicketTimeColumnName, value);

        var bro = Backend.GameData.Update(TicketTableName, new Where(), param);
        if(bro.IsSuccess())
        {
            Debug.Log("게임을 종료합니다");
        }
    }

    #endregion 티켓

    #region 다이아

    public void GetData_Dia()
    {
        loadCount++;

        Backend.GameData.GetMyData(DiaTableName, new Where(), bro =>
        {
            if (!bro.IsSuccess())
                return;

            if (bro.GetReturnValuetoJSON()["rows"].Count <= 0)
            {
                Insert_Dia();
                return;
            }

            var rows = bro.FlattenRows();

            _GameManager.CashDia = int.Parse(rows[0][CashDiaColumnName].ToString());
            _GameManager.FreeDia = int.Parse(rows[0][FreeDiaColumnName].ToString());

            --loadCount;
            Debug.Log($"Dia : {bro}");
        });
    }

    private void Insert_Dia()
    {
        Param param = new Param();
        param.Add(CashDiaColumnName, 0);
        param.Add(FreeDiaColumnName, 0);

        Backend.GameData.Insert(DiaTableName, param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            // Debug.Log("Insert Dia");
            --loadCount;
        });
    }

    public void Update_CashDia(int value)
    {
        Param param = new Param();
        param.Add(CashDiaColumnName, value);

        var bro = Backend.GameData.Update(DiaTableName, new Where(), param);
        if (!bro.IsSuccess())
            return;

        // 결제 완료 처리

        // InsertLog("유료 다이아 갱신", value.ToString());
    }

    public void Update_FreeDia(int value)
    {
        Param param = new Param();
        param.Add(FreeDiaColumnName, value);

        Backend.GameData.Update(DiaTableName, new Where(), param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            // InsertLog("무료 다이아 갱신", value.ToString());
        });
    }

    #endregion 다이아

    #region 피버

    public void GetData_Fever()
    {
        loadCount++;

        Backend.GameData.GetMyData(FeverTableName, new Where(), bro =>
        {
            if (!bro.IsSuccess())
                return;

            if (bro.GetReturnValuetoJSON()["rows"].Count <= 0)
            {
                Insert_Fever();
                return;
            }

            var rows = bro.FlattenRows();

            _GameManager.GameController.Fever = int.Parse(rows[0][FeverColumnName].ToString());

            --loadCount;
            Debug.Log($"Fever : {bro}");
        });
    }

    private void Insert_Fever()
    {
        Param param = new Param();
        param.Add(FeverColumnName, 0);

        Backend.GameData.Insert(FeverTableName, param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            --loadCount;
        });
    }

    public void Update_Fever(int value)
    {
        Param param = new Param();
        param.Add(FeverColumnName, value);

        Backend.GameData.Update(FeverTableName, new Where(), param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            // Debug.Log("피버 개수 업데이트.. : " + value);
        });
    }

    #endregion 피버

    #region 로비 아이템

    public void GetData_LobbyItem()
    {
        ++loadCount;

        Backend.GameData.GetMyData(LobbyItemTableName, new Where(), bro =>
        {
            if (!bro.IsSuccess())
                return;

            if (bro.GetReturnValuetoJSON()["rows"].Count <= 0)
            {
                Insert_LobbyItem();
                return;
            }

            var rows = bro.FlattenRows();

            for (int i = 0, length = _GameManager.ItemsCount.Length; i < length; i++)
            {
                _GameManager.ItemsCount[i] = int.Parse(rows[0][((ELobbyItem)i).ToString()].ToString());
            }

            --loadCount;
            Debug.Log($"LobbyItem : {bro}");
        });
    }

    public void Insert_LobbyItem()
    {
        var items = _GameManager.ItemsCount;
        Param param = new Param();

        for (int i = 0, length = items.Length; i < length; i++)
        {
            param.Add(((ELobbyItem)i).ToString(), items[i]);
        }

        Backend.GameData.Insert(LobbyItemTableName, param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            --loadCount;
        });
    }

    public void Update_LobbyItem(int num, int value)
    {
        Param param = new Param();
        param.Add(((ELobbyItem)num).ToString(), value);

        Backend.GameData.Update(LobbyItemTableName, new Where(), param, bro =>
        {
            if (!bro.IsSuccess())
            {
                return;
            }

            Debug.Log($"로비 아이템 갱신 : {(ELobbyItem)num}");
        });
    }

    #endregion 로비아이템

    #region 일일 제공

    /// <summary>
    /// GameManger AddListener보다 빨리 해야하기에 동기처리
    /// </summary>
    public void GetData_DailyGift()
    {
        ++loadCount;

        var bro = Backend.GameData.GetMyData(DailyGiftTableName, new Where());

        if (!bro.IsSuccess())
            return;

        if (bro.GetReturnValuetoJSON()["rows"].Count <= 0)
        {
            Insert_DailyGift();
            return;
        }

        var rows = bro.FlattenRows();

        // *3/24 TimeReward 추가. CurDelay 추가
        if (rows[0].Keys.Contains(EDailyGift.TimeReward.ToString()) == false)
        {
            Debug.Log("Null Data");
            Insert_DailyGift();
            return;
        }

        foreach (EDailyGift item in Enum.GetValues(typeof(EDailyGift)))
        {
            _GameManager.DailyGifts[item] = new DailyGift
                (
                    int.Parse(rows[0][item.ToString()][FreeCountColumnName].ToString()),
                    int.Parse(rows[0][item.ToString()][AdCountColumnName].ToString())
                // int.Parse(rows[0][item.ToString()][AdDelayColumnName].ToString())
                );

            _GameManager.CheckDailyGiftAdExitTime(item);
        }

        --loadCount;
        // Debug.Log($"DailyGift : {bro}");
    }

    /// <summary>
    /// *딕셔너리 형태보다 각자 들어가 있기때문에 테이블 상태에서 용이할 것으로 예상
    /// </summary>
    private void Insert_DailyGift()
    {
        Param param = new Param();

        foreach (var dailyGiftData in LevelData.Instance.DailyGiftDatas)
        {
            var dailyGift = new DailyGift(dailyGiftData.freeCount, dailyGiftData.adCount);
            param.Add(dailyGiftData.type.ToString(), dailyGift);

            _GameManager.DailyGifts[dailyGiftData.type] = dailyGift;
        }

        var bro = Backend.GameData.Insert(DailyGiftTableName, param);
        if (!bro.IsSuccess())
        {
            Debug.Log("Insert False");
            return;
        }

        --loadCount;
    }

    public void Update_DailyGift(EDailyGift type, DailyGift value)
    {
        Param param = new Param();
        // name활용
        param.Add(type.ToString(), value);

        var bro = Backend.GameData.Update(DailyGiftTableName, new Where(), param);
        if (!bro.IsSuccess())
            return;
    }

    /// <summary>
    /// 일일 초기화 작업
    /// </summary>
    private void GiftReset()
    {
        Param param = new Param();
        foreach (var dailyGiftData in LevelData.Instance.DailyGiftDatas)
        {
            param.Add(dailyGiftData.type.ToString(), new DailyGift
            (
                dailyGiftData.freeCount, dailyGiftData.adCount
            ));
        }

        var giftBro = Backend.GameData.Update(DailyGiftTableName, new Where(), param);
        if (!giftBro.IsSuccess())
        {
            Debug.Log("Update Fail");
            return;
        }
        else
        {
            Debug.Log("Gift Reset");
        }
    }

    #endregion 일일 제공

    #region 게임 결과

    public void GetData_Result()
    {
        ++loadCount;

        Backend.GameData.GetMyData(ResultTableName, new Where(), bro =>
        {
            if (!bro.IsSuccess())
                return;

            if (bro.GetReturnValuetoJSON()["rows"].Count <= 0)
            {
                Insert_Result();
                return;
            }

            var rows = bro.FlattenRows();

            _GameManager.BestScore = int.Parse(rows[0][ScoreColumnName].ToString());
            _GameManager.BestMaxCombo = int.Parse(rows[0][ComboColumnName].ToString());
            _GameManager.AccumulateScore = int.Parse(rows[0][AccumulateScoreColumnName].ToString());

            --loadCount;
            Debug.Log($"Result : {bro}");
        });
    }

    private void Insert_Result()
    {
        Param param = new Param();
        param.Add(IconColumnName, 0);               // Ranking에서 쓰려고 같이 저장
        param.Add(ScoreColumnName, 0);
        param.Add(ComboColumnName, 0);
        param.Add(AccumulateScoreColumnName, 0);
        param.Add(PlayRewardColumnName, 0);

        Backend.GameData.Insert(ResultTableName, param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            userInDate_Result = bro.GetInDate();

            --loadCount;
        });
    }

    public void Update_Result(int score, int maxCombo)
    {
        if(string.IsNullOrEmpty(userInDate_Result))
        {
            Backend.GameData.Get(ResultTableName, new Where(), bro => 
            {
                if (!bro.IsSuccess())
                    return;

                var rows = bro.FlattenRows();
                userInDate_Result = rows[0]["inDate"].ToString();

                Update_ScoreResult(score, maxCombo);
            });
        }
        else
        {
            Update_ScoreResult(score, maxCombo);
        }
    }

    private void Update_ScoreResult(int score, int combo)
    {
        Param param = new Param();

        // Debug.Log(score + " >= " + _GameManager.BestScore);

        if (score >= _GameManager.BestScore)
        {
            param.Add(ScoreColumnName, score);
        }
        if (combo >= _GameManager.BestMaxCombo)
        {
            param.Add(ComboColumnName, combo);
        }

        var bro = Backend.GameData.Update(ResultTableName, new Where(), param);
        if (!bro.IsSuccess())
            return;

        // 누적점수 더하기
        Param updateParam = new Param();
        updateParam.AddCalculation(AccumulateScoreColumnName, GameInfoOperator.addition, score);

        Enqueue(Backend.GameData.UpdateWithCalculation, ResultTableName, new Where(), updateParam, accuBro =>
        {
            // 시리얼 갱신
            if (!accuBro.IsSuccess())
                return;

            _GameManager.AccumulateScore += score;

            if (!canSerial
                && loginType != ELogin.Guest                                                 // 게스트 제외
                && GameApplication.Instance.IsTestMode                                  // **p2e 사용시 제거
                && _GameManager.AccumulateScore >= Values.UnlockSerialScore)
            {
                // canSerial = true;
                UnlockSerial();
            }
        });

        //var accuBro = Backend.GameData.UpdateWithCalculation(ResultTableName, new Where(), updateParam);
        //if (!accuBro.IsSuccess())
        //{
        //    Debug.LogWarning(accuBro);
        //    return;
        //}

        //_GameManager.AccumulateScore += score;

        //if (!canSerial
        //    && loginType != ELogin.Guest                                                 // 게스트 제외
        //    && GameApplication.Instance.IsTestMode                                  // **p2e 사용시 제거
        //    && _GameManager.AccumulateScore >= Values.UnlockSerialScore)
        //{
        //    // canSerial = true;
        //    UnlockSerial();
        //}

        if (score >= Values.PlayRewardScore)
        {
            var addPlayReward = 1;
            
            Param rewardParam = new Param();
            rewardParam.AddCalculation(PlayRewardColumnName, GameInfoOperator.addition, addPlayReward);

            Enqueue(Backend.GameData.UpdateWithCalculation, RewardTableName, new Where(), rewardParam, rewardBro =>
            {
                // 시리얼 갱신
                if (!rewardBro.IsSuccess())
                    return;

                _GameManager.PlayRewardCount += addPlayReward;
            });
        }

        if (canSerial)
        {
            if (serialScoreList[serverDate] >= Values.DailySerialScore)
                return;

            // 오늘날짜 점수
            serialScoreList[serverDate] += score;

            Param serialParam = new Param();
            serialParam.Add(SerialScoreColumnName, serialScoreList);

            Enqueue(Backend.GameData.Update, SerialTableName, new Where(), serialParam, serialBro =>
            {
                if (!serialBro.IsSuccess())
                    return;

                // Debug.Log("미션");
            });
        }

        UpdateResultToRank(score, combo);

        isUpdatedRank = false;
    }

    #endregion 게임 결과

    #region 시리얼

    public void GetData_Serial()
    {
        ++loadCount;
        Backend.GameData.GetMyData(SerialTableName, new Where(), bro =>
        {
            if (!bro.IsSuccess())
                return;

            if (bro.GetReturnValuetoJSON()["rows"].Count <= 0)
            {
                Insert_Serial();
                return;
            }

            var rows = bro.FlattenRows();

            // 이전 데이터들 불러오기 7일 이후 : 삭제 필요
            int score = 0;
            string code = "";

            foreach (JsonData row in rows)
            {
                foreach (var serial in row[SerialScoreColumnName].Keys)
                {
                    FPostInfo postInfo = new FPostInfo();
                    code = "";

                    postInfo.title = "SerialMission";
                    postInfo.contents = "Mission!";

                    postInfo.postType = EPost.Serial;
                    postInfo.serverType = PostType.Admin;

                    // key - serverTime + 7
                    postInfo.remainTime = DateTime.Parse(serial).AddDays(Values.SerialRemainDay) - DateTime.Parse(serverDate);
                    score = int.Parse(row[SerialScoreColumnName][serial].ToString());
                    code = row[SerialCodeColumnName][serial].ToString();

                    if (postInfo.remainTime < TimeSpan.Zero)
                    {   // 7일 지나면 = 8일째에 로그 남기고 삭제 진행
                        RemoveSerialLog(serial, score);
                        return;
                    }

                    postInfo.serialDate = serial;

                    serialScoreList.Add(serial, score);
                    serialCodeList.Add(serial, code);

                    // 프리팹 생성
                    GameUIManager.Instance.AddSerialPost(postInfo);
                }
            }

            if(isDailyReset && canSerial)
            {
                RecvSerialPost();
            }

            --loadCount;
            Debug.Log($"Serial : {bro}");
        });
    }

    private void Insert_Serial()
    {
        var serialList = new Dictionary<string, int>();

        Param param = new Param();
        param.Add(SerialScoreColumnName, serialList);
        param.Add(SerialCodeColumnName, serialList);

        Backend.GameData.Insert(SerialTableName, param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            --loadCount;
        });
    }

    #endregion 시리얼

    #region 시리얼 차트 (GM전용)

    public void GetData_SerialChart()
    {
        if (nickName != gm_nickname)
            return;

        ++loadCount;
        Backend.GameData.GetMyData(SerialChartTableName, new Where(), bro =>
        {
            if (!bro.IsSuccess())
                return;

            if (bro.GetReturnValuetoJSON()["rows"].Count <= 0)
            {
                Insert_SerialChart();
                return;
            }

            var rows = bro.FlattenRows();

            --loadCount;
            Debug.Log($"SerialChart : {bro}");
        });
    }

    private void Insert_SerialChart()
    {
        if (nickName != gm_nickname)
            return;

        // var serialList = new Dictionary<string, int>();

        Param param = new Param();
        param.Add(SerialChartColumnName, 0);

        Backend.GameData.Insert(SerialChartTableName, param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            --loadCount;
        });
    }

    #endregion 시리얼 차트 (GM전용)

    #region 게임 유도 보상

    /// <summary>
    /// HomeUI 적용해야하기에 동기
    /// </summary>
    public void GetData_Reward()
    {
        loadCount++;

        var bro = Backend.GameData.GetMyData(RewardTableName, new Where());

        if (!bro.IsSuccess())
            return;

        if (bro.GetReturnValuetoJSON()["rows"].Count <= 0)
        {
            Insert_Reward();
            return;
        }

        var rows = bro.FlattenRows();

        _GameManager.PlayRewardCount = int.Parse(rows[0][PlayRewardColumnName].ToString());
        _GameManager.TimeRewardTime = DateTime.Parse(rows[0][EndTimeRewardColumnName].ToString());

        --loadCount;
        Debug.Log($"Reward : {bro}");
    }

    private void Insert_Reward()
    {
        var recvTime = DateTime.Now.AddHours(4);
        Param param = new Param();
        param.Add(PlayRewardColumnName, 0);
        param.Add(EndTimeRewardColumnName, recvTime);

        Backend.GameData.Insert(RewardTableName, param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            _GameManager.PlayRewardCount = 0;
            _GameManager.TimeRewardTime = recvTime;

            --loadCount;
        });
    }

    /// <summary>
    /// 5개 지불해서 사용
    /// </summary>
    private void Use_PlayReward()
    {
        Param updateParam = new Param();
        updateParam.AddCalculation(PlayRewardColumnName, GameInfoOperator.subtraction, Values.PlayRewardCount);

        Enqueue(Backend.GameData.UpdateWithCalculation, RewardTableName, new Where(), updateParam, accuBro =>
        {
            // 시리얼 갱신
            if (!accuBro.IsSuccess())
                return;

            _GameManager.PlayRewardCount -= Values.PlayRewardCount;
        });
    }

    public void Ad_TimeReward()
    {
        var time = _GameManager.TimeRewardTime.AddMinutes(-Values.TimeRewardAdTime);

        Param param = new Param();
        param.Add(EndTimeRewardColumnName, time);

        var bro = Backend.GameData.Update(RewardTableName, new Where(), param);
        if (!bro.IsSuccess())
            return;

        _GameManager.TimeRewardTime = time;
    }

    public void Reset_TimeReward()
    {
        var recvTime = DateTime.Now.AddHours(4);
        Param param = new Param();
        param.Add(EndTimeRewardColumnName, recvTime);

        Backend.GameData.Update(RewardTableName, new Where(), param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            _GameManager.TimeRewardTime = recvTime;

            // 보상?
        });
    }

    #endregion 게임 유도 보상

    #endregion 게임 데이터 세팅

    #region 랭킹 

    private void UpdateResultToRank(int score, int combo)
    {
        bool isUpdate = false;
        Param param = new Param();
        if (score >= _GameManager.BestScore)
        {
            param.Add(ScoreColumnName, score);
            isUpdate = true;
        }
        if (combo >= _GameManager.BestMaxCombo)
        {
            param.Add(ComboColumnName, combo);
            isUpdate = true;
        }

        if (!isUpdate)
            return;

        Backend.URank.User.UpdateUserScore(resultRankUUID, ResultTableName, userInDate_Result, param, bro =>
        {
            Debug.Log($"최고 결과 갱신 : {score}, {combo} - {bro}");
        });
    }

    //private void UpdateScoreToRank(int value)
    //{
    //    Param param = new Param();
    //    param.Add(ScoreColumnName, value);

    //    Backend.URank.User.UpdateUserScore(scoreRankUUID, ResultTableName, userInDate_Result, param, updateBro =>
    //    {
    //        Debug.Log($"최고 점수 갱신 : {value} - {updateBro}");
    //    });
    //}

    //private void UpdateComboToRank(int value)
    //{
    //    Param param = new Param();
    //    param.Add(ComboColumnName, value);

    //    Backend.URank.User.UpdateUserScore(comboRankUUID, ResultTableName, userInDate_Result, param, updateBro =>
    //    {
    //        Debug.Log($"최고 콤보 갱신 : {value} - {updateBro}");
    //    });
    //}

    /// <summary>
    /// 프로필에서 자신의 랭크 확인
    /// </summary>
    /// <returns></returns>
    public int GetMyScoreRank()
    {
        var bro = Backend.URank.User.GetMyRank(resultRankUUID);
        if (!bro.IsSuccess())
            return 0;

        var rows = bro.FlattenRows();

        return int.Parse(rows[0]["rank"].ToString());
    }

    public List<FRankInfo> GetScoreRankList()
    {
        var result = new List<FRankInfo>();

        //private int tmpScore;
        //if (tmpScore == GameManager.Instance.BestScore)
        //    tmpScore = GameManager.Instance.BestScore;

        if (isUpdatedRank)
            return null;


        var bro = Backend.URank.User.GetRankList(resultRankUUID, 100);
        if (!bro.IsSuccess())
            return result;

        JsonData rows = bro.FlattenRows();

        FRankInfo rankInfo = new FRankInfo();
        foreach (JsonData row in rows)
        {
            rankInfo.inDate = row["gamerInDate"].ToString();
            // Debug.Log($"{row["gamerInDate"]} - {row["owner_inDate"]}");
            try
            {   // **랭킹에 닉네임이 기록되지 않는 경우가 있음
                rankInfo.nickname = row["nickname"].ToString();
            }
            catch
            {
                rankInfo.nickname = "-";
            }

            rankInfo.score = int.Parse(row[ScoreColumnName].ToString());
            rankInfo.combo = int.Parse(row[ComboColumnName].ToString());
            rankInfo.rank = int.Parse(row["rank"].ToString());

            Where where = new Where();
            where.Equal("owner_inDate", rankInfo.inDate);
            string[] select = { IconColumnName};
            var resultBro = Backend.GameData.Get(ProfileTableName, where, select);
            //var profileBro = Backend.GameData.GetV2(ProfileTableName, userInDate_Profile, rankInfo.inDate);
            if (!resultBro.IsSuccess())
            {   // 탈퇴한 회원의 경우
                rankInfo.iconNum = 0;
                result.Add(rankInfo);
                Debug.Log("실패 : " + rankInfo.nickname);
                continue;
            }

            JsonData data = resultBro.FlattenRows();
            try
            {   // *없는 경우 0번 출력
                rankInfo.iconNum = int.Parse(data[0][IconColumnName].ToString());
            }
            catch
            {
                rankInfo.iconNum = 0;
            }

            result.Add(rankInfo);
        }

        isUpdatedRank = true;
        return result;
    }


#endregion

    #region 차트

    /// <summary>
    /// GetTime (안에있는 GiftReset) 
    /// 보다 빨리해야해서 동기처리
    /// </summary>
    public void GetChartLists()
    {
        var bro = Backend.Chart.GetChartList();

        if (!bro.IsSuccess())
            return;

        JsonData json = bro.FlattenRows();

        string name = "";
        string fileId = "";
        foreach (JsonData chart in json)
        {
            name = chart["chartName"].ToString();
            fileId = chart["selectedChartFileId"].ToString();

            // Debug.Log($"차트 탐색 : {name} / {fileId}");

            if (name == EChart.Item.ToString())
            {
                // GetChart_Item(outNum.ToString());                // Item은 다른 곳에서 적용인 것을 불러오기에 상관없음 (ex. 우편)
            }
            else if (name == EChart.LobbyItem.ToString())
            {
                GetChart_LobbyItem(fileId);
            }
            else if (name == EChart.InGameItem.ToString())
            {
                GetChart_InGameItem(fileId);
            }
            else if (name == EChart.DailyGift.ToString())
            {
                GetChart_DailyGift(fileId);
            }
            else if (name == EChart.Shop.ToString())
            {
                GetChart_ShopItem(fileId);
            }
            else if (name == EChart.Serial.ToString())
            {
                key_SerialChart = fileId;
                // * SerialChart는 퍼블릭으로 가져오기때문에 저장할 필요가 없다
                // : 그때그때 불러서 확인하는 편이 맞음 (트랜잭션이기에)
            }
        }
    }


    /// <summary>
    /// 차트를 로컬 데이터에 저장
    /// </summary>
    public void SaveChart_InGameItem(string key)
    {
        Backend.Chart.GetOneChartAndSave(key, bro => 
        {
            if (!bro.IsSuccess())
            {
                GetChart_InGameItemToServer(key);
                return;
            }

            Debug.Log("데이터 갱신 - InGameItem");

            JsonData rows = bro.FlattenRows();                      // *사용안하면  rows[i]["itemName"][0].ToString(); 가 되어야함
            for (int i = 0, length = rows.Count; i < length; i++)
            {
                // **아이템 개수가 고정이라고 가정
                LevelData.Instance.InGameItemDatas[i].value = int.Parse(rows[i]["value"].ToString());
                LevelData.Instance.InGameItemDatas[i].valueTime = float.Parse(rows[i]["valueTime"].ToString());
                LevelData.Instance.InGameItemDatas[i].normalPercent = float.Parse(rows[i]["normalPercent"].ToString());
                LevelData.Instance.InGameItemDatas[i].rarePercent = float.Parse(rows[i]["rarePercent"].ToString());
            }
        });
    }

    /// <summary>
    /// 차트 확인
    /// </summary>
    public void GetChart_InGameItem(string key)
    {
        var data = Backend.Chart.GetLocalChartData(key);
        if(data == "")
        {
            Debug.Log("인게임 아이템 차트 저장");
            SaveChart_InGameItem(key);
            return;
        }

        // 이미 저장된 데이터 -> SO
        var bro = JsonMapper.ToObject(data);
        var rows = bro["rows"];
        for (int i = 0, length = rows.Count; i < length; i++)
        {
            LevelData.Instance.InGameItemDatas[i].value = int.Parse(rows[i]["value"]["S"].ToString());
            LevelData.Instance.InGameItemDatas[i].valueTime = float.Parse(rows[i]["valueTime"]["S"].ToString());
            LevelData.Instance.InGameItemDatas[i].normalPercent = float.Parse(rows[i]["normalPercent"]["S"].ToString());
            LevelData.Instance.InGameItemDatas[i].rarePercent = float.Parse(rows[i]["rarePercent"]["S"].ToString());
        }
    }

    /// <summary>
    /// 서버에 있는 차트 그대로 사용
    /// </summary>
    public void GetChart_InGameItemToServer(string key)
    {
        Backend.Chart.GetChartContents(key, bro => 
        {
            if (!bro.IsSuccess())
                return;

            JsonData rows = bro.GetReturnValuetoJSON()["rows"];

            for (int i = 0, length = rows.Count; i < length; i++)
            {
                // **아이템 개수가 고정이라고 가정
                LevelData.Instance.InGameItemDatas[i].value = int.Parse(rows[i]["value"][0].ToString());
                LevelData.Instance.InGameItemDatas[i].valueTime = float.Parse(rows[i]["valueTime"][0].ToString());
                LevelData.Instance.InGameItemDatas[i].normalPercent = float.Parse(rows[i]["normalPercent"][0].ToString());
                LevelData.Instance.InGameItemDatas[i].rarePercent = float.Parse(rows[i]["rarePercent"][0].ToString());
                LevelData.Instance.InGameItemDatas[i].startPercent = float.Parse(rows[i]["startPercent"][0].ToString());
            }
        });
    }

    public void SaveChart_LobbyItem(string key)
    {
        Backend.Chart.GetOneChartAndSave(key, bro => 
        {
            if (!bro.IsSuccess())
            {
                GetChart_LobbyItemToServer(key);
                return;
            }

            Debug.Log("데이터 갱신 - InGameItem");

            JsonData rows = bro.FlattenRows();                      // *사용안하면  rows[i]["itemName"][0].ToString(); 가 되어야함
            // var lobbyItems = LevelData.Instance.LobbyItemDatas;
            for (int i = 0, length = rows.Count; i < length; i++)
            {
                LevelData.Instance.LobbyItemDatas[i].value = int.Parse(rows[i]["value"].ToString());
                LevelData.Instance.LobbyItemDatas[i].price = int.Parse(rows[i]["price"].ToString());
                LevelData.Instance.LobbyItemDatas[i].isFree = rows[i]["isFree"].ToString() == "Y";
                LevelData.Instance.LobbyItemDatas[i].salePercent = int.Parse(rows[i]["salePercent"].ToString());

                // Debug.Log(lobbyItems[i].isFree);
            }
        });
    }

    /// <summary>
    /// 차트 확인
    /// </summary>
    public void GetChart_LobbyItem(string key)
    {
        var data = Backend.Chart.GetLocalChartData(key);
        if (data == "")
        {
            Debug.Log("로비 아이템 차트 저장");
            SaveChart_LobbyItem(key);
            return;
        }

        // SO
        var bro = JsonMapper.ToObject(data);
        var rows = bro["rows"];
        for (int i = 0, length = rows.Count; i < length; i++)
        {
            LevelData.Instance.LobbyItemDatas[i].value = int.Parse(rows[i]["value"]["S"].ToString());
            LevelData.Instance.LobbyItemDatas[i].price = int.Parse(rows[i]["price"]["S"].ToString());
            LevelData.Instance.LobbyItemDatas[i].isFree = rows[i]["isFree"]["S"].ToString() == "Y";
            LevelData.Instance.LobbyItemDatas[i].salePercent = int.Parse(rows[i]["salePercent"]["S"].ToString());
        }
    }

    /// <summary>
    /// 서버에 있는 차트 그대로 사용
    /// </summary>
    public void GetChart_LobbyItemToServer(string key)
    {
        Backend.Chart.GetChartContents(key, bro => 
        {
            if (!bro.IsSuccess())
                return;

            JsonData rows = bro.FlattenRows();

            // var lobbyItems = LevelData.Instance.LobbyItemDatas;
            for (int i = 0, length = rows.Count; i < length; i++)
            {
                LevelData.Instance.LobbyItemDatas[i].value = int.Parse(rows[i]["value"].ToString());
                LevelData.Instance.LobbyItemDatas[i].price = int.Parse(rows[i]["price"].ToString());
                LevelData.Instance.LobbyItemDatas[i].isFree = rows[i]["isFree"].ToString() == "Y";
                LevelData.Instance.LobbyItemDatas[i].salePercent = int.Parse(rows[i]["salePercent"].ToString());
            }
        });
    }

    public void SaveChart_DailyGift(string key)
    {
        Backend.Chart.GetOneChartAndSave(key, bro =>
        {
            if (!bro.IsSuccess())
            {
                GetChart_DailyGiftToServer(key);
                return;
            }

            JsonData rows = bro.FlattenRows();
            for (int i = 0, length = rows.Count; i < length; i++)
            {
                LevelData.Instance.DailyGiftDatas[i].type =
                    (EDailyGift)(int.Parse(rows[i]["itemID"].ToString()) - Values.StartID_DailyGift);
                LevelData.Instance.DailyGiftDatas[i].freeCount = int.Parse(rows[i]["freeCount"].ToString());
                LevelData.Instance.DailyGiftDatas[i].adCount = int.Parse(rows[i]["adCount"].ToString());
                LevelData.Instance.DailyGiftDatas[i].chargeValue = int.Parse(rows[i]["chargeValue"].ToString());
                LevelData.Instance.DailyGiftDatas[i].adDelay = int.Parse(rows[i]["adDelay"].ToString());
            }
        });
    }

    public void GetChart_DailyGift(string key)
    {
        var data = Backend.Chart.GetLocalChartData(key);
        if (data == "")
        {
            Debug.Log("일일 제공 항목 차트 저장");
            SaveChart_DailyGift(key);
            return;
        }

        // SO
        var bro = JsonMapper.ToObject(data);
        var rows = bro["rows"];
        for (int i = 0, length = rows.Count; i < length; i++)
        {
            LevelData.Instance.DailyGiftDatas[i].type = 
                                (EDailyGift)(int.Parse(rows[i]["itemID"]["S"].ToString()) - Values.StartID_DailyGift);
            LevelData.Instance.DailyGiftDatas[i].freeCount = int.Parse(rows[i]["freeCount"]["S"].ToString());
            LevelData.Instance.DailyGiftDatas[i].adCount = int.Parse(rows[i]["adCount"]["S"].ToString());
            LevelData.Instance.DailyGiftDatas[i].chargeValue = int.Parse(rows[i]["chargeValue"]["S"].ToString());
            LevelData.Instance.DailyGiftDatas[i].adDelay = int.Parse(rows[i]["adDelay"]["S"].ToString());
        }
    }

    /// <summary>
    /// 서버에 있는 차트 그대로 사용
    /// </summary>
    public void GetChart_DailyGiftToServer(string key)
    {
        Debug.Log(key);

        Backend.Chart.GetChartContents(key, bro =>
        {
            if (!bro.IsSuccess())
                return;

            JsonData rows = bro.GetReturnValuetoJSON()["rows"];

            for (int i = 0, length = rows.Count; i < length; i++)
            {
                LevelData.Instance.DailyGiftDatas[i].type =
                                    (EDailyGift)(int.Parse(rows[i]["itemID"][0].ToString()) - Values.StartID_DailyGift);
                LevelData.Instance.DailyGiftDatas[i].freeCount = int.Parse(rows[i]["freeCount"][0].ToString());
                LevelData.Instance.DailyGiftDatas[i].adCount = int.Parse(rows[i]["adCount"][0].ToString());
                LevelData.Instance.DailyGiftDatas[i].chargeValue = int.Parse(rows[i]["chargeValue"][0].ToString());
                LevelData.Instance.DailyGiftDatas[i].adDelay = int.Parse(rows[i]["adDelay"][0].ToString());
            }
        });
    }

    /// <summary>
    /// 차트를 로컬 데이터에 저장
    /// </summary>
    public void SaveChart_ShopItem(string key)
    {
        //Backend.Chart.GetOneChartAndSave(key, EChart.Shop.ToString(), bro =>
        Backend.Chart.GetOneChartAndSave(key, bro =>
        {
            if (!bro.IsSuccess())
            {
                GetChart_ShopItemToServer(key);
                return;
            }

            JsonData rows = bro.FlattenRows();
            for (int i = 0, length = rows.Count; i < length; i++)
            {
                LevelData.Instance.ShopDatas[i].type = (EShopItem)Enum.Parse(typeof(EShopItem), rows[i]["type"].ToString());
                LevelData.Instance.ShopDatas[i].price = int.Parse(rows[i]["price"].ToString());
                LevelData.Instance.ShopDatas[i].salePercent = int.Parse(rows[i]["salePercent"].ToString());
                int itemLength = int.Parse(rows[i]["itemCount"].ToString());
                LevelData.Instance.ShopDatas[i].items = new FShopItem[itemLength];
                for (int j = 0; j < itemLength; j++)
                {   // item1ID, item1Value부터 시작
                    LevelData.Instance.ShopDatas[i].items[j].id = int.Parse(rows[i][$"item{j + 1}ID"].ToString());
                    LevelData.Instance.ShopDatas[i].items[j].value = int.Parse(rows[i][$"item{j + 1}Value"].ToString());
                }
            }
        });
    }

    /// <summary>
    /// 차트 확인
    /// </summary>
    public void GetChart_ShopItem(string key)
    {
        var data = Backend.Chart.GetLocalChartData(key);
        if (data == "")
        {
            Debug.Log("상점 차트 저장");
            SaveChart_ShopItem(key);
            return;
        }

        var bro = JsonMapper.ToObject(data);
        var rows = bro["rows"];
        for (int i = 0, length = rows.Count; i < length; i++)
        {
            LevelData.Instance.ShopDatas[i].type = (EShopItem)Enum.Parse(typeof(EShopItem), rows[i]["type"]["S"].ToString());
            LevelData.Instance.ShopDatas[i].price = int.Parse(rows[i]["price"]["S"].ToString());
            LevelData.Instance.ShopDatas[i].salePercent = int.Parse(rows[i]["salePercent"]["S"].ToString());
            int itemLength = int.Parse(rows[i]["itemCount"]["S"].ToString());
            LevelData.Instance.ShopDatas[i].items = new FShopItem[itemLength];
            for (int j = 0; j < itemLength; j++)
            {   // item1ID, item1Value부터 시작
                LevelData.Instance.ShopDatas[i].items[j].id = int.Parse(rows[i][$"item{j + 1}ID"]["S"].ToString());
                LevelData.Instance.ShopDatas[i].items[j].value = int.Parse(rows[i][$"item{j + 1}Value"]["S"].ToString());
            }
        }
    }

    /// <summary>
    /// 서버에 있는 차트 그대로 사용
    /// </summary>
    public void GetChart_ShopItemToServer(string key)
    {
        Backend.Chart.GetChartContents(key, bro =>
        {
            if (!bro.IsSuccess())
                return;

            JsonData rows = bro.GetReturnValuetoJSON()["rows"];
            for (int i = 0, length = rows.Count; i < length; i++)
            {
                LevelData.Instance.ShopDatas[i].type = (EShopItem)Enum.Parse(typeof(EShopItem), rows[i]["type"].ToString());
                LevelData.Instance.ShopDatas[i].price = int.Parse(rows[i]["price"].ToString());
                LevelData.Instance.ShopDatas[i].salePercent = int.Parse(rows[i]["salePercent"].ToString());
                int itemLength = int.Parse(rows[i]["itemCount"].ToString());
                LevelData.Instance.ShopDatas[i].items = new FShopItem[itemLength];
                for (int j = 0; j < itemLength; j++)
                {   // item1ID, item1Value부터 시작
                    LevelData.Instance.ShopDatas[i].items[j].id = int.Parse(rows[i][$"item{j + 1}ID"].ToString());
                    LevelData.Instance.ShopDatas[i].items[j].value = int.Parse(rows[i][$"item{j + 1}Value"].ToString());
                }
                // LevelData.Instance.ShopDatas[i].nameNum = int.Parse(rows[i]["languageNum"].ToString());
            }
        });
    }

#endregion 차트

    #region 확률

    /// <param name="type">가챠 종류</param>
    public void Gacha(EGacha type)
    {
        string key = "";
        switch (type)
        {
            case EGacha.ShopItem:
                key = key_ItemGacha_Probability;
                break;
            case EGacha.PlayReward:
                key = key_PlayReward_Probability;
                Use_PlayReward();
                break;
            case EGacha.TimeReward:
                key = key_TimeReward_Probability;
                Reset_TimeReward();
                break;
        }

        // 로그용 코드 추가
        var gachaItem = Probability_Gacha(key);

        Debug.Log($"가챠 아이템 획득 : {type} : {gachaItem.item} - {gachaItem.count}");
        Param param = new Param();
        // param.Add("가챠 종류", type.ToString());
        param.Add("획득 아이템", gachaItem.item);
        InsertLog($"{type} : 가챠 아이템 획득", param);

        GamePopup.Instance.OpenPopup(EGamePopup.Gacha);
    }

    /// <summary>
    /// *한번에 2개 이상 불가..?
    /// </summary>
    public FItemInfo Probability_Gacha(string key)
    {
        FItemInfo result;

        var bro = Backend.Probability.GetProbability(key);
        if (!bro.IsSuccess())
        {
            Debug.LogError(bro.ToString());
            result.icon = null;
            result.item = 0;
            result.count = 0;
            return result;
        }

        JsonData json = bro.GetFlattenJSON();

        var item = (EItem)int.Parse(json["elements"]["itemID"].ToString());
        int value = int.Parse(json["elements"]["value"].ToString());

        //InsertLog("가챠 아이템 획득", string.Format("{0} - {1}", item, value));

        AddItem(item, value);

        _GameManager.UseItemGacha();

        result.icon = GetItemSprite(item);
        result.item = item;
        result.count = value;

        return result;
    }


    #endregion 확률

    #region 메일

    public List<FPostInfo> GetPostList()
    {
        var result = new List<FPostInfo>();

        var bro = Backend.UPost.GetPostList(PostType.Admin, 10);
        if (!bro.IsSuccess())
            return result;

        JsonData json = bro.GetReturnValuetoJSON()["postList"];

        FPostInfo postInfo = new FPostInfo();
        foreach (JsonData rows in json)
        {
            postInfo.title = rows["title"].ToString();
            postInfo.contents = rows["content"].ToString();

            postInfo.postType = rows["items"].Count > 1 ? EPost.Normal : EPost.Package;
            postInfo.serverType = PostType.Admin;

            // 유효기간 날짜
            //TimeSpan remainTime = DateTime.Parse(rows["expirationDate"].ToString()) - DateTime.Parse(rows["reservationDate"].ToString());

            TimeSpan remainTime = DateTime.Parse(rows["expirationDate"].ToString()) - DateTime.Now;
            postInfo.remainTime = remainTime;

            postInfo.inDate = rows["inDate"].ToString();

            postInfo.itemInfos = new FItemInfo[rows["items"].Count];
            if (rows["items"].Count > 0)
            {
                int idx = 0;
                foreach (JsonData row in rows["items"])
                {
                    //Debug.Log($"{row["chartName"]} - {row["item"]["name"]} , {row["item"]["value"]}, {row["itemCount"]}");
                    postInfo.itemInfos[idx].item = (EItem)int.Parse(row["item"]["itemID"].ToString());
                    postInfo.itemInfos[idx].icon = GetItemSprite(postInfo.itemInfos[idx].item);
                    postInfo.itemInfos[idx].count = int.Parse(row["itemCount"].ToString());

                    //postInfo.itemInfos[idx].count = int.Parse(row["item"]["value"].ToString()) * int.Parse(row["itemCount"].ToString());
                    idx++;
                }
            }
            postInfo.sprite = (postInfo.itemInfos.Length > 1) ?
                                        LevelData.Instance.PackageSprite :
                                        postInfo.itemInfos[0].icon;
                                        //GetItemSprite((EItem)int.Parse(rows["items"][0]["item"]["itemID"].ToString()));

            result.Add(postInfo);
        }

        return result;
    }

    public void ReceivePostItem(PostType postType, string postIndate)
    {
        var bro = Backend.UPost.ReceivePostItem(postType, postIndate);
        if(!bro.IsSuccess())
        {
            Debug.LogError("메일을 받을 수 없습니다.");
            return;
        }
        JsonData postList = bro.GetReturnValuetoJSON()["postItems"];

        EItem item;
        int count;
        string log = "";
        foreach (JsonData post in postList)
        {
            if (post.Count <= 0)
            {
                Debug.Log("아이템이 없는 우편");
                continue;
            }

            item = (EItem)Enum.Parse(typeof(EItem), post["item"]["name"].ToString());
            //value = int.Parse(post["item"]["value"].ToString());
            count = int.Parse(post["itemCount"].ToString());

            Debug.Log($"메일 아이템 획득 : {item} - {count}");

            log += $"{item} - {count}";

            AddItem(item, count);
        }

        // InsertLog("메일 아이템 획득", $"{postIndate} : {log}");
    }

    #endregion 메일

    #region 시리얼

    /// <summary>
    /// 언락 + 
    /// 하나 전송해주기 : 메일처럼 보이는 오브젝트
    /// </summary>
    private void UnlockSerial()
    {
        Param param = new Param();
        param.Add(UnlockSerialColumnName, true);

        Backend.GameData.Update(ProfileTableName, new Where(), param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            canSerial = true;

            RecvSerialPost();
        });
    }

    /// <summary>
    /// 매일 받기
    /// </summary>
    private void RecvSerialPost()
    {
        // 현재 시간 : 서버타임
        serialScoreList.Add(serverDate, 0);
        serialCodeList.Add(serverDate, "");

        Param param = new Param();
        param.Add(SerialScoreColumnName, serialScoreList);
        param.Add(SerialCodeColumnName, serialCodeList);

        Backend.GameData.Update(SerialTableName, new Where(), param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            FPostInfo postInfo = new FPostInfo();

            postInfo.title = "SerialMission";
            postInfo.contents = "Mission!";

            postInfo.postType = EPost.Serial;
            postInfo.serverType = PostType.Admin;

            postInfo.remainTime = new TimeSpan(Values.SerialRemainDay, 0, 0, 0);         // 7일 기한

            // postInfo.inDate = rows["inDate"].ToString();

            postInfo.serialDate = serverDate;

            // ScoreReset();

            // 프리팹 생성
            DispatcherAction(() => GameUIManager.Instance.AddSerialPost(postInfo));
        });
    }

    /// <summary>
    /// 진짜 시리얼 번호를 주는 곳
    /// GM 데이터 num 찾아 트랜잭션으로 증가시키고
    /// 차트 데이터 불러와서 기존 번호 시리얼 return 하기
    /// </summary>
    public string RecvSerialCode(string date)
    {
        // 키 값에 데이터 있으면 반환
        if (serialCodeList.ContainsKey(date)
            && serialCodeList[date] != "")
        {
            return serialCodeList[date];
        }
        //var gmBro = Backend.GameInfo.GetPublicContentsByGamerIndate(SerialChartTableName, gm_inDate);
        List<TransactionValue> transactionList = new List<TransactionValue>();
        //transactionList.Add(TransactionValue.SetGetV2(SerialChartTableName, UserIndate, UserIndate));

        Where where = new Where();
        //where.Equal("nickname", gm_nickname);
        where.Equal("owner_inDate", gm_ownerInDate);
        transactionList.Add(TransactionValue.SetGet(SerialChartTableName, where));

        var bro = Backend.GameData.TransactionReadV2(transactionList);
        if (!bro.IsSuccess())
        {
            Debug.Log("Tranaction : " + bro);
            return "";
        }

        JsonData responses = bro.GetReturnValuetoJSON()["Responses"];
        int serialNum = int.Parse(responses[0][SerialChartColumnName]["N"].ToString());

        transactionList.Clear();
        Param param = new Param();
        param.Add(SerialChartColumnName, serialNum + 1);

        transactionList.Add(TransactionValue.SetUpdateV2(SerialChartTableName, gm_inDate, gm_ownerInDate, param));
        //updateParam.AddCalculation(SerialChartColumnName, GameInfoOperator.addition, 1);
        //transactionList.Add(TransactionValue.SetUpdate(SerialChartTableName, where, param));

        var transactionBro = Backend.GameData.TransactionWriteV2(transactionList);
        if (!transactionBro.IsSuccess())
        {
            Debug.Log("Tranaction2 : " + transactionBro);
            return "";
        }

        var chartBro = Backend.Chart.GetChartContents(key_SerialChart);
        if (!chartBro.IsSuccess())
            return "";

        JsonData chartRows = chartBro.GetReturnValuetoJSON()["rows"];
        var serialCode = chartRows[serialNum][SerialCodeColumnName]["S"].ToString();

        serialCodeList[date] = serialCode;

        Param serialParam = new Param();
        serialParam.Add(SerialCodeColumnName, serialCodeList);

        Backend.GameData.Update(SerialTableName, new Where(), serialParam, bro =>
        {
            if (!bro.IsSuccess())
                return;

            // 시리얼 등록 InsertLog
            RecvSerialLog(serialCodeList[date]);
        });

        return serialCode;
    }

    #endregion

    #region 로그 기록

    /* 로그 리스트
     *  닉네임 변경
     *  다이아 갱신 -> X
     *  최고 점수 저장 -> X
     *  최고 콤보 저장 -> X
     *  가챠 아이템
     *  메일 아이템 -> X
     /// 추가 필요
     *  상점 아이템 구입
     *  로비 아이템 구입
     *  게임 플레이
     *  우편
     */

    public void ShopItemyLog(EShopItem shopItem)
    {
        Param param = new Param();
        param.Add("구매아이템", shopItem.ToString());
        param.Add("현재 유료 다이아", GameManager.Instance.CashDia);
        param.Add("현재 무료 다이아", GameManager.Instance.FreeDia);

        InsertLog("유료 아이템 구매", param);
    }

    public void LobbyItemFreeLog(ELobbyItem item)
    {
        Param param = new Param();
        param.Add("구매아이템", item.ToString());

        InsertLog("무료 로비 아이템 구매", param);
    }
    public void LobbyItemDiaLog(ELobbyItem item, int useCash, int useFree, int currentCash, int currentFree)
    {
        Param param = new Param();
        param.Add("구매아이템", item.ToString());
        param.Add("사용 유료 다이아", useCash);
        param.Add("사용 무료 다이아", useFree);
        param.Add("현재 유료 다이아", currentCash);
        param.Add("현재 무료 다이아", currentFree);

        InsertLog("다이아 로비 아이템 구매", param);
    }

    public void TicketDiaLog(int useCash, int useFree, int currentCash, int currentFree)
    {
        Param param = new Param();
        param.Add("사용 유료 다이아", useCash);
        param.Add("사용 무료 다이아", useFree);
        param.Add("현재 유료 다이아", currentCash);
        param.Add("현재 무료 다이아", currentFree);

        InsertLog("티켓 다이아 구매", param);
    }

    public void GamePlayLog(int score, int dia, bool isRevive, bool isDoubleReward)
    {
        Param param = new Param();
        param.Add("플레이시간", DateTime.Now - GameStartTime);
        param.Add("사용 피버", _GameManager.GameController.UseFeverCount);
        param.Add("보유 피버",_GameManager.GameController.Fever);
        param.Add("획득 점수",score);
        param.Add("획득 재화",dia);
        param.Add("Revive", isRevive);
        param.Add("Double Reward", isDoubleReward);

        InsertLog("게임 플레이", param);
    }

    public void InGameItemLog(int itemAddCount, int nomralCount, Dictionary<EInGameItem, int> normalItems, 
                                        int rareCount, Dictionary<EInGameItem, int> rareItems)
    {
        Param param = new Param();
        param.Add("아이템 얻으려 한 횟수", itemAddCount);
        param.Add("획득 일반 상자 수", nomralCount);
        param.Add("일반 상자 아이템", normalItems);
        param.Add("획득 무지개 상자 수", rareCount);
        param.Add("무지개 상자 아이템", rareItems);

        InsertLog("획득 아이템", param);
    }

    private void RecvSerialLog(string code)
    {
        Param param = new Param();
        param.Add("시간", DateTime.Now);
        param.Add("시리얼 코드", code);

        InsertLog("시리얼 우편 수령", param);
    }

    private void RemoveSerialLog(string day, int score)
    {
        Param param = new Param();
        param.Add("날짜", day);
        param.Add("점수", score);

        InsertLog("시리얼 우편 삭제", param);
    }

    private void InsertLog(string logType, Param param)
    {
        Enqueue(Backend.GameLog.InsertLog, logType, param, bro =>
        {
            Debug.Log($"InsertLog : {logType} - {param} - {bro}");              // *param 출력가능?
        });
    }

    #endregion 로그 기록


    #region 영수증 검증

    public void PurchaseReceipt(UnityEngine.Purchasing.Product product)
    {
        var validation = Backend.Receipt.IsValidateGooglePurchase(product.receipt, nickName, false);
        if(!validation.IsSuccess())
        {
            Debug.Log(string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", product.definition.id));
            return;
        }
    }

    #endregion

    /// <summary>
    /// count -> value랑 통합
    /// </summary>
    /// <param name="item"></param>
    /// <param name="value"></param>
    public void AddItem(EItem item, int value)
    {
        switch (item)
        {
            case EItem.CashDia:
                GameManager.Instance.CashDia += value;
                break;
            case EItem.FreeDia:
                GameManager.Instance.FreeDia += value;
                break;
            case EItem.Ticket:
                GameManager.Instance.Ticket += value;
                break;
            case EItem.AddMaxTime:
            case EItem.PermBombPower:
            case EItem.MaxItem:
            case EItem.PermShield:
            case EItem.SuperFeverStart:
            case EItem.BonusScore:
                //GameManager.ItemsCount[(ELobbyItem)((int)item - Values.StartNum_LobbyItem)]++;
                _GameManager.ItemsCount[(int)item - Values.StartID_LobbyItem] += value;
                break;
            case EItem.SuperFever:
                GameManager.Instance.GameController.AddFever(value);
                break;
            case EItem.BonusCharacter:
            case EItem.BombFullGauge:
            case EItem.TimeCharge:
            case EItem.PreventInterrupt:
            case EItem.TempShield:
                Debug.LogError("인게임 아이템");
                break;
            //case EItem.Revive:
            //case EItem.DoubleReward:
            //    Debug.LogError("아웃게임 요소");
            //    break;
        }

        recvItems.Add(new FItemInfo(item, GetItemSprite(item), value));

        Debug.Log("Recv Itmes");
    }

    public Sprite GetItemSprite(EItem item)
    {
        switch (item)
        {
            case EItem.CashDia:
            case EItem.FreeDia:
            case EItem.Ticket:
                return LevelData.Instance.GoodsSprites[(int)item - Values.StartID_Goods];
            case EItem.AddMaxTime:
            case EItem.PermBombPower:
            case EItem.MaxItem:
            case EItem.PermShield:
            case EItem.SuperFeverStart:
            case EItem.BonusScore:
                return LevelData.Instance.LobbyItemDatas[(int)item - Values.StartID_LobbyItem].sprite;
            case EItem.BonusCharacter:      // 배열 [4]
            case EItem.TempBombPower:
            case EItem.BombFullGauge:
            case EItem.TimeCharge:
            case EItem.PreventInterrupt:
            case EItem.TempShield:
            case EItem.SuperFever:
                return LevelData.Instance.InGameItemDatas[(int)item - Values.StartID_InGameItem].sprite;
        }

        return null;
    }

    #region 설정 항목들

    public bool IsValidCoupon(string code)
    {
        var bro = Backend.Coupon.UseCoupon(code);
        if (!bro.IsSuccess())
        {
            Debug.LogWarning("Fail Coupon : " + bro);
            return false;
        }

        EItem id;
        int value;
        JsonData json = bro.GetReturnValuetoJSON();
        foreach (JsonData rows in json["itemObject"])
        {
            id = (EItem)int.Parse(rows["item"]["itemID"].ToString());
            value = int.Parse(rows["itemCount"].ToString());

            AddItem(id, value);
        }
        return true;
    }

    /// <summary>
    /// 로그인에서 알람 설정
    /// *GetData를 하지 않았기때문에 따로 처리 필요
    /// </summary>
    /// <param name="value"></param>
    public void SetPushNotification(bool value)
    {
        isAlarm = value;

#if !UNITY_EDITOR
            var text = Backend.Android.GetDeviceToken();
            Debug.Log("푸시알림 : " + text);
            if(isAlarm)
            {
                Backend.Android.PutDeviceToken();
            }
            else
            {
                Backend.Android.DeleteDeviceToken();
            }
#endif
    }

    /// <summary>
    /// 옵션에서 푸시알람 설정
    /// </summary>
    /// <returns></returns>
    public bool SwitchDeviceToken()
    {
        Param param = new Param();
        param.Add(PushAlarmColumnName, !isAlarm);

        var bro = Backend.GameData.Update(ProfileTableName, new Where(), param);
        if (!bro.IsSuccess())
            return false;

        isAlarm = !isAlarm;

#if !UNITY_EDITOR
            var text = Backend.Android.GetDeviceToken();
            Debug.Log("푸시알림 : " + text);
            if(isAlarm)
            {
                Backend.Android.PutDeviceToken();
            }
            else
            {
                Backend.Android.DeleteDeviceToken();
            }
#endif

        return isAlarm;
    }

    public void ChangeFederation(ELogin _type)
    {
        if (isProgressLogin)
            return;

        isProgressLogin = true;

        FederationType type = _type == ELogin.Google ? FederationType.Google : FederationType.Facebook;
        if (_type == ELogin.Google)
        {
            Social.localUser.Authenticate((bool success) =>
            {
                if (success)
                {
                    var bro = Backend.BMember.ChangeCustomToFederation(GetTokens(_type), type);
                    if (bro.IsSuccess())
                    {
                        GamePopup.Instance.ClosePopup();
                        loginType = _type;
                        isProgressLogin = false;
                    }
                    else
                    { 
                        if(int.Parse(bro.GetStatusCode()) == _Code_DuplicateLogin)
                        {
                            SystemPopupUI.Instance.OpenOneButton(15, 54, 0);
                            isProgressLogin = false;
                        }
                        else
                        {
                            Debug.Log("전환실패 : " + bro);
                            GamePopup.Instance.ClosePopup();
                            isProgressLogin = false;
                        }
                    }
                }
                else
                {
                    Debug.LogError("로그인 실패");
                    SystemPopupUI.Instance.OpenNoneTouch(50);
                    isProgressLogin = false;
                }
            });
        }
        else if (_type == ELogin.Facebook)
        {
            var perms = new List<string>() { "public_profile", "email" };
            FB.LogInWithReadPermissions(perms, AuthChangeFacebook);
        }
    }

    private void AuthChangeFacebook(ILoginResult result)
    {
        if (FB.IsLoggedIn)
        {
            Social.localUser.Authenticate((bool success) =>
            {
                if (success)
                {
                    var bro = Backend.BMember.ChangeCustomToFederation(GetTokens(ELogin.Facebook), FederationType.Facebook);
                    if (bro.IsSuccess())
                    {
                        GamePopup.Instance.ClosePopup();
                        loginType = ELogin.Facebook;
                        isProgressLogin = false;
                    }
                    else
                    {
                        if (int.Parse(bro.GetStatusCode()) == _Code_DuplicateLogin)
                        {
                            SystemPopupUI.Instance.OpenOneButton(15, 54, 0);
                            isProgressLogin = false;
                        }
                        else
                        {
                            Debug.Log("전환실패 : " + bro);
                            GamePopup.Instance.ClosePopup();
                            isProgressLogin = false;
                        }
                    }
                }
                else
                {
                    Debug.LogError("로그인 실패");
                    SystemPopupUI.Instance.OpenNoneTouch(50);
                    isProgressLogin = false;
                }
            });
        }
    }

    public void LogOut()
    {
        var bro = Backend.BMember.Logout();
        if(bro.IsSuccess())
        {
            Debug.Log("로그아웃");
            GameSceneManager.Instance.Restart();
        }
    }

    public void SignOut()
    {
        var bro = Backend.BMember.SignOut();
        if (bro.IsSuccess())
        {
            Debug.Log("탈퇴완료");

            GameSceneManager.Instance.Restart();
        }
    }

    public void ClearRecvItems()
    {
        recvItems.Clear();
    }

    #endregion
}