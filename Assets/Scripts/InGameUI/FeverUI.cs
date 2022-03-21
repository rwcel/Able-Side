using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;

public class FeverUI : MonoBehaviour
{
    [Header("연출")]
    [SerializeField] GameObject[] onoffObjs;
    [SerializeField] Image sliderImg;
    [SerializeField] TextMeshProUGUI sliderText;

    [Header("피버 종류에 따라 바뀌는 것들")]
    [SerializeField] Image[] ribbonImages;
    [SerializeField] Sprite[] feverRibbon;
    [SerializeField] Transform feverVfxParent;

    [Header("아이템")]
    [SerializeField] Image iconImg;
    [SerializeField] ParticleSystem feverParticle;

    private FeverData feverData;


    private void Start()
    {
        GameController gameController = GameManager.Instance.GameController;

        gameController.OnFever += ActiveFever;

        gameController.ObserveEveryValueChanged(_ => gameController.IsFever)
            .Subscribe(value => OnOffFever(value, gameController.Fever))
            .AddTo(this.gameObject);

        gameController.ObserveEveryValueChanged(_ => gameController.Fever)
            .Subscribe(value => UpdateFever(value))
            .AddTo(this.gameObject);
    }

    private void OnOffFever(bool onoff, int value = 0)
    {
        foreach (var feverObject in onoffObjs)
        {
            feverObject.SetActive(onoff);
        }

        int arr = 0;
        foreach (Transform child in feverVfxParent)
        {
            Debug.Log(arr + "," + value);
            child.gameObject.SetActive(arr++ < value);
        }

        if (onoff)
        {
            // *찰나의 순간이기떄문에 오브젝트 켜주기
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
        // UI로는 먼저 꺼주기
        if (data.type != EFever.StartFever)      
        {
            UpdateFever(0);
        }
    }

    /// <summary>
    /// 바깥쪽에서 꺼주는 처리
    /// </summary>
    IEnumerator CoFeverTime()
    {
        float time = feverData.applyTime;
        float maxTime = feverData.applyTime;
        while(true)
        {
            time -= Time.deltaTime;
            yield return null;
            sliderText.text = $"{Mathf.CeilToInt(time)}s";
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
        OnOffFever(false);

        // Slider 초기화
    }
}
