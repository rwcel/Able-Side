using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AdvertiseUI : MonoBehaviour
{
    [Header("Objs")]
    [SerializeField] GameObject countObj;
    [SerializeField] GameObject delayObj;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] TextMeshProUGUI delayText;
    [SerializeField] Button adButton;
    [SerializeField] Button cancelButton;

    private Animator anim;
    private AnimEvent animEvent;

    private EDailyGift type;
    private DailyGift curDailyGift;

    System.Action OnRewardAd;
    string delayCoroutineName;

    public static readonly int _Anim_Close = Animator.StringToHash("Close");

    private void Awake()
    {
        anim = GetComponent<Animator>();
        animEvent = GetComponent<AnimEvent>();
    }

    private void Start()
    {
        animEvent.SetAnimEvent(ClosePopup);

        adButton.onClick.AddListener(OnAd);
        cancelButton.onClick.AddListener(OnCancel);
    }

    private void OnEnable()
    {
        Debug.Log("Ad Enable");
        AudioManager.Instance.PlaySFX(ESFX.Touch);

        if (delayCoroutineName != null
            && delayCoroutineName != "")
        {
            StartCoroutine(delayCoroutineName);
        }
    }


    public void SetData(EDailyGift _type, int _titleNum, System.Action _adRewardAction = null)
    {
        Debug.Log("Ad SetData");

        type = _type;
        titleText.text = _titleNum.Localization();
        curDailyGift = GameManager.Instance.DailyGifts[type];

        if (delayCoroutineName != null
            && delayCoroutineName != "")
        {
            StopCoroutine(nameof(CoUpdateDelay));
            delayCoroutineName = "";
        }

        if (curDailyGift.adDelay > 0)
        {
            countObj.SetActive(false);
            delayObj.SetActive(true);
            delayCoroutineName = nameof(CoUpdateDelay);
        }
        else
        {
            countObj.SetActive(true);
            delayObj.SetActive(false);
        }

        delayText.text = $"{curDailyGift.adDelay}{20.Localization()}";        // 초로만 확인
        countText.text = $"({curDailyGift.adCount}/{LevelData.Instance.DailyGiftDatas[(int)type].adCount})";

        OnRewardAd = _adRewardAction;

        gameObject.SetActive(true);
    }

    IEnumerator CoUpdateDelay()
    {
        while (curDailyGift.adDelay > 0)
        {
            delayText.text = $"{curDailyGift.adDelay}{20.Localization()}";
            yield return Values.Delay1;
        }

        countText.text = $"({curDailyGift.adCount}/{LevelData.Instance.DailyGiftDatas[(int)type].adCount})";

        countObj.SetActive(true);
        delayObj.SetActive(false);
    }

    void OnAd()
    {
        // 딜레이 진행중이거나 개수가 없으면 리턴
        if (curDailyGift.adCount <= 0
            || delayObj.activeSelf)
        {
            SystemPopupUI.Instance.OpenNoneTouch(52);
            OnCancel();
            return;
        }

        AudioManager.Instance.PlaySFX(ESFX.Touch);
        UnityAdsManager.Instance.ShowRewardAD(OnConfirm
                                                              , () => GameManager.Instance.SetAdTimer(type)
                                                              , type);
    }

    void OnConfirm()
    {
        OnRewardAd?.Invoke();

        anim.SetTrigger(_Anim_Close);
    }

    void OnCancel()
    {
        AudioManager.Instance.PlaySFX(ESFX.Touch);

        OnRewardAd = null;

        anim.SetTrigger(_Anim_Close);
    }

    void ClosePopup()
    {
        gameObject.SetActive(false);
    }
}
