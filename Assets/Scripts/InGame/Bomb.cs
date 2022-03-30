using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class Bomb : MonoBehaviour
{
    [SerializeField] GameObject normalBomb;
    [SerializeField] GameObject enhanceBomb;

    private BombController normalController;
    private BombController enhanceController;

    private void Awake()
    {
        normalController = normalBomb.GetComponent<BombController>();
        enhanceController = enhanceBomb.GetComponent<BombController>();

        // Debug.Log($"{normalController.gameObject.name} {enhanceController.gameObject.name}");
    }

    private void Start()
    {
        var gameController = GameManager.Instance.GameController;
        gameController.ObserveEveryValueChanged(_ => gameController.BombCharacter)
            .Subscribe(value => 
            {
                if(value == Values.DeleteBombCharacters)
                {
                    normalBomb.SetActive(true);
                    enhanceBomb.SetActive(false);
                }
                else
                {
                    normalBomb.SetActive(false);
                    enhanceBomb.SetActive(true);
                }
            })
            .AddTo(this.gameObject);

        GameManager.Instance.OnGameStart += (value) =>
        {
            if (value == false)
            {
                ClearData();
            }
        };
    }

    private void ClearData()
    {
        PoolingManager.Instance.Enqueue(gameObject);
    }

    public CharacterController GetController()
    {
        return normalBomb.activeSelf ? normalController : enhanceController;
    }

    public void SetSortingGroup(int layer = -1)
    {
        normalController.BombSortingGroup(layer);
        enhanceController.BombSortingGroup(layer);
    }

}
