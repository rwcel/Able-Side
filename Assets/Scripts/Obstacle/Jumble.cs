using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Obstacle
{
    public class Jumble : Obstacle
    {
        private FJumbleInfo[] jumbleInfos;
        private FJumbleInfo curInfo;


        protected override void Start()
        {
            base.Start();

            jumbleInfos = Values.JumbleInfos;

            // 절반만 적용해서 중간에 값 변경
            pauseDelay = new WaitForSeconds(Values.JumblePauseTime * 0.5f);
        }

        protected override void InitSet()
        {
            score = jumbleInfos[arrayNum].score;

            base.InitSet();
        }

        protected override void UpdateObstacle()
        {
            base.UpdateObstacle();

            if (++arrayNum >= jumbleInfos.Length)
            {
                obstacleDis.Dispose();
                return;
            }

            score = jumbleInfos[arrayNum].score;
        }

        protected override void CheckObstacle()
        {
            base.CheckObstacle();

            curInfo = jumbleInfos[arrayNum - 1];

            //Debug.Log("Jumble Count : " + count);
            if (++count >= curInfo.count)
            {
                count = 0;
                if (IsSuccessObstacle(curInfo.percent))
                {
                    StartCoroutine(nameof(CoApply));
                }
            }
        }

        /// <summary>
        /// 연출 + 적용
        /// </summary>
        protected override IEnumerator CoApply()
        {
            BeginApply();
            yield return pauseDelay;

            // Apply();     -> AnimEvent

            yield return pauseDelay;
            EndApply();
        }

        public override void BeginApply()
        {
            base.BeginApply();

            obstacleType = EObstacle.Jumble;
        }

        public override void Apply() 
        {
            base.Apply();
            _GameManager.GameCharacter.Jumble();
        }
        public override void EndApply() => base.EndApply();
    }
}
