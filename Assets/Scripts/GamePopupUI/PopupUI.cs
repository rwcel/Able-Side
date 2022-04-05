using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupUI : MonoBehaviour
{
    [SerializeField] protected UnityEngine.UI.Button closeButton;

    protected GameManager _GameManager;
    protected GamePopup _GamePopup;

    protected Animator anim;
    protected AnimEvent animEvent;

    protected static readonly string _Anim_Close = "Close";

    protected virtual void Awake()
    {
        anim = GetComponent<Animator>();
        animEvent = GetComponent<AnimEvent>();
    }

    protected virtual void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(_GamePopup.ClosePopup);
    }

    protected void OnEnable()
    {
        if(_GameManager == null)
        {
            _GameManager = GameManager.Instance;
        }
        if(_GamePopup == null)
        {
            _GamePopup = GamePopup.Instance;
        }

        UpdateData();
    }

    protected virtual void UpdateData() 
    { }

    public void PlayCloseAnim(System.Action closeAction)
    {
        closeAction += () => gameObject.SetActive(false);
        animEvent.SetAnimEvent(closeAction);
        anim.SetTrigger(_Anim_Close);
    }
}
