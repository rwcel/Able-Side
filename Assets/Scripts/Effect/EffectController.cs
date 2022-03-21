using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[RequireComponent(typeof(ParticleSystem))]
public class EffectController : MonoBehaviour
{
    [SerializeField] bool isPooling;

    [SerializeField] ParticleSystem mainParticle;
    WaitForSeconds waitEffect;

    private void Awake()
    {
        // mainParticle = GetComponent<ParticleSystem>();

        waitEffect = new WaitForSeconds(mainParticle.main.duration);

        if (!isPooling)
            gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        StartCoroutine(nameof(CoEffect));
    }

    IEnumerator CoEffect()
    {
        yield return waitEffect;

        if(isPooling)
        {
            PoolingManager.Instance.Enqueue(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
