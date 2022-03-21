//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Obstacle;
//using UnityEngine.UI;
//using UniRx;

//public class ObstacleController : MonoBehaviour
//{
//    [SerializeField] Reverse reverse;
//    [SerializeField] Blur blur;
//    [SerializeField] Jumble jumble;

//    [SerializeField] GameObject[] eventObjs;
//    [SerializeField] ParticleSystem[] applyParticles;

//    protected EObstacle obstacleType;
//    public EObstacle Type => obstacleType;

//    protected bool isApply;
//    public bool IsApply => IsApply;

//    protected GameManager _GameManager;
//    protected GameController _GameController;

//    public System.Action<EObstacle> OnObstacle;

//    private void Start()
//    {
//        _GameManager = GameManager.Instance;

//        _GameManager.OnGameStart += (value) =>
//        {
//            if (value)
//            {
//                InitSet();
//            }
//            else
//            {
//                ClearData();
//            }
//        };
//    }

//    void InitSet()
//    {
//        //reverse.SetEventObjs(false);
//        //blur.SetEventObjs(false);
//        //jumble.SetEventObjs(false);

//        // 일정 점수 넘기는 지 확인
//    }

//    void ClearData()
//    {
//        isApply = false;
//    }

//    private void UpdateObstacle()
//    {
//        _GameController.OnCorrect += CanObstacle;
//    }

//    protected void CanObstacle(bool isCorrect)
//    {
//        // *피버 상태일때도 안걸리도록 
//        if (!isCorrect || isApply || _GameController.IsFever)
//            return;

//        CheckObstacle();
//    }

//    private void CheckObstacle()
//    {

//        //reverse.ClearCount();
//        //blur.ClearCount();
//        //jumble.ClearCount();
//    }
//}
