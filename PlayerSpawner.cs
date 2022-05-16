using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
	#region Fields & Properties

	public static PlayerSpawner Instance;

	[SerializeField] GameObject _playerPrefab, _deathFXPrefab;
	[SerializeField] float _respawnDelayTime = 5f;

	GameObject _player;

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

	void Start() 
	{
		if (PhotonNetwork.IsConnected)
			SpawnPlayer();
	}
	#endregion

	#region Public Methods

	public void SpawnPlayer()
	{
		Transform spawnPoint = SpawnManager.Instance.GetSpawnPoint();

		_player = PhotonNetwork.Instantiate(_playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
	}

	public void Die(string damager)
	{
		UIManager.Instance._deathText.text = $"You were killed by {damager}";
		MatchManager.Instance.UpdateStatSend(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

		if(_player != null)
			StartCoroutine(DieRoutine());
	}
	#endregion

	#region Private Methods

	IEnumerator DieRoutine()
	{
		PhotonNetwork.Instantiate(_deathFXPrefab.name, _player.transform.position, Quaternion.identity);

		PhotonNetwork.Destroy(_player);
		_player = null;
		UIManager.Instance._deathScreen.SetActive(true);

		yield return new WaitForSeconds(_respawnDelayTime);

		UIManager.Instance._deathScreen.SetActive(false);

		if (MatchManager.Instance._state == MatchManager.GameState.Playing && _player == null)
			SpawnPlayer();
	}
	#endregion
}
