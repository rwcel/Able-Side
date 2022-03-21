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
using LitJson;

public class DailyGift
{
    public int freeCount;
    public int adCount;
    public int chargeValue;
    public int adDelay;

    public DailyGift() { }

    public DailyGift(int freeCount, int adCount, int chargeValue, int adDelay)
    {
        this.freeCount = freeCount;
        this.adCount = adCount;
        this.chargeValue = chargeValue;
        this.adDelay = adDelay;
    }

    public bool CanUse() => freeCount > 0;
    public bool CanCharge() => adCount > 0;

    public bool UseGift()
    {
        if (!CanUse())
            return false;

        --freeCount;

        return true;
    }

    public bool Charge()
    {
        if (!CanCharge())
            return false;

        --adCount;

        freeCount += chargeValue;

        return true;
    }

    public bool ChargeAndUse()
    {
        if (Charge())
        {
            if(UseGift())
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
    public int count;           // *Value�� �ʿ��� ����

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
    private static readonly int _Code_NotFederationData = 204;
    private static readonly int _Code_FederationData = 200;

    #region ���� ������

    private string currentVersion = "";

    private string nickName = "";                       // �г���
    private string gamerID = "";                        // UUID
    private ELogin loginType;
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
    public string UserIndate => Backend.UserInDate;               // UserInDate�� SDK��. Gamer_id�� ��ȸ �Ұ�
    public int ProfileIcon => profileIcon;
    public ELanguage Language => language;
    public Dictionary<string, int> SerialScoreList => serialScoreList;
    public int SerialScore(string key) => serialScoreList[key];
    public bool BuyWeekly => buyWeekly;
    public string ServerDate => serverDate;

    public List<FItemInfo> RecvItems => recvItems;

    #endregion ���� ������

    #region ���̺� 

    private string LobbyItemTableName = "LobbyItems";           // column�� enum������ search

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
    // **�������� �ʱ�ȭ �ϸ� �ȵ�. -> ������ �������� ����� ����

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

    //private static readonly string scoreRankUUID = "783e9680-8ee7-11ec-b0f8-11d63a08d6e5";          // ����
    //private static readonly string comboRankUUID = "f5fc83e0-943f-11ec-a8f8-13e7955bbb97";       // ����
    private static readonly string resultRankUUID = "12e8c840-a405-11ec-afae-c5cd66ba001b";                   // ���� ���� ��ŷ

    private static readonly string key_ItemGachaProbability = "4273";
    private static readonly string key_DailyGiftChart = "DailyGiftChart";

    private bool isAlarm;           // Ǫ�þ˸�
    public bool IsAlarm => isAlarm;

    private string userInDate_Result;                // ���� ������
    private string userInDate_Profile;

    private bool isUpdatedRank;

    #endregion

    #region GM Data

    private static readonly string gm_nickname = "player";          // *GM������ �� ������ �ٽ� �ؾ���
    private string gm_ownerInDate;
    private string gm_inDate;

    #endregion

    protected override void AwakeInstance()
    {
        var obj = FindObjectsOfType<BackEndServerManager>();
        if (obj.Length == 1)
            DontDestroyOnLoad(gameObject);
        else
        {
            Destroy(gameObject);
        }

        recvItems = new List<FItemInfo>(Values.Max_Reward_Items);
    }

    protected override void DestroyInstance() { }

    private void Start()
    {
        if(Application.internetReachability == NetworkReachability.NotReachable)
        {
            _LoginUI.ShowUpdateUI();
            return;
        }

#if UNITY_ANDROID
        // GPGS �÷����� ����
        var config = new PlayGamesClientConfiguration
            .Builder()
            .RequestServerAuthCode(false)
            .RequestEmail()                     // �̸��� ����
            .RequestIdToken()
            .Build();

        PlayGamesPlatform.InitializeInstance(config);
        PlayGamesPlatform.DebugLogEnabled = true;           // ����׷α� Ȯ��
        PlayGamesPlatform.Activate();
#endif
#if !UNITY_EDITOR
        // Facebook ����
        if(!FB.IsInitialized)
        {
            FB.Init(InitCallback, OnHideUnity);
        }
        else
        {
            FB.ActivateApp();
        }
#endif

        currentVersion = Application.version;

        var bro = Backend.Initialize(true);
        if(bro.IsSuccess())
        {
            if (!string.IsNullOrEmpty(Backend.Utils.GetGoogleHash()))
                Debug.Log(Backend.Utils.GetGoogleHash());
            // Debug.Log(Backend.Utils.GetGoogleHash());

            var servertime = Backend.Utils.GetServerTime();

            serverDateTime = DateTime.Parse(servertime.GetReturnValuetoJSON()["utcTime"].ToString());
            serverDate = serverDateTime.ToString("yyyy-MM-dd");
            //Debug.Log("����Ÿ�� : " + serverDateTime);

            // ShowPolicy();
            CheckApplicationVersion();
        }
        else
        {
            Debug.Log("�ʱ�ȭ ���� - " + bro);               // ���ͳ� ���� ���� �����̹Ƿ� ���� ����
            SystemPopupUI.Instance.OpenOneButton(6, 13, 22, GameApplication.Instance.Quit);
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

    // Dispatcer���� action ���� (���ν�����)
    private void DispatcherAction(Action action)
    {
        Dispatcher.Instance.Invoke(action);
    }

    private void CheckApplicationVersion()
    {
        Backend.Utils.GetLatestVersion(versionBRO =>
        {
            if (versionBRO.IsSuccess())
            {
                string latest = versionBRO.GetReturnValuetoJSON()["version"].ToString();
                Debug.Log("version info - current: " + currentVersion + " latest: " + latest);
                if (currentVersion != latest)
                {       
                    // ������ �ٸ� ���
                    int type = (int)versionBRO.GetReturnValuetoJSON()["type"];
                    // type = 1 : ����, type = 2 : ����
                    if(type == 2)
                    {
                        DispatcherAction(_LoginUI.ShowUpdateUI);
                    }
                }
            }
        });
    }

    /// <summary>
    /// ��ư Ŭ�� �� �α��� Ȯ��
    /// </summary>
    public void LoginWithTheBackendToken()
    {
        if (_LoginUI == null)
        {   // �� ��ε� �� �߻��ϴ� ����
            _LoginUI = FindObjectOfType<LoginUI>();
        }

        Enqueue(Backend.BMember.LoginWithTheBackendToken, loginBro =>
        {
            if (loginBro.IsSuccess())
            {
                OnBackendAuthorized();
            }
            else
            {
                Debug.Log("�α��� ���� - " + loginBro.ToString());
                DispatcherAction(_LoginUI.ShowLoginUI);
            }
        });
    }

    public void SuccessLogin()
    {
        _LoginUI.CloseAll();
        //Debug.Log("�α��� ���� : " + loginType.ToString());
        switch (loginType)
        {
            case ELogin.Google:
                var googleBro = Backend.BMember.CheckUserInBackend(GetTokens(), FederationType.Google);
                if(googleBro.IsSuccess())
                {
                    if (int.Parse(googleBro.GetStatusCode()) == _Code_NotFederationData)
                    {
                        _LoginUI.ShowPrivacyUI();
                    }
                    else if(int.Parse(googleBro.GetStatusCode()) == _Code_FederationData)
                    {
                        OnBackendAuthorized();
                    }
                }
                break;
            case ELogin.Facebook:
                if (!FB.IsLoggedIn)
                {
                    Debug.LogError("���̽��� �α��� �Ұ�");
                    return;
                }

                var fbBro = Backend.BMember.CheckUserInBackend(GetTokens(), FederationType.Facebook);
                if (fbBro.IsSuccess())
                {
                    if (int.Parse(fbBro.GetStatusCode()) == _Code_NotFederationData)
                    {
                        _LoginUI.ShowPrivacyUI();
                    }
                    else if (int.Parse(fbBro.GetStatusCode()) == _Code_FederationData)
                    {
                        OnBackendAuthorized();
                    }
                }
                break;
            case ELogin.Guest:
                _LoginUI.ShowPrivacyUI();
                break;
        }
    }

    public void OnBackendAuthorized()
    {
        _LoginUI.CloseAll();

        GetUserInfo();  // �������� �������� �� ������ ����
        GetData_Time();
        GetGMData();

        DispatcherAction(_LoginUI.AnimStart);
    }

    #region ȸ������

    private bool isProgressLogin;

    public void GoogleLogin()
    {
        if (isProgressLogin)
            return;

        isProgressLogin = true;
        loginType = ELogin.Google;
        if(Social.localUser.authenticated)
        {
            var bro = Backend.BMember.AuthorizeFederation(GetTokens(), FederationType.Google, "gpgs");
            OnBackendAuthorized();
            isProgressLogin = false;
        }
        else
        {
            Social.localUser.Authenticate((bool success) => 
            {
                if(success)
                {
                    var bro = Backend.BMember.AuthorizeFederation(GetTokens(), FederationType.Google, "gpgs");
                    if (bro.IsSuccess())
                    {   
                        var googleBro = Backend.BMember.CheckUserInBackend(GetTokens(), FederationType.Google);
                        if (googleBro.IsSuccess())
                        {
                            Debug.Log(int.Parse(googleBro.GetStatusCode()));
                            if (int.Parse(googleBro.GetStatusCode()) == _Code_NotFederationData)
                            {   // ó�� �α���
                                _LoginUI.ShowPrivacyUI();
                                isProgressLogin = false;
                            }
                            else if (int.Parse(googleBro.GetStatusCode()) == _Code_FederationData)
                            {
                                OnBackendAuthorized();
                                isProgressLogin = false;
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError("�α��� ����");
                }
            });
        }
    }

    private string GetTokens()
    {
        if(loginType == ELogin.Google)
        {
            if (PlayGamesPlatform.Instance.localUser.authenticated)
            {
                return PlayGamesPlatform.Instance.GetIdToken();
            }
            else
            {
                Debug.Log("���ӵǾ����� �ʽ��ϴ�!");
                return null;
            }
        }
        else if(loginType == ELogin.Facebook)
        {
            var aToken = Facebook.Unity.AccessToken.CurrentAccessToken;
            //string facebookToken = aToken.TokenString;

            return aToken.TokenString;
        }

        return null;
    }

    // ���̽��� ȸ������
    private void InitCallback()
    {
        if(FB.IsInitialized)
        {
            FB.ActivateApp();
        }
        else
        {
            Debug.LogWarning("Facebook SDK ���µ� ����");
        }
    }

    private void OnHideUnity(bool isGameShown)
    {
        Time.timeScale = !isGameShown ? 0 : 1;
    }

    public void FacebookLogin()
    {
        var perms = new List<string>() { "public_profile", "email" };
        FB.LogInWithReadPermissions(perms, AuthCallback);
    }

    private void AuthCallback(ILoginResult result)
    {
        if (isProgressLogin)
            return;

        if (FB.IsLoggedIn)
        {
            // �ڳ� ������ ���Կ�û
            isProgressLogin = true;
            loginType = ELogin.Facebook;
            if (Social.localUser.authenticated)
            {
                var bro = Backend.BMember.AuthorizeFederation(GetTokens(), FederationType.Facebook);
                OnBackendAuthorized();
                isProgressLogin = false;
            }
            else
            {
                Social.localUser.Authenticate((bool success) =>
                {
                    if (success)
                    {
                        var bro = Backend.BMember.AuthorizeFederation(GetTokens(), FederationType.Facebook);
                        if (bro.IsSuccess())
                        {   // ó�� �α���
                            var fbBro = Backend.BMember.CheckUserInBackend(GetTokens(), FederationType.Facebook);
                            if (fbBro.IsSuccess())
                            {
                                if (int.Parse(fbBro.GetStatusCode()) == _Code_NotFederationData)
                                {
                                    _LoginUI.ShowPrivacyUI();
                                    isProgressLogin = true;
                                }
                                else if (int.Parse(fbBro.GetStatusCode()) == _Code_FederationData)
                                {
                                    OnBackendAuthorized();
                                    isProgressLogin = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("�α��� ����");
                    }
                });
            }
        }
    }


    /// <summary>
    /// �Խ�Ʈ ȸ������
    /// *���� ó��
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
                // ȸ������
                _LoginUI.ShowPrivacyUI();
                isProgressLogin = false;
            }
            else if(int.Parse(bro.GetStatusCode()) == _Code_Login)
            {
                // �α���
                OnBackendAuthorized();
                Debug.Log("�Խ�Ʈ �α��� ����");
                isProgressLogin = false;
            }
        }
        else
        {
            Debug.LogError("�Խ�Ʈ �α��� ���� : " + bro);
            Param param = new Param();
            param.Add("���л���" + bro);
            InsertLog("�Խ�Ʈ�α��� ����", param);
            // ���� ����
            Backend.BMember.DeleteGuestInfo();

            SystemPopupUI.Instance.OpenOneButton(19, 28, 53, GameApplication.Instance.Quit);
        }
    }

#endregion ȸ������

#region �г���

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

                    // Debug.Log("Move Scene");
                    // GameSceneManager.Instance.SceneChange(GameSceneManager.EScene.InGame);
                }
                else
                {
                    Debug.Log("Not Nickname");
                    //DispatcherAction(() => _LoginUI.ShowNickNameUI(false));
                    DispatcherAction(_LoginUI.ShowNickNameUI);
                }
            }
            else
            {
                Debug.Log("[X]���� ���� �������� ���� - " + userInfoBro);
            }
        });
    }

    public string GetNickName()
    {
        return nickName;
    }

    public int DuplicateNickNameCheck(string _newNickName)
    {
        // *�г��� �ߺ� : ����
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
                    param.Add("���� �г���", before);
                    param.Add("���� �г���", _newNickName);
                    InsertLog("�г��� ���� �Ϸ�", param);
                }
            }
            else
            {
                Debug.Log("�г��� ���� ���� : " + nickNameBro);
            }
        });
    }

