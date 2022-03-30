using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;


namespace Obstacle
{
    public class Obstacle : MonoBehaviour
    {
        [SerializeField] AnimEvent animEvent;
        [SerializeField] List<GameObject> eventObjs;            // *Hue ����
        //[SerializeField] Sprite textSprite;
        //[SerializeField] UnityEngine.UI.Image textImage;
        [SerializeField] ParticleSystem[] applyParticles;
        //[SerializeField] protected Animation eventAnim;
        [SerializeField] UnityEngine.UI.Image timeImage;

        [HideInInspector] public int score;     // ���� ����
        protected int arrayNum;
        protected int count;                        // Correct ī��Ʈ
        // *���ļ��� �ȵǸ� static ���
        protected static bool isApply;                  // �̹� �������� ��� ��� ����
        public static bool IsApply => isApply;

        protected static EObstacle obstacleType;
        public static EObstacle Type => obstacleType;

        protected GameManager _GameManager;
        protected GameController _GameController;
        protected System.IDisposable obstacleDis;

        protected WaitForSeconds pauseDelay;            // ����ð�
        protected float applyTime;            // ����ð�

        /// <summary>
        /// ** InGameUI�� ���������� OnGameStart�� ���� ����Ǿ� InitSet�� ���� �ȵ�
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

        // **Query�� ��� Dispose�ϰ� null�� ����µ� performance �� ������ ����
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

            // *���� ��쿡�� �۵�
            _GameController.OnCorrect -= CanObstacle;

            StopCoroutine(nameof(CoApply));
        }

        protected virtual void UpdateObstacle() 
        {
            // ���� AddListener
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

            // *�ǹ� �����϶��� �Ȱɸ����� 
            if (!isCorrect || isApply || _GameController.IsFever)
                return;

            CheckObstacle();
        }

        /// <summary>
        /// true�϶��� ȿ�� ����
        /// </summary>
        protected virtual void CheckObstacle()
        {
        }

        /// <summary>
        /// 100���� Ȯ��
        /// </summary>
        protected bool IsSuccessObstacle(float percent)
        {
            return Random.Range(0f, 100f) < percent;
        }

        //bool isStart;

        /// <summary>
        /// ���� : BeginApply -> Apply -> EndApply
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

            // ���� ����
            --_GameManager.InputValue;

            animEvent.SetAnimEvent(Apply);
        }

        // ��� �ð����� ������ �ʱ�
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
