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

    protected override void Start()
    {
        base.Start();

        AddListeners();
        AddObservables();
    }

    private void AddListeners()
    {
        reviveButton.onClick.AddListener(OnRevive);
        rewardButton.onClick.AddListener(OnDoubleReward);
        lobbyButton.onClick.AddListener(() => OnLobby());

        _GameManager.OnGameStart += (value) =>
        {
            if (!value)
            {   // Pause�ؼ� ���� ��� �ʱ�ȭ �������
                useRevive = false;
            }
        };
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

        // *Controller�� �����ִ� ���̴ٸ�
        scoreValue = (int)(gameController.Score * gameController.ExtraScore);

        scoreText.text = $"{scoreValue.CommaThousands()}";

        bonusText.text = $"{(gameController.ExtraScore - 1) * 100}%";               // 1.15 or 1 -> (N-1) * 100
        maxComboText.text = $"{gameController.MaxCombo.CommaThousands()}";

        diaValue = _GameManager.GameController.AddDia;
        addDiaText.text = $"{diaValue}";

        reviveButton.interactable = !useRevive;             //  && GameManager.Instance.CanRevive;
        rewardButton.interactable = diaValue > 0;        //&& GameManager.Instance.CanDoubleReward;       // 0���� false

        // **������ ���� �߰��ϱ⶧���� �־��ֱ�
        // animEvent.SetAnimEvent(() => AudioManager.Instance.PlaySFX(ESFX.Result));
    }

    void UpdateBestScore(int value)
    {
        bestScoreText.text = $"{value.CommaThousands()}";
        updateObj.SetActive(scoreValue == value);
    }

    /// <summary>
    /// ���� ��û �� �÷��� ����
    /// </summary>
    void OnRevive()
    {
        AudioManager.Instance.PlaySFX(ESFX.Touch);
        if (useRevive)
            return;

        _GameManager.UseDailyGift(EDailyGift.Revive, Revive);
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
        AudioManager.Instance.PlaySFX(ESFX.Touch);
        _GameManager.UseDailyGift(EDailyGift.DoubleReward, DoubleReward);     
    }

    /// <summary>
    /// 2�� �����϶��� UI ����ֱ�
    /// </summary>
    private void DoubleReward()
    {
        BackEndServerManager.Instance.AddItem(EItem.FreeDia, diaValue * 2);
        // _GameManager.FreeDia += ;

        Dispatcher.Instance.Invoke(() => OnLobby(false));
    }

    /// <summary>
    /// Double Reward�� ������ ���� ����
    /// </summary>
    void OnLobby(bool normalReward = true)
    {
        // ������ ���� �÷��� �α� ������
        BackEndServerManager.Instance.GamePlayLog(scoreValue, diaValue, useRevive, !normalReward);

        // Data�ʱ�ȭ
        // useRevive = false;

        // **���̾� �޴� ��� ���� ���� ������ �ȳ����⶧���� ���� �ʿ�
        _GamePopup.ClosePopup();

        if (normalReward)
        {
            AudioManager.Instance.PlaySFX(ESFX.Touch);
            _GameManager.FreeDia += diaValue;
            UnityAdsManager.Instance.ShowInterstitialAD();

            GameUIManager.Instance.MoveDock(EDock.Home);
        }
        else
        {
            GamePopup.Instance.OpenPopup(EGamePopup.Reward, null,
                () => GameUIManager.Instance.MoveDock(EDock.Home));
        }
    }
}
