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
    [SerializeField] Button policyButton;           // ���
    [SerializeField] Button noticeButton;           // ��������
    [SerializeField] Button csButton;               // ��������
    [SerializeField] Button infoButton;             // �˸���
    [SerializeField] Button couponButton;       // �������
    [SerializeField] Button guideButton;        // ���ӹ��

    [Header("========== Account Setting ==========")]
    [SerializeField] TextMeshProUGUI uuidText;
    [SerializeField] Button uuidCopyButton;
    [SerializeField] Button accountButton;          // ������ȯ, Ż��?

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

    //**������ ���� �� �����ؾ���. 
    public void SwitchLanguage()
    {
        BackEndServerManager.Instance.ChangeLanguage();
        // GameSceneManager.Instance.ReloadScene();
    }

    /// <summary>
    /// �̿� ���
    /// </summary>
    public void OnPolicy()
    {
        _AudioManager.PlaySFX(ESFX.Touch);
        GameApplication.Instance.ShowWebView("AbleX", "http://ablegames.co.kr/terms-of-service");
    }

    /// <summary>
    /// �������� : �ڳ�?
    /// </summary>
    public void OnNotice()
    {
        _AudioManager.PlaySFX(ESFX.Touch);
        //Application.OpenURL("https://cafe.naver.com/ArticleList.nhn?search.clubid=30546852&search.menuid=1&search.boardtype=L");
        GameApplication.Instance.ShowWebView("Cafe", "https://cafe.naver.com/ArticleList.nhn?search.clubid=30546852&search.menuid=1&search.boardtype=L");
    }

    /// <summary>
    /// ��������
    /// </summary>
    public void OnCS()
    {
        _AudioManager.PlaySFX(ESFX.Touch);
        GameApplication.Instance.ShowWebView("AbleX", "http://ablegames.co.kr/#contact");
    }

    /// <summary>
    /// �˸���
    /// </summary>
    public void OnInfo()
    {
        _AudioManager.PlaySFX(ESFX.Touch);
        _GamePopup.OpenPopup(EGamePopup.Notice);
    }

    /// <summary>
    /// ���� ���
    /// </summary>
    public void OnCoupon()
    {
        _AudioManager.PlaySFX(ESFX.Touch);
        _GamePopup.OpenPopup(EGamePopup.Coupon);
    }

    /// <summary>
    /// ���� ���
    /// </summary>
    public void OnGuide()
    {
        _AudioManager.PlaySFX(ESFX.Touch);
    }

    public void OnCopy()
    {
        _AudioManager.PlaySFX(ESFX.Touch);
        SystemPopupUI.Instance.OpenNoneTouch(72);

        uuidText.text.CopyToClipboard();
    }

    public void OnAccount()
    {
        _AudioManager.PlaySFX(ESFX.Touch);
        _GamePopup.OpenPopup(EGamePopup.Account);
    }
}