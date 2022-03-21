using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using DG.Tweening;
using TMPro;

public class ItemSelectUI : PopupUI
{
    private enum EItemState
    {
        Count,          // 1�̻� ����
        Empty,          // �� ��
        Select,          // ä����
    }

    [System.Serializable]
    private struct FItemUI
    {
        [HideInInspector] public ELobbyItem type;
        public Button button;
        public Image itemImage;
        public Image buttonImage;
        // public TextMeshProUGUI nameText;
        public TextMeshProUGUI countText;
        public Transform stateParent;
        [HideInInspector] public EItemState state;
        [HideInInspector] public GameObject[] stateObjs;
    }

    [Header("Item")]
    [SerializeField] FItemUI[] itemsUI;

    [Header("Buttons")] 
    [SerializeField] Button startButton;
    [SerializeField] Button cancelButton;

    [Header("Sprites")]
    [SerializeField] Sprite normalSprite;
    [SerializeField] Sprite selectSprite;

    private bool isBuy;

    protected override void Awake()
    {
        base.Awake();

        int stateCount = itemsUI[0].stateParent.childCount;
        for (int i = 0, length = itemsUI.Length; i < length; i++)
        {
            itemsUI[i].type = (ELobbyItem)i;

            itemsUI[i].stateObjs = new GameObject[stateCount];
            //new GameObject[Enum.GetValues(typeof(EItemState)).Length];

            for (int state = 0; state < stateCount; state++)
            {
                itemsUI[i].stateObjs[state] = itemsUI[i].stateParent.GetChild(state).gameObject;
            }
        }
    }

    // ������ ���� �ʱ�ȭ
    protected override void UpdateData()
    {
        base.UpdateData();

        //*foreach �ȵ�
        ClearItems();
    }

    private void Start()
    {
        AddListeners();

        AddActions();

        AddObservables();
    }

    void AddListeners()
    {
        startButton.onClick.AddListener(OnGameStart);
        cancelButton.onClick.AddListener(_GamePopup.ClosePopup);


        // **������ �̸� ���� : Language
        for (int i = 0, length = itemsUI.Length; i < length; i++)
        {
            // *i���� �ٷ� ������ ������ ���� ��ΰ� 6�� �ȴ�
            var lobbyItem = (ELobbyItem)i;

            //itemsUI[i].nameText.text = LevelData.Instance.LobbyItemDatas[i].type.ToString();
            itemsUI[i].itemImage.sprite = LevelData.Instance.LobbyItemDatas[i].sprite;

            itemsUI[i].button.onClick.AddListener(() => OnClickItem(lobbyItem));
        }
    }

    void AddActions()
    {
        _GameManager.OnBuyLobbyItem += OnBuyItem;
    }

    void AddObservables()
    {
        // �ش� ������ ������ �ٲ𶧸��� ����
        for (int i = 0, length = _GameManager.ItemsCount.Length; i < length; i++)
        {
            int num = i;
            _GameManager.ObserveEveryValueChanged(_ => _GameManager.ItemsCount[num])            // item.Value
                .Subscribe(value => UpdateItem(num, value))
                .AddTo(this.gameObject);

            this.ObserveEveryValueChanged(_ => itemsUI[num].state)            // item.Value
                .Subscribe(value => UpdateItemState(num, value))
                .AddTo(this.gameObject);
        }
    }

    /// <summary>
    /// select�� �ƴ� ���·� �ٲٱ�
    /// button clicked��?
    /// </summary>
    public void ClearItems()
    {
        for (int i = 0, length = itemsUI.Length; i < length; i++)
        {
            itemsUI[i].state = int.Parse(itemsUI[i].countText.text) <= 0 ? EItemState.Empty : EItemState.Count;
            itemsUI[i].buttonImage.sprite = normalSprite;
        }
    }

    private void OnBuyItem(ELobbyItem item)
    {
        // **state����� count������ ���ÿ� �Ͼ�� ����
        isBuy = true;
        itemsUI[(int)item].state = EItemState.Select;
    }

    /// <summary>
    /// ������ Ŭ�� -> ���� ����
    /// </summary>
    private void OnClickItem(ELobbyItem item)
    {
        var itemInfo = itemsUI[(int)item];

        if (itemInfo.state == EItemState.Empty)
        {
            _GameManager.SelectLobbyItem = item;
            _GamePopup.OpenPopup(EGamePopup.ItemBuy);

            return;
        }

        // *itemInfo�� ������ �ȵ� : ���� ���� ����
        if (itemInfo.state == EItemState.Select)
        {
            itemsUI[(int)item].state = EItemState.Count;
            itemsUI[(int)item].buttonImage.sprite = normalSprite;
        }
        else
        {
            itemsUI[(int)item].state = EItemState.Select;
            itemsUI[(int)item].buttonImage.sprite = selectSprite;
        }

        // itemInfo = itemsUI[(int)item];
        // itemInfo.button.targetGraphic.color = itemInfo.isClicked ? Color.gray : Color.white;     // Anim?

        // Debug.Log($"{itemUI.Length} / {(int)item} : {itemInfo.isClicked}");
    }

