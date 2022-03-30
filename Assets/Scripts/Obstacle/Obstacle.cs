using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;


namespace Obstacle
{
    public class Obstacle : MonoBehaviour
    {
        [SerializeField] AnimEvent animEvent;
        [SerializeField] List<GameObject> eventObjs;            // *Hue 포함
        //[SerializeField] Sprite textSprite;
        //[SerializeField] UnityEngine.UI.Image textImage;
        [SerializeField] ParticleSystem[] applyParticles;
        //[SerializeField] protected Animation eventAnim;
        [SerializeField] UnityEngine.UI.Image timeImage;

        [HideInInspector] public int score;     // 기준 점수
        protected int arrayNum;
        protected int count;                        // Correct 카운트
        // *겹쳐서도 안되면 static 사용
        protected static bool isApply;                  // 이미 적용중인 경우 계산 안함
        public static bool IsApply => isApply;

        protected static EObstacle obstacleType;
        public static EObstacle Type => obstacleType;

        protected GameManager _GameManager;
        protected GameController _GameController;
        protected System.IDisposable obstacleDis;

        protected WaitForSeconds pauseDelay;            // 연출시간
        protected float applyTime;            // 적용시간

        /// <summary>
        /// ** InGameUI가 꺼져있으면 OnGameStart가 먼저 실행되어 InitSet이 적용 안됨
        /// </summary>
        protected virtual void Start()
        {
            obstacleType = EObstacle.None;

            _GameManager = GameManager.Instance;
            _GameController = _GameManager.GameController;

            _GameManager.OnGameStart += (value) =>
            {
                if (value == true)
                {
                    InitSet();
                }
                else
                {
                    ClearData();
                }
            };
        }

        // **Query를 계속 Dispose하고 null로 만드는데 performance 상 문제가 없나
        protected virtual void InitSet()
        {
            obstacleDis = _GameController.ObserveEveryValueChanged(_ => _GameController.Score)
                .Skip(System.TimeSpan.Zero)
                .Where(value => value >= score)
                .Subscribe(_ => UpdateObstacle())
                .AddTo(this.gameObject);
        }

        protected void ClearData()
        {
            arrayNum = 0;
            isApply = false;

            foreach (var eventObj in eventObjs)
            {
                eventObj.SetActive(false);
            }

            if (obstacleDis != null)
            {
                obstacleDis.Dispose();
            }

            // *없는 경우에도 작동
            _GameController.OnCorrect -= CanObstacle;

            StopCoroutine(nameof(CoApply));
        }

        protected virtual void UpdateObstacle() 
        {
            // 최초 AddListener
            if (arrayNum == 0)
            {
                _GameController.OnCorrect += CanObstacle;
            }
        }

        protected void CanObstacle(bool isCorrect)
        {
            // Debug.Log(isApply + " : " + gameObject.name);
            if (isApply)
                count = 0;

            // *피버 상태일때도 안걸리도록 
            if (!isCorrect || isApply || _GameController.IsFever)
                return;

            CheckObstacle();
        }

        /// <summary>
        /// true일때만 효과 있음
        /// </summary>
        protected virtual void CheckObstacle()
        {
        }

        /// <summary>
        /// 100분위 확률
        /// </summary>
        protected bool IsSuccessObstacle(float percent)
        {
            return Random.Range(0f, 100f) < percent;
        }

        //bool isStart;

        /// <summary>
        /// 변경 : BeginApply -> Apply -> EndApply
        /// </summary>
        protected virtual IEnumerator CoApply() { yield return null; }

        public virtual void BeginApply()
        {
            // Debug.Log("Begin Apply : " + _GameManager.InputValue);
            //isStart = true;

            isApply = true;
            _GameController.OnItemObstacle += ItemEndApply;
            foreach (var eventObj in eventObjs)
            {
                eventObj.SetActive(true);
            }

            AudioManager.Instance.PlaySFX(ESFX.Obstacle);

            // 시작 연출
            --_GameManager.InputValue;

            animEvent.SetAnimEvent(Apply);
        }

        // 대기 시간에는 켜주지 않기
        public virtual void Apply()
        {
            StartCoroutine(nameof(CoTimeBar));

            // isApply = true;
            foreach (var applyParticle in applyParticles)
            {
                applyParticle.Play();
            }

            ++_GameManager.InputValue;

            //isStart = false;
        }

        public virtual void EndApply()
        {
            // Debug.Log("End Apply : " + _GameManager.InputValue);
            Debug.Log("End Apply");
            StopCoroutine(nameof(CoTimeBar));
            timeImage.fillAmount = 0f;

            StopCoroutine(nameof(CoApply));
            isApply = false;
            obstacleType = EObstacle.None;
            _GameController.OnItemObstacle -= ItemEndApply;

            foreach (var eventObj in eventObjs)
            {
                eventObj.SetActive(false);
            }

            // isStart = false;
            // ++_GameManager.InputValue;
        }

        public virtual void ItemEndApply()
        {
            //if(isStart)
            //{
            //    ++_GameManager.InputValue;
            //}
            EndApply();
        }

        IEnumerator CoTimeBar()
        {
            float fill = applyTime;
            timeImage.fillAmount = 1f;
            
            while (isApply)
            {
                yield return null;

                fill -= Time.deltaTime;
                timeImage.fillAmount = fill / applyTime;
            }
            timeImage.fillAmount = 0f;
        }
    }
}
