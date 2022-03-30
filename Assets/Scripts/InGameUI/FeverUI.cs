using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;

public class FeverUI : MonoBehaviour
{
    [Header("����")]
    [SerializeField] GameObject[] onoffObjs;
    [SerializeField] Image sliderImg;
    [SerializeField] TextMeshProUGUI sliderText;

    [Header("�ǹ� ������ ���� �ٲ�� �͵�")]
    [SerializeField] Image[] ribbonImages;
    [SerializeField] Sprite[] feverRibbon;
    [SerializeField] Transform feverVfxParent;

    [Header("������")]
    [SerializeField] Image iconImg;
    [SerializeField] ParticleSystem feverParticle;

    private FeverData feverData;


    private void Start()
    {
        GameController gameController = GameManager.Instance.GameController;

        gameController.OnFever += ActiveFever;

        gameController.ObserveEveryValueChanged(_ => gameController.IsFever)
            .Subscribe(value => OnOffFever(value, gameController.IsStartFever, gameController.Fever))
            .AddTo(this.gameObject);

        gameController.ObserveEveryValueChanged(_ => gameController.Fever)
            .Subscribe(value => UpdateFever(value))
            .AddTo(this.gameObject);
    }

    private void OnOffFever(bool onoff, bool isStartFever, int value = 0)
    {
        foreach (var feverObject in onoffObjs)
        {
            feverObject.SetActive(onoff);
        }

        if (isStartFever)
        {
            value = 1;
        }

        int arr = 0;
        foreach (Transform child in feverVfxParent)
        {
            child.gameObject.SetActive(arr++ < value);
        }

        if (onoff)
        {
            // *������ �����̱⋚���� ������Ʈ ���ֱ�
            gameObject.SetActive(true);         
            StartCoroutine(nameof(CoFeverTime));

            if(value != 0)
            {
                foreach (var ribbon in ribbonImages)
                {
                    ribbon.sprite = feverRibbon[value - 1];
                }
            }
        }
        else if (gameObject.activeSelf)
        {
                StopCoroutine(nameof(CoFeverTime));
        }
    }

    private void ActiveFever(FeverData data)
    {
        feverData = data;
        // UI�δ� ���� ���ֱ�
        if (data.type != EFever.StartFever)      
        {
            UpdateFever(0);
        }
    }

    /// <summary>
    /// �ٱ��ʿ��� ���ִ� ó��
    /// </summary>
    IEnumerator CoFeverTime()
    {
        float time = feverData.applyTime;
        float maxTime = feverData.applyTime;
        while(true)
        {
            time -= Time.deltaTime;
            yield return null;
            sliderText.text = $"{Mathf.CeilToInt(time)}";
            sliderImg.fillAmount = time / maxTime;
            // Text?
        }
    }

    public void UpdateFever(int value)
    {
        iconImg.sprite = LevelData.Instance.FeverDatas[value].sprite;

        if (value != 0)
        {
            feverParticle.Play();
        }
    }

    public void ClearData()
    {
        OnOffFever(false, false);

        // Slider �ʱ�ȭ
    }
}
