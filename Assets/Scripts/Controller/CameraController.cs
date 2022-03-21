using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CameraController : MonoBehaviour
{
    DOTweenAnimation tweenAnimation;

    private void Awake()
    {
        tweenAnimation = GetComponent<DOTweenAnimation>();
    }

    private void Start()
    {
        GameManager.Instance.GameController.OnCorrect += (value) => { if (!value) CameraShake("InCorrectShake"); };
        GameManager.Instance.GameCharacter.OnStartBomb += () => CameraShake("BombShake");
    }

    void CameraShake(string id)
    {
        tweenAnimation.DOPlayById(id);
    }
}
