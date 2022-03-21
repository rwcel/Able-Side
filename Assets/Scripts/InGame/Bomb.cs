using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class Bomb : MonoBehaviour
{
    [SerializeField] GameObject normalBomb;
    [SerializeField] GameObject enhanceBomb;

    private CharacterController normalController;
    private CharacterController enhanceController;

    private void Awake()
    {
        normalController = normalBomb.GetComponent<CharacterController>();
        enhanceController = enhanceBomb.GetComponent<CharacterController>();

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

                    normalController.SetSortingGroup(enhanceController.SortOrder);
                }
                else
                {
                    // �̹� ��ȭ���¶�� sortOrder�� �ǵ帮�� �ʱ�     **�ٽ� �ǵ帮�� �ֻ������ �ö󰡰� ��
                    if (!enhanceBomb.activeSelf)        
                        enhanceController.SetSortingGroup(normalController.SortOrder);

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

}