#endregion �г���


    public void ClearRecvItems()
    {
        recvItems.Clear();
    }

#region ���� ������ ����

    /* ������ ��� �� ["N"] ["S"] ���� ���� ����
     BOOL	bool	        boolean ������ �����Ͱ� �̿� �ش�˴ϴ�.
     N	        numbers	    int, float, double �� ��� ������ �����ʹ� �̿� �ش�˴ϴ�.
     S	        string	        string ������ �����Ͱ� �̿� �ش�˴ϴ�.
     L	        list            	list ������ �����Ͱ� �̿� �ش�˴ϴ�.
     M	        map	        map, dictionary ������ �����Ͱ� �̿� �ش�˴ϴ�.
     NULL   	null	        ���� �������� �ʴ� ��� �̿� �ش�˴ϴ�.
    https://developer.thebackend.io/unity3d/guide/gameDataV3/getMyData/where/
     */

    GameManager _GameManager;
    private int loadCount;

    public void GetGameDatas(GameManager gameManager)
    {
        _GameManager = gameManager;
        loadCount = 0;

        //GetData_Time();             // �̰Ϳ� ���� DailyGift Reset

        // �����͵� ����
        GetData_Profile();
        GetData_Ticket();            // ���� �÷��� ���
        GetData_Dia();               // ��ȭ
        GetData_Fever();            // �ΰ��� ������ ���
        GetData_LobbyItem();     // �κ� ������ ���
        GetData_DailyGift();
        GetData_Result();
        GetData_Serial();
        GetData_SerialChart();

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

#region ������

    /// <summary>
    /// CanSerial�� ���� ����
    /// </summary>
    public void GetData_Profile()
    {
        loadCount++;

        // ������ ������ ������ ��ȣ
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
        language = (ELanguage)Enum.Parse(typeof(ELanguage), rows[0][LanguageColumnName].ToString());

        userInDate_Profile = rows[0]["inDate"].ToString();
        canSerial = rows[0][UnlockSerialColumnName].ToString() == "True";
        buyWeekly = rows[0][BuyWeeklyColumnName].ToString() == "True";
        isAlarm = rows[0][PushAlarmColumnName].ToString() == "True";

        --loadCount;
        Debug.Log($"Profile : {bro}");
    }

    private void Insert_Profile()
    {
        Param param = new Param();
        param.Add(IconColumnName, 0);
        param.Add(LanguageColumnName, "English");
        param.Add(UnlockSerialColumnName, false);
        param.Add(BuyWeeklyColumnName, false);
        param.Add(PushAlarmColumnName, isAlarm);                // *�˶��� �� �մܰ迡�� �̹� ������

        Backend.GameData.Insert(ProfileTableName, param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            userInDate_Profile = bro.GetInDate();
            // Debug.Log(userInDate_Profile);

            profileIcon = 0;
            language = ELanguage.English;
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
            Debug.Log($"������ ���� : {profileIcon}");

            Enqueue(Backend.GameData.Update, ResultTableName, new Where(), param, resultBro => 
            {
                isUpdatedRank = false;
            });
        });
    }

    /// <summary>
    /// �ΰ����� ��� ���游
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

            // **���� �Ϸ� : �� �����?
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
    /// �ְ� ��Ű�� ���� ��
    /// **�ϳ��ۿ� ���ٰ� �������� �� ���� ����
    /// </summary>
    public void BuyWeeklyPackage(FShopItem[] shopItems)
    {
        // �̹� �����ߴٸ� ����      **������ �˻��ϱ�
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

#endregion ������

#region Ƽ��

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

            _GameManager.CheckDateOfExitTime();

            --loadCount;
            Debug.Log($"Ticket : {bro}");
        });
    }

    // ���� 1ȸ�� ����
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

            _GameManager.CheckDateOfExitTime();

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
    /// ���� ���� ���� �� �� �ֳ�?
    /// *TicketTime�� ��ǻ� �Ұ����ϴٰ� �Ǵ�
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
            Debug.Log("������ �����մϴ�");
        }
    }

