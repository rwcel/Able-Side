using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;

// **받았을 때 초기화 필요. 
public class TimeReward : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] GameObject particleObj;

    GameManager _GameManager;

    Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();

        _GameManager = GameManager.Instance;
    }

    private void Start()
    {
        button.onClick.AddListener(OnTimeReward);

        this.ObserveEveryValueChanged(_ => _GameManager.TimeRewardTime)
            .Subscribe(_ => UpdateData())
            .AddTo(this.gameObject);
    }

    public void UpdateData()
    {
        if (_GameManager == null)
        {
            _GameManager = GameManager.Instance;
        }

        //timeText.text = (_GameManager.TimeRewardTime - System.DateTime.Now).MinRemainTime();
        if (System.DateTime.Now < _GameManager.TimeRewardTime)
        {   // 아직 못받음. 1분 이상을 유지해야함
            timeText.text = (_GameManager.TimeRewardTime.AddMinutes(1) - System.DateTime.Now).ToString(@"hh\:mm");
        }
        else
        {   // 받을 수 있음
            timeText.text = 151.Localization();
        }
        OnOff(timeText.text == 151.Localization());
    }

    void OnOff(bool value)
    {
        anim.enabled = value;
        particleObj.SetActive(value);
    }

    public void OnTimeReward()
    {
        //var time = _GameManager.TimeRewardTime - System.DateTime.Now;
        //Debug.Log($"남은시간 : {time}");

        GamePopup.Instance.OpenPopup(EGamePopup.TimeReward, null, UpdateData);
    }
}
