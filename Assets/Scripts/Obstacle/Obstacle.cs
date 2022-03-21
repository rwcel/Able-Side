using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;


namespace Obstacle
{
    public class Obstacle : MonoBehaviour
    {
        [SerializeField] AnimEvent animEvent;
        protected List<GameObject> eventObjs;
        //[SerializeField] Sprite textSprite;
        //[SerializeField] UnityEngine.UI.Image textImage;
        [SerializeField] ParticleSystem[] applyParticles;
        //[SerializeField] protected Animation eventAnim;

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

            eventObjs = new List<GameObject>();
            foreach (Transform child in transform)
            {
                eventObjs.Add(child.gameObject);
                //child.gameObject.SetActive(false);
            }
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

        /// <summary>
        /// ������
        /// ���� : BeginApply(true) -> EndApply(false) -> Apply() -> BeginApply(false) -> EndApply(true)
        /// ���� : BeginApply -> Apply -> EndApply
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator CoApply() { yield return null; }

        public virtual void BeginApply()
        {
            // Debug.Log("Begin Apply : " + _GameManager.InputValue);

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
            // isApply = true;
            foreach (var applyParticle in applyParticles)
            {
                applyParticle.Play();
            }

            ++_GameManager.InputValue;
        }

        // **���� ����
        public virtual void EndApply()
        {
            // Debug.Log("End Apply : " + _GameManager.InputValue);

            StopCoroutine(nameof(CoApply));
            isApply = false;
            obstacleType = EObstacle.None;
            _GameController.OnItemObstacle -= ItemEndApply;

            foreach (var eventObj in eventObjs)
            {
                eventObj.SetActive(false);
            }

            // ++_GameManager.InputValue;
        }

        public virtual void ItemEndApply()
        {
            // --_GameManager.InputValue;
            EndApply();
        }


        //public virtual void BeginApply(bool isObstacleStart)
        //{
        //    Debug.Log("Begin Apply : " + _GameManager.InputValue);

        //    if(isObstacleStart)
        //    {
        //        isApply = true;
        //        _GameController.OnItemObstacle += ItemEndApply;
        //    }
        //    foreach (var eventObj in eventObjs)
        //    {
        //        eventObj.SetActive(true);
        //    }

        //    // ���� ����
        //    --_GameManager.InputValue;

        //    // textImage.sprite = textSprite;

        //    animEvent.SetAnimEvent(Apply);
        //}

        //// ��� �ð����� ������ �ʱ�
        //public virtual void Apply() 
        //{
        //    // isApply = true;
        //    foreach (var applyParticle in applyParticles)
        //    {
        //        applyParticle.Play();
        //    }
        //}

        //// **���� ����
        //public virtual void EndApply(bool isObstacleEnd)
        //{
        //    Debug.Log("End Apply : " + _GameManager.InputValue);

        //    if (isObstacleEnd)
        //    {
        //        StopCoroutine(nameof(CoApply));
        //        isApply = false;
        //        obstacleType = EObstacle.None;
        //        _GameController.OnItemObstacle -= ItemEndApply;

        //        foreach (var eventObj in eventObjs)
        //        {
        //            eventObj.SetActive(false);
        //        }
        //    }
        //    ++_GameManager.InputValue;
        //}

        //public virtual void ItemEndApply()
        //{
        //    StopCoroutine(nameof(CoApply));
        //    isApply = false;
        //    obstacleType = EObstacle.None;
        //    _GameController.OnItemObstacle -= ItemEndApply;
        //    foreach (var eventObj in eventObjs)
        //    {
        //        eventObj.SetActive(false);
        //    }
        //}


    }
}
