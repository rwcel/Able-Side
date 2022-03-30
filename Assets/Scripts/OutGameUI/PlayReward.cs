using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;

public class PlayReward : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] Button button;
    [SerializeField] Image boxImage;
    [SerializeField] TextMeshProUGUI stackText;
    [SerializeField] TextMeshProUGUI barText;
    [SerializeField] Image countImageBar;

    GameManager _GameManager;

    Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        _GameManager = GameManager.Instance;

        button.onClick.AddListener(OnPlayReward);

        UpdateLanguage();

        _GameManager.ObserveEveryValueChanged(_ => _GameManager.PlayRewardCount)
        .Subscribe(value =>UpdateData(value))
        .AddTo(this.gameObject);
    }

    void UpdateData(int value)
    {
        stackText.text = $"{value / 5}";
        barText.text = $"{value % 5} / {Values.PlayRewardCount}";
        //playRewardCountBar.fillAmount = ((value-1) % 5 + 1) / Values.PlayRewardCount ;
        countImageBar.fillAmount = (float)(value % 5) / Values.PlayRewardCount;
        if(value >= Values.PlayRewardCount)
        {
            boxImage.color = Color.white;
            anim.enabled = true;
        }
        else
        {
            boxImage.color = Color.gray;
            anim.enabled = false;
        }
    }

    public void OnPlayReward()
    {
        if (_GameManager.PlayRewardCount < 5)
        {
            AudioManager.Instance.PlaySFX(ESFX.Touch);
            // SystemPopupUI.Instance.OpenNoneTouch(4);
            return;
        }

        BackEndServerManager.Instance.Gacha(EGacha.PlayReward);
    }

    public void UpdateLanguage()
    {
        titleText.text = string.Format(150.Localization(), Values.PlayRewardScore.NumberFormat());
    }
}