    /// <summary>
    /// ���� ����,
    /// ���õ� ������ ����
    /// </summary>
    public void OnGameStart()
    {
        if(!_GameManager.CanGameStart)
        {
            _GamePopup.OpenPopup(EGamePopup.TicketShop);
            return;
        }

        ApplyItems();
        ApplyBonus();

        _GamePopup.ClosePopup();
        _GameManager.GameStart();
    }

    private void ApplyItems()
    {
        var gameController = _GameManager.GameController;
        // ������ ����
        foreach (var itemUI in itemsUI)
        {
            if (itemUI.state == EItemState.Select)
            {
                --_GameManager.ItemsCount[(int)itemUI.type];

                switch (itemUI.type)
                {
                    case ELobbyItem.AddMaxTime:
                        gameController.Time += Values.AddMaxTime_Value;
                        break;
                    case ELobbyItem.AddBombPower:
                        gameController.BombCharacter += Values.AddBombPower_Value;
                        break;
                    case ELobbyItem.MaxItem:
                        for (int i = 0, length = Values.MaxInGameItemCount; i < length; i++)
                        {
                            gameController.AddItem(gameController.StartGacha());
                        }
                        break;
                    case ELobbyItem.SuperFeverStart:
                        gameController.IsStartFever = true;
                        break;
                    case ELobbyItem.AddScore:
                        gameController.ExtraScore += Values.AddScore_Value * 0.01f;
                        break;
                    case ELobbyItem.Shield:
                        //gameController.Shield += Values.Shield_Value;
                        gameController.IsShield = true;
                        break;
                    default:
                        Debug.LogError("Not Item!");
                        break;
                }
            }
        }
    }

    /// <summary>
    /// ������ �� ���������� ���ʽ� 1% �ֱ�
    /// 1) ������ ���� ������ üũ�� �� -> Only score ���̶�� �ʿ� ����
    ///   : List<bool>�� �ּ� all true��� ����
    /// 2) �ְ������θ� �� -> Only score ���� ��� ����
    /// </summary>
    private void ApplyBonus()
    {
        var profileDatas = LevelData.Instance.ProfileDatas;
        // Debug.Log($"������ �ְ����� : {_GameManager.BestScore} / {profileDatas[profileDatas.Length - 1].missionScore}");
        if (_GameManager.AccumulateScore >= profileDatas[profileDatas.Length - 1].missionScore)
        {
            _GameManager.GameController.ExtraScore += Values.AddScore_Value * 0.01f;
        }
        // Bonus_AllProfile
    }

    private void UpdateItem(int num, int value)
    {
        itemsUI[num].countText.text = value.ToString();

        // �������� ���� �������� ����
        if(!isBuy)
        {
            itemsUI[num].state = (value > 0) ? EItemState.Count : EItemState.Empty;
        }

        isBuy = false;
        //Debug.Log($"{num} - {value} - {itemsUI[num].state}");
    }

    private void UpdateItemState(int num, EItemState state)
    {
        // Debug.Log($"{num} - {state}");

        // itemsUI[num].state = (num > 0) ? EItemState.Count : EItemState.Empty;
        foreach (var item in itemsUI[num].stateObjs)
        {
            item.SetActive(false);
        }
        itemsUI[num].stateObjs[(int)state].SetActive(true);
    }

    #region ���

    // ������ �̵� ����
    // **Instantiate �ؼ� �̵�?
    //if (!gameManager.CanGameStart)
    //    return;

    //int num = gameManager.Ticket - 1;

    //Debug.Log($"{ticketObjs[num].GetComponent<RectTransform>().anchoredPosition} / " +
    //    $"{startButton.GetComponent<RectTransform>().anchoredPosition}" +
    //    $"{startButton.transform.localPosition}");

    //Sequence sequence = DOTween.Sequence();
    //sequence.Append(ticketObjs[num].GetComponent<RectTransform>().DOPivot(new Vector2(0.5f, 0f), 1f)
    //    .SetEase(Ease.OutQuart)
    //    .OnComplete(() =>
    //    {
    //        PopupUIManager.Instance.ClosePopup();
    //        gameManager.GameStart();
    //    }));
    //sequence.Join(ticketObjs[num].GetComponent<RectTransform>().DOAnchorPos
    //    (
    //        Vector2.zero
    //        , 1f
    //    ));

    #endregion
}