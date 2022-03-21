using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UniRx;

public class ResultUI : PopupUI
{
    [Header("Texts")]
    [SerializeField] TextMeshProUGUI nickText;
    [SerializeField] TextMeshProUGUI bestScoreText;
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI maxComboText;
    [SerializeField] TextMeshProUGUI bonusText;
    //[SerializeField] TextMeshProUGUI bestMaxComboText;
    [SerializeField] TextMeshProUGUI addDiaText;
    [SerializeField] GameObject updateObj;

    [Header("Buttons")]
    [SerializeField] Button reviveButton;       // �����û
    [SerializeField] Button rewardButton;       // �����û
    [SerializeField] Button lobbyButton;

    private bool useRevive;                      // �̾��ϱ�� 1���� ����

    private int diaValue = 0;
    private int scoreValue = 0;

    private void Start()
    {
        AddListeners();
        AddObservables();
    }

    private void AddListeners()
    {
        reviveButton.onClick.AddListener(OnRevive);
        rewardButton.onClick.AddListener(OnDoubleReward);
        lobbyButton.onClick.AddListener(() => OnLobby());
    }

    private void AddObservables()
    {
        _GameManager.ObserveEveryValueChanged(_ => _GameManager.BestScore)
            .Subscribe(value => UpdateBestScore(value))
            .AddTo(this.gameObject);
    }

    protected override void UpdateData()
    {
        if (_GameManager == null)
        {
            _GameManager = GameManager.Instance;
        }

        nickText.text = BackEndServerManager.Instance.NickName;

        var gameController = _GameManager.GameController;

        // *Controller���� �����ִ� ���̴ٸ�
        // **�߰� ���� Text �������..?
        scoreValue = (int)(gameController.Score * gameController.ExtraScore);

        scoreText.text = $"{scoreValue.CommaThousands()}";

        bonusText.text = $"{(gameController.ExtraScore - 1) * 100}%";               // 1.15 or 1 -> (N-1) * 100
        maxComboText.text = $"{gameController.MaxCombo.CommaThousands()}";

        //bestMaxComboText.text = $"Best MaxCombo : { Utils.ValueThousands(_GameManager.BestMaxCombo)}";

        //bestMaxComboText.color = (_GameManager.GameController.MaxCombo == _GameManager.BestMaxCombo) ?
        //    updateColor : Color.black;

        diaValue = _GameManager.GameController.AddDia;
        addDiaText.text = $"{diaValue}";

        reviveButton.interactable = !useRevive && GameManager.Instance.CanRevive;
        rewardButton.interactable = GameManager.Instance.CanDoubleReward;
    }

    void UpdateBestScore(int value)
    {
        bestScoreText.text = $"{value.CommaThousands()}";
        updateObj.SetActive(scoreValue == value);
    }

    void UpdateBestMaxCombo(int value)
    {

    }

    /// <summary>
    /// ���� ��û �� �÷��� ����
    /// </summary>
    void OnRevive()
    {
        if (useRevive)
            return;

        if (_GameManager.CanUseRevive)      // ���� ��
        {   
            Revive();
        }
        else if(_GameManager.ChargeRevive())
        {
            // ����
            UnityAdsManager.Instance.ShowRewardAD(Revive
                                                                      , _GameManager.Timer_Revive
                                                                      , EDailyGift.Revive);

            Debug.Log($"�̾��ϱ� ���� Ƚ�� :  {_GameManager.ReviveCount}");
        }
    }

    private void Revive()
    {
        _GamePopup.ClosePopupNotAction();

        // Time => Max
        _GameManager.GameController.Revive();

        useRevive = true;
    }

    void OnDoubleReward()
    {
        if (_GameManager.CanUseDoubleReward)
        {
            DoubleReward();
        }
        else if (_GameManager.ChargeDoubleReward())
        {
            // ����
            UnityAdsManager.Instance.ShowRewardAD(DoubleReward
                                                                      ,  _GameManager.Timer_DoubleReward
                                                                      , EDailyGift.DoubleReward);

            Debug.Log($"�ι躸�� ���� Ƚ�� :  {_GameManager.DoubleRewardCount}");
        }        
    }

    private void DoubleReward()
    {
        _GameManager.FreeDia += (diaValue * 2);

        OnLobby(false);
    }

    /// <summary>
    /// Double Reward�� ������ ���� ����
    /// </summary>
    void OnLobby(bool isAddValue = true)
    {
        // ������ ���� �÷��� �α� ������
        BackEndServerManager.Instance.GamePlayLog(scoreValue, diaValue, useRevive , !isAddValue);

        // Data�ʱ�ȭ
        useRevive = false;

        if(isAddValue)
        {
            _GameManager.FreeDia += diaValue;
        }

        _GamePopup.ClosePopup();
    }
}
