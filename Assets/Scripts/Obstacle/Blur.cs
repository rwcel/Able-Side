using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Obstacle
{
    public class Blur : Obstacle
    {
        private FBlurInfo[] blurInfos;
        private FBlurInfo curInfo;

        private WaitForSeconds resumeDelay;

        protected override void Start()
        {
            base.Start();

            blurInfos = Values.BlurInfos;

            pauseDelay = new WaitForSeconds(Values.BlurPauseTime);
            resumeDelay = new WaitForSeconds(Values.BlurPauseTime * 0.2f);
        }

        protected override void InitSet()
        {
            score = blurInfos[arrayNum].score;

            base.InitSet();
        }

        protected override void UpdateObstacle()
        {
            base.UpdateObstacle();

            if (++arrayNum >= blurInfos.Length)
            {
                obstacleDis.Dispose();
                return;
            }

            score = blurInfos[arrayNum].score;
        }

        protected override void CheckObstacle()
        {
            base.CheckObstacle();

            curInfo = blurInfos[arrayNum - 1];

            //Debug.Log("Blur Count : " + count);
            if (++count >= curInfo.count)
            {
                count = 0;
                if (IsSuccessObstacle(curInfo.percent))
                {
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

            obstacleType = EObstacle.Blur;
        }

        public override void Apply() 
        {
            base.Apply();

            // 블러처리
            GameUIManager.Instance.InGameSlotBlur(true);
        }

        public override void EndApply() 
        {
            base.EndApply();
            GameUIManager.Instance.InGameSlotBlur(false);
        }
    }
}

