using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionUI : DockUI
{
    private enum EGameSetting
    {
        BGM, SFX, Alarm, Language
    }

    [System.Serializable]
    private struct FSetting
    {
        [HideInInspector] public EGameSetting type;
        public Button button;
        [HideInInspector] public Image image;
        public Sprite[] sprites;
        [HideInInspector] public int spriteNum;
    }

    [SerializeField] ScrollRect scrollRect;

    [Header("========== Game Setting ==========")]
    [SerializeField] FSetting[] gameSettings;


    [Header("========== Term & Services==========")]
    [SerializeField] Button policyButton;           // 약관
    [SerializeField] Button noticeButton;           // 공지사항
    [SerializeField] Button csButton;               // 고객센터
    [SerializeField] Button infoButton;             // 알림판
    [SerializeField] Button couponButton;       // 쿠폰등록
    [SerializeField] Button guideButton;        // 게임방법

    [Header("========== Account Setting ==========")]
    [SerializeField] TextMeshProUGUI uuidText;
    [SerializeField] Button uuidCopyButton;
    [SerializeField] Button accountButton;          // 계정전환, 탈퇴?

    [Header("========== Version ==========")]
    [SerializeField] TextMeshProUGUI versionText;

    AudioManager _AudioManager;
    GamePopup _GamePopup;

    private void Awake()
    {
        for (int i = 0; i < gameSettings.Length; i++)
        {
            gameSettings[i].image = gameSettings[i].button.GetComponent<Image>();
        }
    }

    public override void OnStart()
    {
        _AudioManager = AudioManager.Instance;
        _GamePopup = GamePopup.Instance;

        InitSet();

        AddListeners();

        //AddObserve();
    }

    public override void UpdateDatas()
    {
        scrollRect.verticalNormalizedPosition = 1f;
    }

    void InitSet()
    {
        uuidText.text = $"{BackEndServerManager.Instance.UUID}";
        versionText.text = $"Version {Application.version}";
    }

    private void AddListeners()
    {
        gameSettings[(int)EGameSetting.BGM].button.onClick.AddListener(() =>
        {
            SetGameSetting(EGameSetting.BGM, AudioManager.Instance.SwitchBGM() ? 1 : 0);
            _AudioManager.PlaySFX(ESFX.Toggle);
        });
        SetGameSetting(EGameSetting.BGM, AudioManager.Instance.OnOffBGM ? 1 : 0);

        gameSettings[(int)EGameSetting.SFX].button.onClick.AddListener(() =>
        {
            SetGameSetting(EGameSetting.SFX, AudioManager.Instance.SwitchSFX() ? 1 : 0);
            _AudioManager.PlaySFX(ESFX.Toggle);
        });
        SetGameSetting(EGameSetting.SFX, AudioManager.Instance.OnOffSFX ? 1 : 0);

        gameSettings[(int)EGameSetting.Alarm].button.onClick.AddListener(() => 
        {
            SetGameSetting(EGameSetting.Alarm, BackEndServerManager.Instance.SwitchDeviceToken() ? 1 : 0);
            _AudioManager.PlaySFX(ESFX.Toggle);
        });
        SetGameSetting(EGameSetting.Alarm, BackEndServerManager.Instance.IsAlarm ? 1 : 0);

        gameSettings[(int)EGameSetting.Language].button.onClick.AddListener(SwitchLanguage);

        policyButton.onClick.AddListener(OnPolicy);
        noticeButton.onClick.AddListener(OnNotice);
        csButton.onClick.AddListener(OnCS);
        infoButton.onClick.AddListener(OnInfo);
        couponButton.onClick.AddListener(OnCoupon);
        guideButton.onClick.AddListener(OnGuide);

        uuidCopyButton.onClick.AddListener(OnCopy);
        accountButton.onClick.AddListener(OnAccount);
    }

    private void SetGameSetting(EGameSetting setting, int num)
    {
        gameSettings[(int)setting].spriteNum = num;
        gameSettings[(int)setting].image.sprite = gameSettings[(int)setting].sprites[num];
    }

    private void AddObserve()
    {
        // GameSceneManager.Instance.MovePrevScene();
    }

    //**씬변경 조금 더 생각해야함. 
    public void SwitchLanguage()
    {
        _AudioManager.PlaySFX(ESFX.Touch);
        BackEndServerManager.Instance.ChangeLanguage();
        // GameSceneManager.Instance.ReloadScene();
    }

    /// <summary>
    /// 이용 약관
    /// </summary>
    public void OnPolicy()
    {
        GameApplication.Instance.ShowWebView("AbleX", "http://ablegames.co.kr/terms-of-service");
    }

    /// <summary>
    /// 공식카페 공지사항
    /// </summary>
    public void OnNotice()
    {
        GameApplication.Instance.ShowWebView("Cafe", "https://cafe.naver.com/ArticleList.nhn?search.clubid=30546852&search.menuid=1&search.boardtype=L");
    }

    /// <summary>
    /// 고객센터
    /// </summary>
    public void OnCS()
    {
        GameApplication.Instance.ShowWebView("Cafe", "https://cafe.naver.com/MemoList.nhn?search.clubid=30546852&search.menuid=20&viewType=pc");
    }

    /// <summary>
    /// 알림판
    /// </summary>
    public void OnInfo()
    {
        _GamePopup.OpenPopup(EGamePopup.Notice);
    }

    /// <summary>
    /// 쿠폰 등록
    /// </summary>
    public void OnCoupon()
    {
        //_GamePopup.OpenPopup(EGamePopup.Coupon);

        SystemPopupUI.Instance.OpenInputTwoButton(Values.Input_Limit_Coupon, 181, 188, CheckCoupon, null);
    }

    private void CheckCoupon(string _text)
    {
        if (BackEndServerManager.Instance.IsValidCoupon(_text))
        {
            _GamePopup.OpenPopup(EGamePopup.Reward, null, () => _GamePopup.AllClosePopup(null));
            SystemPopupUI.Instance.OpenNoneTouch(6);
        }
        else
        {
            SystemPopupUI.Instance.OpenNoneTouch(4);
        }
    }

    /// <summary>
    /// 게임 방법
    /// </summary>
    public void OnGuide()
    {
        _GamePopup.OpenPopup(EGamePopup.Guide);
        // GameApplication.Instance.ShowWebView("Cafe", "https://cafe.naver.com/ablegames/68");
    }

    public void OnCopy()
    {
        _AudioManager.PlaySFX(ESFX.Touch);
        SystemPopupUI.Instance.OpenNoneTouch(16);

        uuidText.text.CopyToClipboard();
    }

    public void OnAccount()
    {
        _GamePopup.OpenPopup(EGamePopup.Account);
    }
}