#endregion Ƽ��

    // ���̾�
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

        // ���� �Ϸ� ó��

        // InsertLog("���� ���̾� ����", value.ToString());
    }

    public void Update_FreeDia(int value)
    {
        Param param = new Param();
        param.Add(FreeDiaColumnName, value);

        Backend.GameData.Update(DiaTableName, new Where(), param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            // InsertLog("���� ���̾� ����", value.ToString());
        });
    }

    // �ǹ�
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
        });
    }

    // �κ� ������
    public void GetData_LobbyItem()
    {
        ++loadCount;

        Backend.GameData.GetMyData(LobbyItemTableName, new Where(), bro =>
        {
            if (!bro.IsSuccess())
                return;

            // row�� �ϳ��� ���� ��� -> ��Ű�� ������?
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

            //foreach (JsonData row in rows)
            //{
            //    _GameManager.ItemsCount[(int)ELobbyItem.AddMaxTime] = int.Parse(row[ELobbyItem.AddMaxTime.ToString()].ToString());
            //    _GameManager.ItemsCount[(int)ELobbyItem.AddBombPower] = int.Parse(row[ELobbyItem.AddBombPower.ToString()].ToString());
            //    _GameManager.ItemsCount[(int)ELobbyItem.MaxItem] = int.Parse(row[ELobbyItem.MaxItem.ToString()].ToString());
            //    _GameManager.ItemsCount[(int)ELobbyItem.SuperFeverStart] = int.Parse(row[ELobbyItem.SuperFeverStart.ToString()].ToString());
            //    _GameManager.ItemsCount[(int)ELobbyItem.AddScore] = int.Parse(row[ELobbyItem.AddScore.ToString()].ToString());
            //    _GameManager.ItemsCount[(int)ELobbyItem.Shield] = int.Parse(row[ELobbyItem.Shield.ToString()].ToString());
            //}

            --loadCount;
            Debug.Log($"LobbyItem : {bro}");

            //var rows = BackendReturnObject.Flatten(bro.Rows());
            //foreach (var key in rows.Keys)
            //{
            //    Debug.Log($"{rows.Keys}");
            //    GameManager.ItemsCount.Add((ELobbyItem)Enum.Parse(typeof(ELobbyItem), key)
            //                                                , int.Parse(rows[key].ToString()));
            //}
        });
    }

    public void Insert_LobbyItem()
    {
        //Param param = new Param();

        //foreach (ELobbyItem item in Enum.GetValues(typeof(ELobbyItem)))
        //{
        //    GameManager.ItemsCount.Add(item, 0);
        //}
        //param.Add("LobbyItem", GameManager.ItemsCount);

        //Backend.GameData.Insert(LobbyItemTableName, param, bro =>
        //{
        //    if (!bro.IsSuccess())
        //        return;

        //    Debug.Log("�κ� ������ ����");
        //});

        var items = _GameManager.ItemsCount;
        Param param = new Param();

        for (int i = 0, length = items.Length; i < length; i++)
        {
            param.Add(((ELobbyItem)i).ToString(), items[i]);
        }
        //param.Add(ELobbyItem.AddMaxTime.ToString(), items[0]);

        Backend.GameData.Insert(LobbyItemTableName, param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            // userInDate_LobbyItem = bro.GetInDate();
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

            Debug.Log($"�κ� ������ ���� : {(ELobbyItem)num}");
        });
    }

    // ���� -> Struct or Class
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

        foreach (EDailyGift item in Enum.GetValues(typeof(EDailyGift)))
        {
            _GameManager.DailyGifts[item] = new DailyGift
                (
                    int.Parse(rows[0][item.ToString()][FreeCountColumnName].ToString()),
                    int.Parse(rows[0][item.ToString()][AdCountColumnName].ToString()),
                    int.Parse(rows[0][item.ToString()][ChargeValueColumnName].ToString()),
                    int.Parse(rows[0][item.ToString()][AdDelayColumnName].ToString())
                );
        }

        --loadCount;
        Debug.Log($"DailyGift : {bro}");
        // Debug.Log("���� : " +_GameManager.DailyGifts.Count);
    }

    /// <summary>
    /// *��ųʸ� ���º��� ���� �� �ֱ⶧���� ���̺� ���¿��� ������ ������ ����
    /// </summary>
    private void Insert_DailyGift()
    {
        if (!PlayerPrefs.HasKey(key_DailyGiftChart))     // �ʱ� 1ȸ ���� ��
            return;

        var data = Backend.Chart.GetLocalChartData(PlayerPrefs.GetString(key_DailyGiftChart));
        if (data == "")
        {
            Debug.Log("DailyGift ��Ʈ ����");
            return;
        }

        var bro = JsonMapper.ToObject(data);
        var rows = bro["rows"];

        Param param = new Param();
        int freeCount, adCount, chargeValue, adDelay;
        for (int i = 0, length = rows.Count; i < length; i++)
        {
            freeCount = int.Parse(rows[i][FreeCountColumnName]["S"].ToString());
            adCount = int.Parse(rows[i][AdCountColumnName]["S"].ToString());
            chargeValue = int.Parse(rows[i][ChargeValueColumnName]["S"].ToString());
            adDelay = int.Parse(rows[i][AdDelayColumnName]["S"].ToString());

            param.Add(((EDailyGift)i).ToString(), new DailyGift
                (
                freeCount, adCount, chargeValue, adDelay
                ));

            _GameManager.DailyGifts[(EDailyGift)i] = new DailyGift
                (
                    freeCount, adCount, chargeValue, adDelay
                );
        }

        // Debug.Log(_GameManager.DailyGifts);

        Backend.GameData.Insert(DailyGiftTableName, param, insertBro =>
        {
            if (!insertBro.IsSuccess())
            {
                Debug.Log("Insert False");
                return;
            }

            --loadCount;
        });
        --loadCount;
    }

    public void Update_DailyGift(EDailyGift name, DailyGift value)
    {
        Param param = new Param();
        // nameȰ��
        param.Add(name.ToString(), value);

        var bro = Backend.GameData.Update(DailyGiftTableName, new Where(), param);
        if (!bro.IsSuccess())
            return;
    }

    // ���� ���
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
        param.Add(IconColumnName, 0);               // Ranking���� ������ ���� ����
        param.Add(ScoreColumnName, 0);
        param.Add(ComboColumnName, 0);
        param.Add(AccumulateScoreColumnName, 0);

        Backend.GameData.Insert(ResultTableName, param, bro =>
        {
            if (!bro.IsSuccess())
                return;

            userInDate_Result = bro.GetInDate();
            // Debug.Log(userInDate_Result);

            --loadCount;
            //Enqueue(Backend.URank.User.UpdateUserScore, rankUuid, ResultTableName, userInDate_Score, param, updateScoreBro => 
            //{
            //});
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

        // �������� ���ϱ�
        Param updateParam = new Param();
        updateParam.AddCalculation(AccumulateScoreColumnName, GameInfoOperator.addition, score);

        Enqueue(Backend.GameData.UpdateWithCalculation, ResultTableName, new Where(), updateParam, accuBro =>
        {
            // �ø��� ����
            if (!accuBro.IsSuccess())
                return;

            _GameManager.AccumulateScore += score;
            if (!canSerial 
            && loginType != ELogin.Guest                                                 // �Խ�Ʈ ����
            && _GameManager.AccumulateScore >= Values.UnlockSerialScore)
            {
                // canSerial = true;
                UnlockSerial();
            }
        });
        
        if(canSerial)
        {
            if (serialScoreList[serverDate] >= Values.DailySerialScore)
                return;

            // ���ó�¥ ����
            serialScoreList[serverDate] += score;

            Param serialParam = new Param();
            serialParam.Add(SerialScoreColumnName, serialScoreList);

            Enqueue(Backend.GameData.Update, SerialTableName, new Where(), serialParam, serialBro =>
            {
                if (!serialBro.IsSuccess())
                    return;

                // Debug.Log("�̼�");
            });
        }

        //if (score >= _GameManager.BestScore)
        //{
        //    UpdateScoreToRank(score);
        //}
        //if(combo >= _GameManager.BestMaxCombo)
        //{
        //    UpdateComboToRank(combo);
        //}
        UpdateResultToRank(score, combo);

        isUpdatedRank = false;
    }


    // �ð� Ȯ��
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
            // �ð� ������Ʈ
            Param param = new Param();
            param.Add(LastLoginColumnName, serverDate);

            Enqueue(Backend.GameData.Update, TimeTableName, new Where(), param, updateTimeBro =>
            {
                Debug.Log("UpdateTimeBro : " + updateTimeBro);
            });

            GiftReset();

            // Serial�ޱ�  --> GetData_Serial�ʿ��� ����       **serialList�� ������ ����

            // weeklyGift �ʱ�ȭ
            if(serverDateTime.DayOfWeek == DayOfWeek.Monday)
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

    private void GetGMData()
    {
        //Backend.Social.GetGamerIndateByNickname()     **�������
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

    private void GiftReset()
    {
        // ��Ʈ �ҷ�����
        if(!PlayerPrefs.HasKey(key_DailyGiftChart))     // �ʱ� 1ȸ ���� ��
            return;

        var data = Backend.Chart.GetLocalChartData(PlayerPrefs.GetString(key_DailyGiftChart));
        if (data == "")
        {
            Debug.Log("��Ʈ�� ��� �Ұ���");
            return;
        }

        var bro = JsonMapper.ToObject(data);
        var rows = bro["rows"];

        Param param = new Param();
        for (int i = 0, length = rows.Count; i < length; i++)
        {
            param.Add(((EDailyGift)i).ToString(), new DailyGift
                (
                int.Parse(rows[i][FreeCountColumnName]["S"].ToString()),
                int.Parse(rows[i][AdCountColumnName]["S"].ToString()),
                int.Parse(rows[i][ChargeValueColumnName]["S"].ToString()),
                int.Parse(rows[i][AdDelayColumnName]["S"].ToString())
                ));
        }

        var giftBro = Backend.GameData.Update(DailyGiftTableName, new Where(), param);
        if (!giftBro.IsSuccess())
        {
            Debug.Log("Update Fail");
            return;
        }
    }

    /// <summary>
    /// *������
    /// </summary>
    private void ScoreReset()
    {
        _GameManager.AccumulateScore = 0;

        Param param = new Param();
        param.Add(AccumulateScoreColumnName, 0);

        Enqueue(Backend.GameData.Update, ResultTableName, new Where(), param, bro =>
        {
            // �ø��� ����
            if (!bro.IsSuccess())
                return;

            Debug.Log("�������� �ʱ�ȭ");
        });
    }


    // �ø���
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

            // ���� �����͵� �ҷ����� 7�� ���� : ���� �ʿ�
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
                    {   // 7�� ������ = 8��°�� �α� ����� ���� ����
                        RemoveSerialLog(serial, score);
                        return;
                    }

                    postInfo.serialDate = serial;

                    serialScoreList.Add(serial, score);
                    serialCodeList.Add(serial, code);

                    // ������ ����
                    GameUIManager.Instance.AddSerialPost(postInfo);
                }
            }

            if(isDailyReset && canSerial)
            {
                RecvSerialPost();

                // CanSerial�� �˾ƾ� �� �� �ֱ⶧���� Enqueue�� ������ ���� Ȯ���ϱ�
                //Enqueue(Backend.GameData.GetMyData, ProfileTableName, new Where(), profileBro =>
                //{
                //    if (!profileBro.IsSuccess())
                //        return;
                //    if (profileBro.GetReturnValuetoJSON()["rows"].Count <= 0)       // �ű� ����
                //        return;

                //    var profileRows = profileBro.FlattenRows();
                //    if (profileRows[0][UnlockSerialColumnName].ToString() == "True")            // CanSerial
                //    {
                //        RecvSerialPost();
                //    }
                //});
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

    //public void Update_Serial(int score)
    //{
    //    // ���̻� ������ �ʿ䰡 ���� : ä��
    //    if (serialList[serverDateTime] >= Values.DailySerialScore)
    //        return;

    //    serialList[serverDateTime] += score;

    //    Param param = new Param();
    //    param.Add(SerialListColumnName, serialList);

    //    Backend.GameData.Update(SerialTableName, new Where(), param, bro =>
    //    {
    //        if (!bro.IsSuccess())
    //            return;

    //        Debug.Log("�̼�");
    //    });
    //}

    // �ø�����Ʈ - Ʈ����� : GM����?
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

    #endregion

    #region ��ŷ 

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
            Debug.Log($"�ְ� ��� ���� : {score}, {combo} - {bro}");
        });
    }

    //private void UpdateScoreToRank(int value)
    //{
    //    Param param = new Param();
    //    param.Add(ScoreColumnName, value);

    //    Backend.URank.User.UpdateUserScore(scoreRankUUID, ResultTableName, userInDate_Result, param, updateBro =>
    //    {
    //        Debug.Log($"�ְ� ���� ���� : {value} - {updateBro}");
    //    });
    //}

    //private void UpdateComboToRank(int value)
    //{
    //    Param param = new Param();
    //    param.Add(ComboColumnName, value);

    //    Backend.URank.User.UpdateUserScore(comboRankUUID, ResultTableName, userInDate_Result, param, updateBro =>
    //    {
    //        Debug.Log($"�ְ� �޺� ���� : {value} - {updateBro}");
    //    });
    //}

    /// <summary>
    /// �����ʿ��� �ڽ��� ��ũ Ȯ��
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
            {   // **��ŷ�� �г����� ��ϵ��� �ʴ� ��찡 ����
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
            {   // Ż���� ȸ���� ���
                rankInfo.iconNum = 0;
                result.Add(rankInfo);
                Debug.Log("���� : " + rankInfo.nickname);
                continue;
            }

            JsonData data = resultBro.FlattenRows();
            try
            {   // *���� ��� 0�� ���
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

#region ��Ʈ

    /*
     * ������ (********Save���س���! : ���� �ʿ�)
     * �κ� ������
     * �ΰ��� ������
     */

    public void GetChartLists()
    {
        // Ű �� �ҷ�����
        //keyCharts = new Dictionary<string, string>();

        //foreach (string chartKey in Enum.GetValues(typeof(EChart)))
        //{
        //    if(PlayerPrefs.HasKey(chartKey))
        //    {
        //        keyCharts.Add(chartKey, PlayerPrefs.GetString(chartKey));
        //    }
        //}

        Backend.Chart.GetChartList(bro => 
        {
            if (!bro.IsSuccess())
                return;

            JsonData json = bro.FlattenRows();

            string name = "";
            string fileId = "";
            foreach (JsonData chart in json)
            {
                name = chart["chartName"].ToString();
                fileId = chart["selectedChartFileId"].ToString();

                Debug.Log($"��Ʈ Ž�� : {name} / {fileId}");

                if (name == EChart.Item.ToString())
                {
                    // GetChart_Item(outNum.ToString());
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
                    // * SerialChart�� �ۺ������� �������⶧���� ������ �ʿ䰡 ����
                    // : �׶��׶� �ҷ��� Ȯ���ϴ� ���� ���� (Ʈ������̱⿡)
                }

                //if (Int32.TryParse(chart["selectedChartFileId"].ToString(), out outNum) == false)             // ��Ʈ ������ �ȵȰ�쿡 ���� ����
                //{
                //    Debug.Log($"==== Chart �����ʿ� {name} ====");

                //    // *foreach?
                //    if(name == EChart.Item.ToString())
                //    {                    
                //        // **������ ��Ʈ�� ������ : jsonData�� � �����Ű�� �Ǳ⿡ �ʿ� ����
                //        //GetChart_ItemToServer(outNum.ToString());
                //    }
                //    else if(name == EChart.LobbyItem.ToString())
                //    {
                //        GetChart_LobbyItemToServer(outNum.ToString());
                //    }
                //    else if (name == EChart.InGameItem.ToString())
                //    {
                //        GetChart_InGameItemToServer(outNum.ToString());
                //    }
                //    else if(name == EChart.DailyGift.ToString())
                //    {
                //        GetChart_DailyGiftToServer(outNum.ToString());
                //    }
                //    else if (name == EChart.Shop.ToString())
                //    {
                //        GetChart_ShopItemToServer(outNum.ToString());
                //    }
                //}
            }
            
        });
    }


    /// <summary>
    /// ��Ʈ�� ���� �����Ϳ� ����
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

            Debug.Log("������ ���� - InGameItem");

            JsonData rows = bro.FlattenRows();                      // *�����ϸ�  rows[i]["itemName"][0].ToString(); �� �Ǿ����
            var inGameItemDatas = LevelData.Instance.InGameItemDatas;
            for (int i = 0, length = rows.Count; i < length; i++)
            {
                // **������ ������ �����̶�� ����
                //inGameItemDatas[i].itemName = rows[i]["itemName"][0].ToString();
                LevelData.Instance.InGameItemDatas[i].value = int.Parse(rows[i]["value"].ToString());
                LevelData.Instance.InGameItemDatas[i].valueTime = float.Parse(rows[i]["valueTime"].ToString());
                LevelData.Instance.InGameItemDatas[i].normalPercent = float.Parse(rows[i]["normalPercent"].ToString());
                LevelData.Instance.InGameItemDatas[i].rarePercent = float.Parse(rows[i]["rarePercent"].ToString());
            }
        });
    }

    /// <summary>
    /// ��Ʈ Ȯ��
    /// </summary>
    public void GetChart_InGameItem(string key)
    {
        var data = Backend.Chart.GetLocalChartData(key);
        if(data == "")
        {
            Debug.Log("�ΰ��� ������ ��Ʈ ����");
            SaveChart_InGameItem(key);
            return;
        }

        // �̹� ����� ������ -> SO
        var bro = JsonMapper.ToObject(data);
        var rows = bro["rows"];
        var inGameItemDatas = LevelData.Instance.InGameItemDatas;
        for (int i = 0, length = rows.Count; i < length; i++)
        {
            LevelData.Instance.InGameItemDatas[i].value = int.Parse(rows[i]["value"]["S"].ToString());
            LevelData.Instance.InGameItemDatas[i].valueTime = float.Parse(rows[i]["valueTime"]["S"].ToString());
            LevelData.Instance.InGameItemDatas[i].normalPercent = float.Parse(rows[i]["normalPercent"]["S"].ToString());
            LevelData.Instance.InGameItemDatas[i].rarePercent = float.Parse(rows[i]["rarePercent"]["S"].ToString());
        }
    }

    /// <summary>
    /// ������ �ִ� ��Ʈ �״�� ���
    /// </summary>
    public void GetChart_InGameItemToServer(string key)
    {
        Backend.Chart.GetChartContents(key, bro => 
        {
            if (!bro.IsSuccess())
                return;

            JsonData rows = bro.GetReturnValuetoJSON()["rows"];

            var inGameItemDatas = LevelData.Instance.InGameItemDatas;
            for (int i = 0, length = rows.Count; i < length; i++)
            {
                // **������ ������ �����̶�� ����
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

            Debug.Log("������ ���� - InGameItem");

            JsonData rows = bro.FlattenRows();                      // *�����ϸ�  rows[i]["itemName"][0].ToString(); �� �Ǿ����
            // var lobbyItems = LevelData.Instance.LobbyItemDatas;
            for (int i = 0, length = rows.Count; i < length; i++)
            {
                LevelData.Instance.LobbyItemDatas[i].value = int.Parse(rows[i]["value"].ToString());
                LevelData.Instance.LobbyItemDatas[i].price = int.Parse(rows[i]["price"].ToString());
                LevelData.Instance.LobbyItemDatas[i].isFree = rows[i]["isFree"].ToString() == "Y";

                // Debug.Log(lobbyItems[i].isFree);
            }
        });
    }

    /// <summary>
    /// ��Ʈ Ȯ��
    /// </summary>
    public void GetChart_LobbyItem(string key)
    {
        var data = Backend.Chart.GetLocalChartData(key);
        if (data == "")
        {
            Debug.Log("�κ� ������ ��Ʈ ����");
            SaveChart_LobbyItem(key);
            return;
        }

        // SO
    }

    /// <summary>
    /// ������ �ִ� ��Ʈ �״�� ���
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
            }
        });
    }

    public void GetChart_DailyGift(string key)
    {
        var data = Backend.Chart.GetLocalChartData(key);
        if (data == "")
        {
            Debug.Log("���� ���� �׸� ��Ʈ ����");
            SaveChart_DailyGift(key);
            return;
        }
    }

    /// <summary>
    /// ������ �ִ� ��Ʈ �״�� ���
    /// </summary>
    public void GetChart_DailyGiftToServer(string key)
    {
        Debug.Log(key);

        Backend.Chart.GetChartContents(key, bro =>
        {
            if (!bro.IsSuccess())
                return;
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

            PlayerPrefs.SetString(key_DailyGiftChart, key);

            GetData_DailyGift();
        });
    }

    /// <summary>
    /// ��Ʈ�� ���� �����Ϳ� ����
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
                LevelData.Instance.ShopDatas[i].price = int.Parse(rows[i]["price"].ToString());         // **���⼭?
                int itemLength = int.Parse(rows[i]["itemCount"].ToString());
                LevelData.Instance.ShopDatas[i].items = new FShopItem[itemLength];
                for (int j = 0; j < itemLength; j++)
                {   // item1ID, item1Value���� ����
                    LevelData.Instance.ShopDatas[i].items[j].id = int.Parse(rows[i][$"item{j + 1}ID"].ToString());
                    LevelData.Instance.ShopDatas[i].items[j].value = int.Parse(rows[i][$"item{j + 1}Value"].ToString());
                }
            }
        });
    }

    /// <summary>
    /// ��Ʈ Ȯ��
    /// </summary>
    public void GetChart_ShopItem(string key)
    {
        var data = Backend.Chart.GetLocalChartData(key);
        if (data == "")
        {
            Debug.Log("���� ��Ʈ ����");
            SaveChart_ShopItem(key);
            return;
        }

        var bro = JsonMapper.ToObject(data);
        var rows = bro["rows"];
        for (int i = 0, length = rows.Count; i < length; i++)
        {
            LevelData.Instance.ShopDatas[i].type = (EShopItem)Enum.Parse(typeof(EShopItem), rows[i]["type"]["S"].ToString());
            LevelData.Instance.ShopDatas[i].price = int.Parse(rows[i]["price"]["S"].ToString());         // **���⼭?
            int itemLength = int.Parse(rows[i]["itemCount"]["S"].ToString());
            LevelData.Instance.ShopDatas[i].items = new FShopItem[itemLength];
            for (int j = 0; j < itemLength; j++)
            {   // item1ID, item1Value���� ����
                LevelData.Instance.ShopDatas[i].items[j].id = int.Parse(rows[i][$"item{j + 1}ID"]["S"].ToString());
                LevelData.Instance.ShopDatas[i].items[j].value = int.Parse(rows[i][$"item{j + 1}Value"]["S"].ToString());
            }
        }
    }

    /// <summary>
    /// ������ �ִ� ��Ʈ �״�� ���
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
                LevelData.Instance.ShopDatas[i].price = int.Parse(rows[i]["price"].ToString());         // **���⼭?
                int itemLength = int.Parse(rows[i]["itemCount"].ToString());
                LevelData.Instance.ShopDatas[i].items = new FShopItem[itemLength];
                for (int j = 0; j < itemLength; j++)
                {   // item1ID, item1Value���� ����
                    LevelData.Instance.ShopDatas[i].items[j].id = int.Parse(rows[i][$"item{j + 1}ID"].ToString());
                    LevelData.Instance.ShopDatas[i].items[j].value = int.Parse(rows[i][$"item{j + 1}Value"].ToString());
                }
                // LevelData.Instance.ShopDatas[i].nameNum = int.Parse(rows[i]["languageNum"].ToString());
            }
        });
    }

