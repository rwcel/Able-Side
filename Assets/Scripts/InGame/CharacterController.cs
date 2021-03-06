using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;

public class CharacterController : MonoBehaviour
{
    public ECharacter Type;

    protected SortingGroup sortingGroup;
    public int SortOrder => sortingGroup.sortingOrder;
    protected Animator anim;

    protected static readonly int _Anim_Run = Animator.StringToHash("IsRun");
    protected static readonly int _Anim_Fail = Animator.StringToHash("Fail");

    protected Vector3 movePos;
    protected float duration;
    protected Ease ease;

    private void Awake()
    {
        sortingGroup = GetComponent<SortingGroup>();
        anim = GetComponent<Animator>();
    }

    public virtual void SetSortingGroup(int layer = -1)
    {
        if (layer == -1)
        {
            ++sortingGroup.sortingOrder;
        }
        else
        {
            sortingGroup.sortingOrder = layer;
        }
    }

    /// <summary>
    /// *Type더 많아지면 enum으로 조절 필요
    /// </summary>
    public void PlayRun(bool isRun)
    {
        anim.SetBool(_Anim_Run, isRun);
    }

    public void PlayFail()
    {
        anim.SetTrigger(_Anim_Fail);
    }


    public virtual void DownMove(Vector3 movePos, float duration, Ease ease)
    {
        SetSortingGroup();

        this.movePos = movePos;
        this.duration = duration;

        transform.DOMove(movePos, duration)
            .SetEase(ease)
            .OnComplete(() => PlayRun(false));
    }

    public virtual void OutMove(Vector3 movePos, float duration, Ease ease)
    {
        SetSortingGroup();
        PlayRun(true);

        this.movePos = movePos;
        this.duration = duration;

        transform.DOMove(movePos, duration)
            .SetEase(ease)
            .OnComplete(() =>
            {
                PoolingManager.Instance.Enqueue(gameObject);
                PlayRun(false);
            });
    }
}
