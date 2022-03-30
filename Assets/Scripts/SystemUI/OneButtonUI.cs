using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(AnimEvent))]
public class OneButtonUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI contentsText;
    [SerializeField] TextMeshProUGUI buttonText;
    [SerializeField] Button button;

    private Animator anim;
    private AnimEvent animEvent;

    public static readonly int _Anim_Close = Animator.StringToHash("Close");
    

    private void Awake()
    {
        anim = GetComponent<Animator>();
        animEvent = GetComponent<AnimEvent>();
    }

    private void OnEnable()
    {
        AudioManager.Instance.PlaySFX(ESFX.Touch);
    }

    private void Start()
    {
        animEvent.SetAnimEvent(ClosePopup);
    }

    public void SetData(string _titleText, string _contentsText, string _buttonText, System.Action _buttonAction = null)
    {
        titleText.text = _titleText;
        contentsText.text = _contentsText;
        buttonText.text = _buttonText;

        button.onClick.AddListener(() => OnConfirm(_buttonAction));

        gameObject.SetActive(true);
    }

    public void SetData(int _titleNum, int _contentsNum, int _buttonNum, System.Action _buttonAction = null)
    {
        titleText.text = _titleNum.Localization();
        contentsText.text = _contentsNum.Localization();
        buttonText.text = _buttonNum.Localization();

        button.onClick.AddListener(() => OnConfirm(_buttonAction));

        gameObject.SetActive(true);
    }

    void OnConfirm(System.Action buttonAction)
    {
        AudioManager.Instance.PlaySFX(ESFX.Touch);

        buttonAction?.Invoke();

        button.onClick.RemoveAllListeners();

        anim.SetTrigger(_Anim_Close);
    }

    void ClosePopup()
    {
        gameObject.SetActive(false);
    }

}
