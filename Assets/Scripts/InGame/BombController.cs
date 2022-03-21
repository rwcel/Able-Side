using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombController : CharacterController
{
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
}
