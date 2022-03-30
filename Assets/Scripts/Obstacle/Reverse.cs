using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using DG.Tweening;

namespace Obstacle
{
    public class Reverse : Obstacle
    {
        [Header("Reverse")]
        [SerializeField] DOTweenAnimation tweenAnimation;

        private FReverseInfo[] reverseInfos;
        private FReverseInfo curInfo;

        protected override void Start()
        {
            base.Start();

            reverseInfos = Values.ReverseInfos;

            pauseDelay = new WaitForSeconds(Values.ReversePauseTime);
        }

        protected override void InitSet()
        {
            score = reverseInfos[arrayNum].score;

            base.InitSet();
        }

        protected override void UpdateObstacle()
        {
            base.UpdateObstacle();

            // ���̻� ���� �ʱ�
            if(++arrayNum >= reverseInfos.Length)
            {
                obstacleDis.Dispose();
                return;
            }

            score = reverseInfos[arrayNum].score;
        }


        protected override void CheckObstacle()
        {
            base.CheckObstacle();

            curInfo = reverseInfos[arrayNum - 1];

            //Debug.Log("Reverse Count : " + count);
            // �������ΰ��� �̹� ������ ��
            if (++count >= curInfo.count)
            {
                count = 0;
                if (IsSuccessObstacle(curInfo.percent))
                {
                    tweenAnimation.delay = curInfo.time;        // ������ ����
                    Debug.Log("������ : " + tweenAnimation.delay);

                    // obstacle.time �ð� ���� ����
                    StartCoroutine(nameof(CoApply));
                }
            }
        }

        protected override IEnumerator CoApply() 
        {
            applyTime = curInfo.time;
            WaitForSeconds applyDelay = new WaitForSeconds(curInfo.time);

            BeginApply();
            yield return pauseDelay;

            // Apply();     -> AnimEvent
            yield return applyDelay;

            EndApply();
        }

        public override void BeginApply()
        {
            base.BeginApply();
            obstacleType = EObstacle.Reverse;
        }

        public override void Apply() 
        {
            base.Apply();

            // ������ ����
            _GameController.IsReverse = true;

            // *��ư�� IsReverse ������ �ɾ����
        }
        public override void EndApply() 
        {
            base.EndApply();
            _GameController.IsReverse = false;
        }

        public override void ItemEndApply()
        {
            base.ItemEndApply();
            _GameController.IsReverse = false;
        }
    }
}
