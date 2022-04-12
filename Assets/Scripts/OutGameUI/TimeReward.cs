using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;

// **�޾��� �� �ʱ�ȭ �ʿ�. 
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
        {   // ���� ������. 1�� �̻��� �����ؾ���
            timeText.text = (_GameManager.TimeRewardTime.AddMinutes(1) - System.DateTime.Now).ToString(@"hh\:mm");
        }
        else
        {   // ���� �� ����
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
        //Debug.Log($"�����ð� : {time}");

        GamePopup.Instance.OpenPopup(EGamePopup.TimeReward, null, UpdateData);
    }
}
