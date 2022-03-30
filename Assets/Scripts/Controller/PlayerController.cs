using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private bool isAuto = false;        // 오토모드

    [Header("========== Buttons ==========")]
    [SerializeField] UnityEngine.UI.Button leftButton;
    [SerializeField] UnityEngine.UI.Button rightButton;
    [SerializeField] UnityEngine.UI.Button feverButton;
    [SerializeField] UnityEngine.UI.Button itemButton;

    [Header("========== Model ==========")]
    [SerializeField] GameObject shieldObj;
    [SerializeField] Animator playerAnim;

    private GameManager _GameManager;
    private GameCharacter gameCharacter;

    private static readonly int _Anim_Left = Animator.StringToHash("Left");
    private static readonly int _Anim_Right = Animator.StringToHash("Right");
    private static readonly int _Anim_Fever = Animator.StringToHash("IsFever");
    private static readonly int _Anim_Obstacle = Animator.StringToHash("IsObstacle");
    private static readonly int _Anim_Clear = Animator.StringToHash("Clear");

    private void Start()
    {
        _GameManager = GameManager.Instance;
        gameCharacter = _GameManager.GameCharacter;

        AddActions();
        AddListeners();
        AddObserves();

        if (isAuto)
            StartCoroutine(nameof(CoAutoClick));
    }

    void AddActions()
    {
        _GameManager.OnGameStart += (value) => transform.GetChild(0).gameObject.SetActive(value);
    }

    void AddListeners()
    {
        leftButton.onClick.AddListener(OnLeft);
        rightButton.onClick.AddListener(OnRight);

        feverButton.onClick.AddListener(OnFever);
        itemButton.onClick.AddListener(OnItem);
    }

    void AddObserves()
    {
        var gameController = _GameManager.GameController;

        gameController.ObserveEveryValueChanged(_ => gameController.IsFever)
            .Subscribe(value => playerAnim.SetBool(_Anim_Fever, value))
            .AddTo(this.gameObject);

        gameController.ObserveEveryValueChanged(_ => gameController.CanShield)
            .Subscribe(value => shieldObj.SetActive(value))
            .AddTo(this.gameObject);

        this.ObserveEveryValueChanged(_ => Obstacle.Obstacle.IsApply)
            .Subscribe(value => {
                playerAnim.SetBool(_Anim_Obstacle, value);
            })
            .AddTo(this.gameObject);

        // *이 스크립트가 먼저 들어가서 먼저 실행
        gameController.OnItemObstacle += () =>
        {
            if(Obstacle.Obstacle.IsApply)
            //if(anim.GetCurrentAnimatorStateInfo(0).IsName("Obstacle"))
            {
                playerAnim.SetTrigger(_Anim_Clear);
            }
        };
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!_GameManager.CanInput)
            return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            OnLeft();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            OnRight();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            OnFever();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            OnItem();
        }
    }

#endif

    public void OnLeft()
    {
        if (!_GameManager.CanInput)
            return;

        gameCharacter.DetectCharacter(ESide.Left);

        playerAnim.SetTrigger(_Anim_Left);
    }

    public void OnRight()
    {
        if (!_GameManager.CanInput)
            return;

        gameCharacter.DetectCharacter(ESide.Right);

        playerAnim.SetTrigger(_Anim_Right);
    }

    public void OnFever()
    {
        if (!_GameManager.CanInput)
            return;

        // Fever 사용이 가능한지, 충전 개수에 따라 달라짐
        _GameManager.GameController.UseFever();
    }

    public void OnItem()
    {
        if (!_GameManager.CanInput)
            return;

        // 아이템 보유했는지 확인
        _GameManager.GameController.UseItem();
    }

    IEnumerator CoAutoClick()
    {
        WaitForSeconds delay = new WaitForSeconds(0.05f);
        while (true)
        {
            yield return delay;
            if (Random.Range(0, 1f) >= 0.5f)
            {
                OnLeft();
            }
            else
            {
                OnRight();
            }
        }
    }
}