#endregion ��Ʈ

#region Ȯ��

    /// <summary>
    /// ������ 2�� �̻��� �ʿ��ϸ� ���� �����͸� �����ؾ���
    /// </summary>
    public FItemInfo Probability_NormalGacha()
    {
        FItemInfo result;
        var bro = Backend.Probability.GetProbability(key_ItemGachaProbability);

        if (!bro.IsSuccess())
        {
            Debug.LogError(bro.ToString());
            result.icon = null;
            result.item = 0;
            result.count = 0;
            return result;
        }

        JsonData json = bro.GetFlattenJSON();

        //var item = (EItem)Enum.Parse(typeof(EItem), json["elements"]["type"].ToString());
        var item = (EItem)int.Parse(json["elements"]["itemID"].ToString());
        int value = int.Parse(json["elements"]["value"].ToString());

        Debug.Log($"���� ��í ������ ȹ�� : {item} - {value}");
        Param param = new Param();
        param.Add("ȹ�� ������", item);
        InsertLog("��í ������ ȹ��", param);
        //InsertLog("��í ������ ȹ��", string.Format("{0} - {1}", item, value));

        AddItem(item, value);

        _GameManager.UseItemGacha();

        result.icon = GetItemSprite(item);
        result.item = item;
        result.count = value;

        return result;
    }


