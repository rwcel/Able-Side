using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class GameSceneManager : Singleton<GameSceneManager>
{
	public enum EScene
	{
		CI = 0,
		// 로그인
		Intro = 1,
		InGame = 2,
	}

	[SerializeField] UnityEngine.UI.Image background;

	private Sequence sequence;

	private System.Action loadAction;

	protected override void AwakeInstance()
	{
		var obj = FindObjectsOfType<GameSceneManager>();
		if (obj.Length == 1)
			DontDestroyOnLoad(gameObject);
		else
		{
			Destroy(gameObject);
		}
	}

	protected override void DestroyInstance() { }

	public void MoveNextScene()
	{
		background.DOColor(Color.black, 0.5f).OnComplete (() => 
		{
			StartCoroutine(LoadScene(SceneManager.GetActiveScene().buildIndex + 1));
		});
	}

	/// <summary>
	/// **에러 테스트 필요 : DontDestroyLoad
	/// </summary>
	public void ReloadScene()
	{
		background.DOColor(Color.black, 0.5f).OnComplete(() =>
		{
			StartCoroutine(LoadScene(SceneManager.GetActiveScene().buildIndex));
		});
	}

	public void Restart()
	{
		background.DOColor(Color.black, 0.5f).OnComplete(() =>
		{
			StartCoroutine(LoadScene(0));
		});
	}

	public void MovePrevScene()
	{
		background.DOColor(Color.black, 0.5f).OnComplete(() =>
		{
			StartCoroutine(LoadScene(SceneManager.GetActiveScene().buildIndex - 1));
		});
	}

	public void SceneChange(EScene scene, System.Action loadAction = null)
	{
		this.loadAction = loadAction;

		background.DOColor(Color.black, 0.5f).OnComplete(() =>
		{
			StartCoroutine(LoadScene(scene.ToString()));
		});
	}

	IEnumerator LoadScene(string nextScene)
	{
		// dotween FadeIn, FadeOut
		//yield return StartCoroutine(fadeController.FadeIn(0.3f));
		yield return null;

		AsyncOperation op = SceneManager.LoadSceneAsync(nextScene);
		op.allowSceneActivation = false;
		while (!op.isDone)
		{
			yield return null;
			if (op.progress < 0.9f)
			{
				//this.load.text = "Loading... " + Mathf.RoundToInt(op.progress * 100) + "%";
			}
			else
			{
				op.allowSceneActivation = true;
				if (loadAction != null)
					loadAction?.Invoke();
			}
		}

		background.DOColor(Color.clear, 0.3f);
	}

	IEnumerator LoadScene(int sceneNum)
	{
		// dotween FadeIn, FadeOut
		//yield return StartCoroutine(fadeController.FadeIn(0.3f));
		yield return null;

		AsyncOperation op = SceneManager.LoadSceneAsync(sceneNum, LoadSceneMode.Single);
		op.allowSceneActivation = false;
		while (!op.isDone)
		{
			yield return null;
			if (op.progress < 0.9f)
			{
				//this.load.text = "Loading... " + Mathf.RoundToInt(op.progress * 100) + "%";
			}
			else
			{
				op.allowSceneActivation = true;
			}
		}

		background.DOColor(Color.clear, 0.3f);
	}

#if UNITY_EDITOR
	///Custom Editor
	[MenuItem("SceneChange/CI")]
	private static void ToCIScene()
	{
		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		EditorSceneManager.OpenScene("Assets/Scenes/CI.unity");
	}

	[MenuItem("SceneChange/Intro")]
	private static void ToIntroScene()
	{
		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		EditorSceneManager.OpenScene("Assets/Scenes/Intro.unity");
	}

	[MenuItem("SceneChange/InGame")]
	private static void ToInGameScene()
	{
		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
	}
#endif

	public void Quit()
    {
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
	}
}