using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    [SerializeField] GameObject privacyUI;              // 개인정보 동의
    [SerializeField] GameObject loginUI;                // 로그인 방식
    [SerializeField] GameObject nicknameUI;          // 닉네임 결정
    [SerializeField] GameObject updateUI;             // 버전 업데이트 필요

    [SerializeField] Button startButton;

    [SerializeField] Button googleButton;
    [SerializeField] Button facebookButton;
    [SerializeField] Button guestButton;

    protected Stack<GameObject> openObjects;

    private Animator animator;
    private AnimEvent animEvent;
    private static readonly int _Anim_Start = Animator.StringToHash("Start");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animEvent = GetComponent<AnimEvent>();
    }

    private void Start()
    {
        openObjects = new Stack<GameObject>();

        BackEndServerManager backEndServerManager = BackEndServerManager.Instance;

        startButton.onClick.AddListener(() =>
        {
            backEndServerManager.LoginWithTheBackendToken();
            startButton.enabled = false;            // 연속 클릭 못하게
        });

        googleButton.onClick.AddListener(backEndServerManager.GoogleLogin);
        facebookButton.onClick.AddListener(backEndServerManager.FacebookLogin);
        guestButton.onClick.AddListener(backEndServerManager.GuestLogin);
    }

    private void ShowUI(GameObject uiObject)
    {
        openObjects.Push(uiObject);
        uiObject.SetActive(true);
    }

    private void CloseUI()
    {
        if (openObjects.Count <= 0)
            return;

        openObjects.Pop().SetActive(false);
    }

    public void CloseAll()
    {
        foreach (var openObj in openObjects)
        {
            openObj.SetActive(false);
        }
        openObjects.Clear();
    }


    public void ShowPrivacyUI()
    {
        CloseUI();
        ShowUI(privacyUI);
    }

    public void ShowLoginUI()
    {
        CloseUI();
        ShowUI(loginUI);
    }

    public void ShowNickNameUI()
    {
        CloseUI();
        ShowUI(nicknameUI);
    }

    // 업데이트 UI 표시
    public void ShowUpdateUI()
    {
        // 업데이트 UI 표시
        ShowUI(updateUI);
    }

    public void AnimStart()
    {
        animEvent.SetAnimEvent(() => 
        {
            GameSceneManager.Instance.SceneChange(GameSceneManager.EScene.InGame);
        });
        animator.SetTrigger(_Anim_Start);
    }
}
