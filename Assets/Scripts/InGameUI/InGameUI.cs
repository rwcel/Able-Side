using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;

public class InGameUI : MonoBehaviour
{
    #region 변수 선언

    [Header("========== Texts ==========")]
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI diaText;
    [SerializeField] GameObject diaParticleObj;

    [Header("========== Buttons ==========")]
    [SerializeField] Button pauseButton;

    [Header("========== Arrows ==========")]
    [SerializeField] RectTransform leftArrowRect;
    [SerializeField] RectTransform rightArrowRect;

    [Header("========== Bomb ==========")]
    [SerializeField] Image bombSliderImg;
    [SerializeField] ParticleSystem bombParticle;

    [Header("========== Combo ==========")]
    [SerializeField] Animator comboAnim;
    [SerializeField] TextMeshProUGUI comboText;

    [Header("========== Fever ==========")]
    [SerializeField] FeverUI feverUI;

    [Header("========== Ready ==========")]
    [SerializeField] GameObject readyObj;
    [SerializeField] Animator readyAnim;
    [SerializeField] AnimEvent readyAnimEvent;

    [Header("========== Item ==========")]
    [SerializeField] Sprite itemNullSprite;
    [SerializeField] ParticleSystem itemParticle;

    [Header("========== Slot ==========")]
    public SlotUI SlotUI;

    [SerializeField] Image[] itemImages;

