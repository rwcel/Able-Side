using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(AnimEvent))]
public class NoneTouchUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TextMeshProUGUI contentsText;

    private Animator anim;
    private AnimEvent animEvent;

    public static readonly int _Anim_Close = Animator.StringToHash("ClosePopup");


    private void Awake()
    {
        anim = GetComponent<Animator>();
        animEvent = GetComponent<AnimEvent>();
    }

    private void Start()
    {
        animEvent.SetAnimEvent(ClosePopup);
    }

    public void SetData(string _contentsText)
    {
        contentsText.text = _contentsText;

        gameObject.SetActive(true);
    }

    public void SetData(int _contentsNum)
    {
        contentsText.text = _contentsNum.Localization();

        gameObject.SetActive(true);
    }

    void ClosePopup()
    {
        gameObject.SetActive(false);
    }
}
