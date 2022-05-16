using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class UIManager : MonoBehaviour
{
	#region Fields & Properties

	public static UIManager Instance;

	public TMP_Text _overheatedText, _deathText;
	public Slider _weaponTempSlider, _healthSlider;
	public GameObject _deathScreen;
	public TMP_Text _killsText, _deathsText;
	public GameObject _leaderboard;
	public LeaderboardPlayer _leaderboardPlayerDisplay;
	public GameObject _endScreen;
	public TMP_Text _timerText;
	public GameObject _optionsScreen;

	#endregion

	#region Getters


	#endregion

	#region Unity Methods

	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			//DontDestroyOnLoad(gameObject);
		}
		else if (Instance != this)
			Destroy(gameObject);
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			ShowHideOptions();
		}

		if(_optionsScreen.activeInHierarchy && Cursor.lockState != CursorLockMode.None)
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
	}
	#endregion

	#region Public Methods

	public void ShowHideOptions()
	{
		if (!_optionsScreen.activeInHierarchy)
			_optionsScreen.SetActive(true);
		else
			_optionsScreen.SetActive(false);
	}

	public void ReturnToMainMenu()
	{
		PhotonNetwork.AutomaticallySyncScene = false;
		PhotonNetwork.LeaveRoom();
	}

	public void QuitGame()
	{
		PhotonNetwork.LeaveRoom();
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
		Application.Quit();
	}
	#endregion

	#region Private Methods


	#endregion
}