    [Header("========== Time ==========")]
    [SerializeField] Image timeSliderImg;
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] GameObject normalTimer;
    [SerializeField] Color normalColor;
    [SerializeField] Color invincibleColor;

    [Header("========== Clean ==========")]
    [SerializeField] GameObject cleanObj;

    private float maxTime;        // 보관값
    private int itemCount;          // 아이템 갯수 보관값

    GameManager _GameManager;

    public static readonly int _Anim_Combo = Animator.StringToHash("Combo");

    #endregion 변수 선언

    public void OnStart()
    {
        InitSet();

        AddListeners();

        AddActions();

        AddObserves();
    }

    private void InitSet()
    {
        _GameManager = GameManager.Instance;

        ClearData();
    }

    private void AddListeners()
    {
        //leftButton.onClick.AddListener(OnClickLeft);
        //rightButton.onClick.AddListener(OnClickRight);

        pauseButton.onClick.AddListener(() => 
        {
            GamePopup.Instance.OpenPopup(EGamePopup.Pause
                , () =>
                {
                    --_GameManager.InputValue;
                    Time.timeScale = 0;
                    AudioManager.Instance.PauseBGM(true);
                }
                , () =>
                {
                    ++_GameManager.InputValue;
                    Time.timeScale = 1;
                    AudioManager.Instance.PauseBGM(false);
                });
        });
    }

    private void AddActions()
    {
        _GameManager.OnGameStart += (value) =>
        {
            if (value == false)
            {
                ClearData();
            }
            else
            {
                readyObj.SetActive(true);

                if (_GameManager.BestScore <= 0)         // **InGame에서 조절하는건 바람직한 방법이 아니긴함
                {
                    GamePopup.Instance.OpenPopup(EGamePopup.Tutorial);
                    PauseReadyAnim(true);
                }
                else
                {
                    AudioManager.Instance.PlaySFX(ESFX.ReadyGo);
                }
            }
        };

        readyAnimEvent.SetAnimEvent(() => 
        {
            BackEndServerManager.Instance.GameStartTime = System.DateTime.Now;
            readyObj.SetActive(false);

            _GameManager.InputValue = 1;
            _GameManager.GameController.StartTime();
        });
    }

    void AddObserves()
    {
        GameController gameController = _GameManager.GameController;

        gameController.ObserveEveryValueChanged(_ => gameController.Score)
            .Subscribe(value => UpdateScore(value))
            .AddTo(this.gameObject);

        //gameController.ScoreReactiveProperty
        //    .Subscribe(value => UpdateScore(value));

        gameController.ObserveEveryValueChanged(_ => gameController.Combo)
            .Subscribe(value => UpdateCombo(value))
            .AddTo(this.gameObject);

        gameController.ObserveEveryValueChanged(_ => gameController.Time)
            .Subscribe(value => UpdateTime(value))
            .AddTo(this.gameObject);

        // TimeText 변경하기
        gameController.ObserveEveryValueChanged(_ => gameController.IsFever)
            .Subscribe(value => OnOffTimer(value))
            .AddTo(this.gameObject);

        gameController.ObserveEveryValueChanged(_ => gameController.Bomb)
            .Subscribe(value => UpdateBomb(value))
            .AddTo(this.gameObject);

        //bombSlider.maxValue = Values.MaxBombCount - 1;          // 꽉 찬 것을 보여주기

        gameController.ObserveEveryValueChanged(_ => gameController.Items.Count)
            //.Skip(System.TimeSpan.Zero)
            .Subscribe(_ => UpdateItems(gameController.Items))
            .AddTo(this.gameObject);

        gameController.ObserveEveryValueChanged(_ => gameController.AddDia)
            .Subscribe(value => UpdateDia(value))
            .AddTo(this.gameObject);

        gameController.ObserveEveryValueChanged(_ => gameController.IsInvincible)
            .Subscribe(value => UpdateTimer(value))
            .AddTo(this.gameObject);

        gameController.OnItemObstacle += () =>
        {
            if (Obstacle.Obstacle.IsApply)
            {
                cleanObj.SetActive(true);
            }
        };

        gameController.ObserveEveryValueChanged(_ => gameController.IsReverse)
            .Skip(System.TimeSpan.Zero)
            .Subscribe(value => ReverseButton(value))
            .AddTo(this.gameObject);
    }

    private void ClearData()
    {
        feverUI.ClearData();
        SlotUI.ClearData();

        UpdateTimer(false);

        itemCount = 0;
    }

    #region UpdateDatas

    public void UpdateDia(int value)
    {
        diaText.text = value.CommaThousands();
        if (value != 0)
        {
            diaParticleObj.SetActive(true);
        }
    }

    public void UpdateScore(int value)
    {
        // 3자리 단위
        scoreText.text = value.CommaThousands();
    }

    public void UpdateCombo(int value)
    {
        if (value == 0)
            return;

        comboText.text = $"{value}";
        comboText.color = value.ComboColor();
        comboAnim.SetTrigger(_Anim_Combo);
    }

    public void SetMaxTime(float value)
    {
        maxTime = value;
        UpdateTime(_GameManager.GameController.Time);
    }

    public void UpdateTime(float value)
    {
        timeSliderImg.fillAmount = value / maxTime;
        timeText.text = ((int)value).ToString();
    }

    public void UpdateBomb(float value)
    {
        bombSliderImg.fillAmount = value / (Values.MaxBombCount - 1);

        if(bombSliderImg.fillAmount == 0)
            bombParticle.Play();
    }

    public void UpdateItems(Queue<InGameItemData> items)
    {
        int array = 0;
        foreach (var item in items)
        {
            itemImages[array].sprite = item.sprite;
            itemImages[array].gameObject.SetActive(true);

            // Debug.Log("Item : " + item.type);

            array++;
        }

        // 0은 무조건 켜져있기
        if (array == 0)
        {
            ++array;
        }

        for (int i = array, length = itemImages.Length; i < length; i++)
        {
            itemImages[i].gameObject.SetActive(false);
        }

        if(items.Count == 0)
        {
            itemImages[0].sprite = itemNullSprite;
        }

        if(items.Count > itemCount)
            itemParticle.Play();

        itemCount = items.Count;
    }

    private void OnOffTimer(bool isFever)
    {
        normalTimer.SetActive(!isFever);
    }

    private void UpdateTimer(bool isInvincible)
    {
        timeSliderImg.color = isInvincible ? invincibleColor : normalColor;
        if (isInvincible)
            timeText.text = "MAX";
    }

    #endregion

    // *IsReverse *= -1 불가능
    public void ReverseButton(bool isReverse)
    {
        leftArrowRect.localScale = new Vector3(isReverse ? -1 : 1, 1, 1);
        rightArrowRect.localScale = new Vector3(isReverse ? 1 : -1, 1, 1);
    }

    public void PauseReadyAnim(bool isPause)
    {
        readyAnim.speed = isPause ? 0 : 1;
        if(!isPause)
        {
            AudioManager.Instance.PlaySFX(ESFX.ReadyGo);
        }
    }
}
