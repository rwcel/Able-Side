using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlotUI : MonoBehaviour
{
    [SerializeField] Sprite normalSlot;
    [SerializeField] Sprite bonusSlot;

    [SerializeField] Image[] leftSlotImgs;
    [SerializeField] Image[] rightSlotImgs;
    [SerializeField] Image[] leftSideImgs;
    [SerializeField] Image[] rightSideImgs;

    // Obstacle도?
    [SerializeField] ParticleSystem[] leftBonusParticle;
    [SerializeField] ParticleSystem[] rightBonusParticle;


    protected List<Sprite> leftSide = new List<Sprite>();
    protected List<Sprite> rightSide = new List<Sprite>();

    protected List<GameObject> leftSlotObjs;
    protected List<GameObject> rightSlotObjs;

    private void Awake()
    {
        leftSlotObjs = new List<GameObject>(leftSlotImgs.Length);
        rightSlotObjs = new List<GameObject>(rightSideImgs.Length);

        foreach (var leftSlotImg in leftSlotImgs)
        {
            leftSlotObjs.Add(leftSlotImg.gameObject);
        }
        foreach (var rightSlotImg in rightSlotImgs)
        {
            rightSlotObjs.Add(rightSlotImg.gameObject);
        }
    }

    public void ClearData()
    {
        // Slider 초기화
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
    }

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

        // 기본 오픈
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

    /// <summary>
    /// 이미지 + 이펙트
    /// </summary>
    public void SetBonusChar(bool isBonus, ESide side, int arrayNum)
    {
        if (side == ESide.Left)
        {
            leftSlotImgs[arrayNum].sprite = isBonus ? bonusSlot : normalSlot;
        }
        else
        {
            rightSlotImgs[arrayNum].sprite = isBonus ? bonusSlot : normalSlot;
        }

        // Debug.Log($"보너스 여부 : {isBonus} / {side} - {arrayNum}");
    }

    public void MoveBonusChar(ESide side, int arrayNum)
    {
        // Debug.Log($"{side} / {arrayNum}");
        if (arrayNum == -1)
            return;

        if(side == ESide.Left)
        {
            leftBonusParticle[arrayNum].Play();
        }
        else
        {
            rightBonusParticle[arrayNum].Play();
        }
    }

    /// <summary>
    /// 모두 설정되게
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
    /// 이미지 블러처리
    /// </summary>
    public void Blur(bool isBlur)
    {
        for (int i = 0, length = leftSide.Count; i < length; i++)
        {
            leftSideImgs[i].gameObject.SetActive(!isBlur);
        }
        for (int i = 0, length = rightSide.Count; i < length; i++)
        {
            rightSideImgs[i].gameObject.SetActive(!isBlur);
        }
    }
}
