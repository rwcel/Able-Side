using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;

public class InGameUI : MonoBehaviour
{
    #region ���� ����

    [Header("========== Texts ==========")]
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI diaText;

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
    [SerializeField] AnimEvent readyAnimEvent;

    [Header("========== Item ==========")]
    [SerializeField] Sprite itemNullSprite;
    [SerializeField] ParticleSystem itemParticle;

    [Header("========== Slot ==========")]
    [SerializeField] Sprite normalSlot;
    [SerializeField] Sprite bonusSlot;

    [SerializeField] Image[] leftSlotImgs;
    [SerializeField] Image[] rightSlotImgs;
    [SerializeField] Image[] leftSideImgs;
    [SerializeField] Image[] rightSideImgs;
    [SerializeField] Image[] itemImages;

    [Header("========== Time ==========")]
    [SerializeField] Image timeSliderImg;
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] GameObject normalTimer;
    [SerializeField] Color normalColor;
    [SerializeField] Color invincibleColor;

    [Header("========== Clean ==========")]
    [SerializeField] GameObject cleanObj;


    protected List<Sprite> leftSide = new List<Sprite>();
    protected List<Sprite> rightSide = new List<Sprite>();

    protected List<GameObject> leftSlotObjs;
    protected List<GameObject> rightSlotObjs;

    private float maxTime;        // ������
    private int itemCount;          // ������ ���� ������

    public static readonly int _Anim_Combo = Animator.StringToHash("Combo");

    #endregion ���� ����

    public void OnStart()
    {
        InitSet();

        AddListeners();

        AddActions();

        AddObserves();
    }

    private void InitSet()
    {
        leftSlotObjs = new List<GameObject>();
        rightSlotObjs = new List<GameObject>();

        foreach (var leftSlotImg in leftSlotImgs)
        {
            leftSlotObjs.Add(leftSlotImg.gameObject);
        }
        foreach (var rightSlotImg in rightSlotImgs)
        {
            rightSlotObjs.Add(rightSlotImg.gameObject);
        }

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
                    --GameManager.Instance.InputValue;
                    Time.timeScale = 0;
                    AudioManager.Instance.PauseBGM(true);
                }
                , () =>
                {
                    ++GameManager.Instance.InputValue;
                    Time.timeScale = 1;
                    AudioManager.Instance.PauseBGM(false);
                });
        });
    }

    private void AddActions()
    {
        GameManager.Instance.OnGameStart += (value) =>
        {
            if (value == false)
            {
                ClearData();
            }
            else
            {
                readyObj.SetActive(true);
            }
        };

        readyAnimEvent.SetAnimEvent(() => 
        {
            BackEndServerManager.Instance.GameStartTime = System.DateTime.Now;
            GameManager.Instance.InputValue = 1;
            GameManager.Instance.GameController.StartTime();
            readyObj.SetActive(false);
        });
    }

    void AddObserves()
    {
        GameController gameController = GameManager.Instance.GameController;

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

        // TimeText �����ϱ�
        gameController.ObserveEveryValueChanged(_ => gameController.IsFever)
            .Subscribe(value => OnOffTimer(value))
            .AddTo(this.gameObject);

        gameController.ObserveEveryValueChanged(_ => gameController.Bomb)
            .Subscribe(value => UpdateBomb(value))
            .AddTo(this.gameObject);

        //bombSlider.maxValue = Values.MaxBombCount - 1;          // �� �� ���� �����ֱ�

        gameController.ObserveEveryValueChanged(_ => gameController.Items.Count)
            //.Skip(System.TimeSpan.Zero)
            .Subscribe(_ => UpdateItems(gameController.Items))
            .AddTo(this.gameObject);

        gameController.ObserveEveryValueChanged(_ => gameController.AddDia)
            .Subscribe(value => diaText.text = value.CommaThousands())
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
        foreach (var leftSlot in leftSlotObjs)
        {
            leftSlot.SetActive(false);
        }
        foreach (var rightSlot in rightSlotObjs)
        {
            rightSlot.SetActive(false);
        }

        foreach (var leftSlotImg in leftSlotImgs)
        {
            leftSlotImg.sprite = normalSlot;
        }
        foreach (var rightSlotImg in rightSlotImgs)
        {
            rightSlotImg.sprite = normalSlot;
        }

        Blur(false);

        feverUI.ClearData();

        UpdateTimer(false);

        itemCount = 0;
    }

    /// <summary>
    /// ���� ������ �̹�����
    /// 
    /// **Jumble ���� ������. sprite Out of bound array
    /// </summary>
    public void SetSideImage(List<Sprite> leftSide, List<Sprite> rightSide)
    {
        this.leftSide = leftSide;
        this.rightSide = rightSide;

        for (int i = 0, length = leftSide.Count; i < length; i++)
        {
            leftSideImgs[i].sprite = leftSide[i];
        }
        for (int i = 0, length = rightSide.Count; i < length; i++)
        {
            rightSideImgs[i].sprite = rightSide[i];
        }

        // �⺻ ����
        OpenImage(ESide.Left, 0);
        OpenImage(ESide.Right, 0);
    }

    public void OpenImage(ESide side, int arrayNum)
    {
        // Debug.Log($"{side}, {arrayNum}");
        if (side == ESide.Left)
        {
            leftSlotObjs[arrayNum].SetActive(true);
        }
        else
        {
            rightSlotObjs[arrayNum].SetActive(true);
        }
    }

    #region UpdateDatas

    public void UpdateScore(int value)
    {
        // 3�ڸ� ����
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
        //timeSlider.maxValue = value;
        maxTime = value;
        UpdateTime(GameManager.Instance.GameController.Time);
    }

    public void UpdateTime(float value)
    {
        timeSliderImg.fillAmount = value / maxTime;
        //timeSlider.value = value;
        timeText.text = ((int)value).ToString();
    }

    public void UpdateBomb(float value)
    {
        bombSliderImg.fillAmount = value / (Values.MaxBombCount - 1);
        // bombSlider.value = value;

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

        // 0�� ������ �����ֱ�
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

        // **������������ ����?
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

    public void BonusCharImg(bool isBonus, ESide side, int arrayNum)
    {
        if (side == ESide.Left)
        {
            leftSlotImgs[arrayNum].sprite = isBonus ? bonusSlot : normalSlot;
        }
        else
        {
            rightSlotImgs[arrayNum].sprite = isBonus ? bonusSlot : normalSlot;
        }

        // Debug.Log($"���ʽ� ���� : {isBonus} / {side} - {arrayNum}");
    }

    /// <summary>
    /// ��� �����ǰ�
    /// </summary>
    /// <param name="isBonus"></param>
    public void FeverBonus(bool isBonus)
    {
        for (int i = 0, length = leftSide.Count; i < length; i++)
        {
            //leftSideImgs[i].sprite = characterPairs[leftSide[i]];
        }
        for (int i = 0, length = rightSide.Count; i < length; i++)
        {
            //rightSideImgs[i].sprite = characterPairs[rightSide[i]];
        }
    }

    /// <summary>
    /// �̹��� ��ó��
    /// </summary>
    public void Blur(bool isBlur)
    {
        if(isBlur)
        {
            for (int i = 0, length = leftSide.Count; i < length; i++)
            {
                leftSideImgs[i].gameObject.SetActive(false);
            }
            for (int i = 0, length = rightSide.Count; i < length; i++)
            {
                rightSideImgs[i].gameObject.SetActive(false);
            }
        }
        else
        {
            for (int i = 0, length = leftSide.Count; i < length; i++)
            {
                leftSideImgs[i].gameObject.SetActive(true);
            }
            for (int i = 0, length = rightSide.Count; i < length; i++)
            {
                rightSideImgs[i].gameObject.SetActive(true);
            }
        }
    }

    public void ReverseButton(bool isReverse)
    {
        // IsReverse *= -1 ���ϳ�?
        //leftArrowRect.localScale *= Vector3.left;

        leftArrowRect.localScale = new Vector3(isReverse ? -1 : 1, 1, 1);
        rightArrowRect.localScale = new Vector3(isReverse ? 1 : -1, 1, 1);
    }
}