#endregion Ȯ��

#region ����

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

            // ��ȿ�Ⱓ ��¥
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
            Debug.LogError("������ ���� �� �����ϴ�.");
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
                Debug.Log("�������� ���� ����");
                continue;
            }

            item = (EItem)Enum.Parse(typeof(EItem), post["item"]["name"].ToString());
            //value = int.Parse(post["item"]["value"].ToString());
            count = int.Parse(post["itemCount"].ToString());

            Debug.Log($"���� ������ ȹ�� : {item} - {count}");

            log += $"{item} - {count}";

            AddItem(item, count);
        }

        // InsertLog("���� ������ ȹ��", $"{postIndate} : {log}");
    }

    #endregion ����


    #region �ø���

    /// <summary>
    /// ��� + 
    /// �ϳ� �������ֱ� : ����ó�� ���̴� ������Ʈ
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
    /// ���� �ޱ�
    /// </summary>
    private void RecvSerialPost()
    {
        // ���� �ð� : ����Ÿ��
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

            postInfo.remainTime = new TimeSpan(Values.SerialRemainDay, 0, 0, 0);         // 7�� ����

            // postInfo.inDate = rows["inDate"].ToString();

            postInfo.serialDate = serverDate;

            // ScoreReset();

            // ������ ����
            DispatcherAction(() => GameUIManager.Instance.AddSerialPost(postInfo));
        });
    }

    /// <summary>
    /// ��¥ �ø��� ��ȣ�� �ִ� ��
    /// GM ������ num ã�� Ʈ��������� ������Ű��
    /// ��Ʈ ������ �ҷ��ͼ� ���� ��ȣ �ø��� return �ϱ�
    /// </summary>
    public string RecvSerialCode(string date)
    {
        // Ű ���� ������ ������ ��ȯ
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

            // �ø��� ��� InsertLog
            RecvSerialLog(serialCodeList[date]);
        });

        return serialCode;
    }

    #endregion


    #region �α� ���

    /* �α� ����Ʈ
     *  �г��� ����
     *  ���̾� ���� -> X
     *  �ְ� ���� ���� -> X
     *  �ְ� �޺� ���� -> X
     *  ��í ������
     *  ���� ������ -> X
     /// �߰� �ʿ�
     *  ���� ������ ����
     *  �κ� ������ ����
     *  ���� �÷���
     *  ����
     */

    public void ShopItemyLog(EShopItem shopItem)
    {
        Param param = new Param();
        param.Add("���ž�����", shopItem.ToString());
        param.Add("���� ���� ���̾�", GameManager.Instance.CashDia);
        param.Add("���� ���� ���̾�", GameManager.Instance.FreeDia);

        InsertLog("���� ������ ����", param);
    }

    public void LobbyItemFreeLog(ELobbyItem item)
    {
        Param param = new Param();
        param.Add("���ž�����", item.ToString());

        InsertLog("���� �κ� ������ ����", param);
    }
    public void LobbyItemDiaLog(ELobbyItem item, int useCash, int useFree, int currentCash, int currentFree)
    {
        Param param = new Param();
        param.Add("���ž�����", item.ToString());
        param.Add("��� ���� ���̾�", useCash);
        param.Add("��� ���� ���̾�", useFree);
        param.Add("���� ���� ���̾�", currentCash);
        param.Add("���� ���� ���̾�", currentFree);

        InsertLog("���̾� �κ� ������ ����", param);
    }

    public void TicketDiaLog(int useCash, int useFree, int currentCash, int currentFree)
    {
        Param param = new Param();
        param.Add("��� ���� ���̾�", useCash);
        param.Add("��� ���� ���̾�", useFree);
        param.Add("���� ���� ���̾�", currentCash);
        param.Add("���� ���� ���̾�", currentFree);

        InsertLog("Ƽ�� ���̾� ����", param);
    }

    public void GamePlayLog(int score, int dia, bool isRevive, bool isDoubleReward)
    {
        Param param = new Param();
        param.Add("�÷��̽ð�", DateTime.Now - GameStartTime);
        param.Add("��� �ǹ�", _GameManager.GameController.UseFeverCount);
        param.Add("���� �ǹ�",_GameManager.GameController.Fever);
        param.Add("ȹ�� ����",score);
        param.Add("ȹ�� ��ȭ",dia);
        param.Add("Revive", isRevive);
        param.Add("Double Reward", isDoubleReward);

        InsertLog("���� �÷���", param);
    }

    public void InGameItemLog(int itemAddCount, int nomralCount, Dictionary<EInGameItem, int> normalItems, 
                                        int rareCount, Dictionary<EInGameItem, int> rareItems)
    {
        Param param = new Param();
        param.Add("������ ������ �� Ƚ��", itemAddCount);
        param.Add("ȹ�� �Ϲ� ���� ��", nomralCount);
        param.Add("�Ϲ� ���� ������", normalItems);
        param.Add("ȹ�� ������ ���� ��", rareCount);
        param.Add("������ ���� ������", rareItems);

        InsertLog("ȹ�� ������", param);
    }

    private void RecvSerialLog(string code)
    {
        Param param = new Param();
        param.Add("�ð�", DateTime.Now);
        param.Add("�ø��� �ڵ�", code);

        InsertLog("�ø��� ���� ����", param);
    }

    private void RemoveSerialLog(string day, int score)
    {
        Param param = new Param();
        param.Add("��¥", day);
        param.Add("����", score);

        InsertLog("�ø��� ���� ����", param);
    }

    private void InsertLog(string logType, Param param)
    {
        Enqueue(Backend.GameLog.InsertLog, logType, param, bro =>
        {
            Debug.Log($"InsertLog : {logType} - {param} - {bro}");              // *param ��°���?
        });
    }

