using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(AnimEvent))]
public class InputFieldTwoButtonUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_InputField inputField;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI contentsText;          // PlaceHolder
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

    private void OnEnable()
    {
        AudioManager.Instance.PlaySFX(ESFX.Touch);
    }

    private void Start()
    {
        animEvent.SetAnimEvent(ClosePopup);
    }

    public void SetData(int _charLimit, int _titleNum, int _contentsNum,
                                    System.Action<string> _leftButtonAction = null, System.Action<string> _rightButtonAction = null)
    {
        inputField.text = "";
        inputField.characterLimit = _charLimit;

        titleText.text = _titleNum.Localization();
        contentsText.text = string.Format(_contentsNum.Localization(), _charLimit);

        leftButton.onClick.AddListener(() => OnConfirm(_leftButtonAction));
        rightButton.onClick.AddListener(() => OnConfirm(_rightButtonAction));

        gameObject.SetActive(true);
    }


    void OnConfirm(System.Action<string> buttonAction)
    {
        AudioManager.Instance.PlaySFX(ESFX.Touch);

        buttonAction?.Invoke(inputField.text);

        leftButton.onClick.RemoveAllListeners();
        rightButton.onClick.RemoveAllListeners();

        anim.SetTrigger(_Anim_Close);
    }

    void ClosePopup()
    {
        gameObject.SetActive(false);
    }
}
