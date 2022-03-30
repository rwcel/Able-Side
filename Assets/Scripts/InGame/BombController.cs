using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombController : CharacterController
{
    Bomb bomb;          // Parent

    public override void DownMove(Vector3 movePos, float duration, Ease ease)
    {
        //base.DownMove(movePos, duration, ease);

        SetSortingGroup();

        this.movePos = movePos;
        this.duration = duration;

        transform.parent.DOMove(movePos, duration)
            .SetEase(ease)
            .OnComplete(() => PlayRun(false));
    }

    public override void OutMove(Vector3 movePos, float duration, Ease ease)
    {
        //base.OutMove(movePos, duration, ease);

        SetSortingGroup();
        PlayRun(true);

        this.movePos = movePos;
        this.duration = duration;

        transform.parent.DOMove(movePos, duration)
            .SetEase(ease)
            .OnComplete(() =>
            {
                PoolingManager.Instance.Enqueue(transform.parent.gameObject);
                //transform.localPosition = Vector3.zero;
                PlayRun(false);
            });
    }

    public override void SetSortingGroup(int layer = -1)
    {
        if(bomb == null)
        {
            bomb = transform.parent.GetComponent<Bomb>();
        }
        // base.SetSortingGroup(layer);
        bomb.SetSortingGroup(layer);
    }

    public void BombSortingGroup(int layer = -1)
    {
        if (layer == -1)
        {
            ++sortingGroup.sortingOrder;
            // PlayRun(true);
        }
        else
            sortingGroup.sortingOrder = layer;
    }
}