#endregion �α� ���

    /// <summary>
    /// count -> value�� ����
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
            case EItem.SuperFeverStart:
            case EItem.BonusScore:
            case EItem.PermShield:
                //GameManager.ItemsCount[(ELobbyItem)((int)item - Values.StartNum_LobbyItem)]++;
                _GameManager.ItemsCount[(int)item - Values.StartNum_LobbyItem] += value;
                break;
            case EItem.SuperFever:
                GameManager.Instance.GameController.AddFever(value);
                break;
            case EItem.BonusCharacter:
            case EItem.BombFullGauge:
            case EItem.TimeCharge:
            case EItem.PreventInterrupt:
            case EItem.TempShield:
                Debug.LogError("�ΰ��� ������");
                break;
            //case EItem.Revive:
            //case EItem.DoubleReward:
            //    Debug.LogError("�ƿ����� ���");
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
                return LevelData.Instance.GoodsSprites[(int)item - Values.StartNum_Goods];
            case EItem.AddMaxTime:
            case EItem.PermBombPower:
            case EItem.MaxItem:
            case EItem.SuperFeverStart:
            case EItem.BonusScore:
            case EItem.PermShield:
                return LevelData.Instance.LobbyItemDatas[(int)item - Values.StartNum_LobbyItem].sprite;
            case EItem.BonusCharacter:      // �迭 [4]
            case EItem.TempBombPower:
            case EItem.BombFullGauge:
            case EItem.TimeCharge:
            case EItem.PreventInterrupt:
            case EItem.TempShield:
            case EItem.SuperFever:
                return LevelData.Instance.InGameItemDatas[(int)item - Values.StartNum_InGameItem].sprite;
        }

        return null;
    }

    #region ���� �׸��

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

    public bool SwitchDeviceToken()
    {
        Param param = new Param();
        param.Add(PushAlarmColumnName, isAlarm);

        var bro = Backend.GameData.Update(ProfileTableName, new Where(), param);
        if (!bro.IsSuccess())
            return false;

        isAlarm = !isAlarm;

#if !UNITY_EDITOR
            var text = Backend.Android.GetDeviceToken();
            Debug.Log("Ǫ�þ˸� : " + text);
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
        FederationType type = _type == ELogin.Google ? FederationType.Google : FederationType.Facebook;
        var bro = Backend.BMember.ChangeCustomToFederation(GetTokens(), type);
        if(bro.IsSuccess())
        {
            Debug.Log("������ȯ : " + bro);
            GameSceneManager.Instance.Restart();
        }
        else
        {
            Debug.Log("��ȯ���� : " + bro);
        }
    }

    public void LogOut()
    {
        var bro = Backend.BMember.Logout();
        if(bro.IsSuccess())
        {
            Debug.Log("�α׾ƿ�");
            GameSceneManager.Instance.Restart();
        }
    }

    public void SignOut()
    {
        var bro = Backend.BMember.SignOut();
        if (bro.IsSuccess())
        {
            Debug.Log("Ż��Ϸ�");
            GameSceneManager.Instance.Restart();
        }
    }

#endregion
}