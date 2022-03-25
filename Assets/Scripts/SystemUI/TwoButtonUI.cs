using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(AnimEvent))]
public class TwoButtonUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI contentsText;
    [SerializeField] TextMeshProUGUI leftButtonText;
    [SerializeField] TextMeshProUGUI rightButtonText;
    [SerializeField] Button leftButton;
    [SerializeField] Button rightButton;

    private Animator anim;
    private AnimEvent animEvent;

    public static readonly int _Anim_Close = Animator.StringToHash("Close");


    private void Awake()
    {
        anim = GetComponent<Animator>();
        animEvent = GetComponent<AnimEvent>();
    }

    private void Start()
    {
        animEvent.SetAnimEvent(ClosePopup);
    }

    public void SetData(string _titleText, string _contentsText, string _leftButtonText, string _rightButtonText,
                                    System.Action _leftButtonAction = null, System.Action _rightButtonAction = null)
    {
        titleText.text = _titleText;
        contentsText.text = _contentsText;
        leftButtonText.text = _leftButtonText;
        rightButtonText.text = _rightButtonText;

        leftButton.onClick.AddListener(() => OnConfirm(_leftButtonAction));
        rightButton.onClick.AddListener(() => OnConfirm(_rightButtonAction));

        gameObject.SetActive(true);
    }

    public void SetData(int _titleNum, int _contentsNum, int _leftButtonNum, int _rightButtonNum,
                                    System.Action _leftButtonAction = null, System.Action _rightButtonAction = null)
    {
        titleText.text = _titleNum.Localization();
        contentsText.text = _contentsNum.Localization();
        leftButtonText.text = _leftButtonNum.Localization();
        rightButtonText.text = _rightButtonNum.Localization();

        leftButton.onClick.AddListener(() => OnConfirm(_leftButtonAction));
        rightButton.onClick.AddListener(() => OnConfirm(_rightButtonAction));

        gameObject.SetActive(true);
    }

    public void SetData(int _titleNum, string _contentsText, int _leftButtonNum, int _rightButtonNum,
                                System.Action _leftButtonAction = null, System.Action _rightButtonAction = null)
    {
        titleText.text = _titleNum.Localization();
        contentsText.text = _contentsText;
        leftButtonText.text = _leftButtonNum.Localization();
        rightButtonText.text = _rightButtonNum.Localization();

        leftButton.onClick.AddListener(() => OnConfirm(_leftButtonAction));
        rightButton.onClick.AddListener(() => OnConfirm(_rightButtonAction));

        gameObject.SetActive(true);
    }

    void OnConfirm(System.Action buttonAction)
    {
        buttonAction?.Invoke();

        leftButton.onClick.RemoveAllListeners();
        rightButton.onClick.RemoveAllListeners();

        anim.SetTrigger(_Anim_Close);
    }

    void ClosePopup()
    {
        gameObject.SetActive(false);
    }
}
