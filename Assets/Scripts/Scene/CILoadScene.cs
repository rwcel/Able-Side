using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CILoadScene : MonoBehaviour
{
    FadeController mcs_fader;

    private void Awake()
    {
        mcs_fader = GetComponent<FadeController>();
    }

    // CI ����� ������ �ִϸ��̼ǿ��� ȣ��. 
    public void LoadMyIntroScene()
    {
        //GameSceneManager.Instance.SceneChange(GameSceneManager.EScene.InGame);
        GameSceneManager.Instance.SceneChange(GameSceneManager.EScene.Intro);
    }
}
