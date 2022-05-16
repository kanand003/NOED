using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
	public enum EventCodes : byte
	{
		NewPlayer,
		ListPlayers,
		UpdateStat,
		NextMatch,
		TimerSync,
		RemovePlayer
	}

	public enum GameState
	{
		Waiting,
		Playing,
		Ending
	}

	#region Fields & Properties

	public static MatchManager Instance;

	[SerializeField] List<PlayerInfo> _allPlayers = new List<PlayerInfo>();
	[SerializeField] int _killsToWin = 3;
	public Transform _mapCamPoint;
	public GameState _state = GameState.Waiting;
	[SerializeField] float _waitAfterEnding = 5f;
	public bool _perpetual;
	public float _matchLength = 180f;

	float _currentMatchTime, _sendTimer;
	int _index; //the local player's index in the _allPlayers List

	List<LeaderboardPlayer> _lboardPlayers = new List<LeaderboardPlayer>();

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
		if (!PhotonNetwork.IsConnected)
			SceneManager.LoadScene(0);
		else
		{
			NewPlayerSend(PhotonNetwork.NickName);
			_state = GameState.Playing;

			SetupTimer();
		}

		if (!PhotonNetwork.IsMasterClient)
			UIManager.Instance._timerText.gameObject.SetActive(false);
	}

	void Update()
	{
		//leaderboard...
		if (Input.GetKeyDown(KeyCode.Tab) && _state != GameState.Ending)
		{
			if (UIManager.Instance._leaderboard.activeInHierarchy)
				UIManager.Instance._leaderboard.SetActive(false);
			else
				ShowLeaderboard();
		}
		//match timer...
		if (PhotonNetwork.IsMasterClient)
		{
			if (_currentMatchTime > 0 && _state == GameState.Playing)
			{
				//_currentMatchTime = Mathf.Max(0f, _currentMatchTime - Time.deltaTime);
				_currentMatchTime -= Time.deltaTime;

				if (_currentMatchTime <= 0f)
				{
					_currentMatchTime = 0f;
					_sendTimer = 0f;
					_state = GameState.Ending;
					ListPlayersSend();   //update all player's state
					StateCheck();
				}
				UpdateTimerDisplay();

				_sendTimer -= Time.deltaTime;
				if (_sendTimer <= 0f)
				{
					_sendTimer += 1f;
					TimerSend();
				}
			}
		}
	}
	#endregion

	#region Photon Methods

	public void OnEvent(EventData photonEvent)
	{
		if (photonEvent.Code < 200)
		{
			EventCodes theEvent = (EventCodes)photonEvent.Code;
			object[] data = (object[])photonEvent.CustomData;

			//Debug.Log($"Received Event {theEvent}");

			switch (theEvent)
			{
				case EventCodes.NewPlayer:
					NewPlayerReceive(data);
					break;
				case EventCodes.ListPlayers:
					ListPlayersReceive(data);
					break;
				case EventCodes.UpdateStat:
					UpdateStatReceive(data);
					break;
				case EventCodes.NextMatch:
					NextMatchReceive();
					break;
				case EventCodes.TimerSync:
					TimerReceive(data);
					break;
				case EventCodes.RemovePlayer:
					RemovePlayerReceive(data);
					break;
			}
		}
	}

	public override void OnEnable()
	{
		base.OnEnable();
		//PhotonNetwork.AddCallbackTarget(this);	//this is the base call...
	}

	public override void OnDisable()
	{
		base.OnDisable();
		//PhotonNetwork.RemoveCallbackTarget(this);	//this is the base call...
	}

	public override void OnLeftRoom()
	{
		//TODO: SEND REMOVE PLAYER FROM THE _ALLPLAYERS LIST EVENT
		//RemovePlayerSend();
		SceneManager.LoadScene(0);
	}
	#endregion

	#region Photon Event Methods

	public void NewPlayerSend(string username)
	{
		object[] package = new object[4];
		package[0] = username;
		package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
		package[2] = 0;
		package[3] = 0;

		PhotonNetwork.RaiseEvent(
			(byte)EventCodes.NewPlayer,
			package,
			new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
			new SendOptions { Reliability = true });
	}

	public void NewPlayerReceive(object[] dataReceived)   //master Client only
	{
		PlayerInfo player = new PlayerInfo(
			(string)dataReceived[0],
			(int)dataReceived[1],
			(int)dataReceived[2],
			(int)dataReceived[3]);

		_allPlayers.Add(player);
		ListPlayersSend();
	}

	public void ListPlayersSend()	//master client only
	{
		object[] package = new object[_allPlayers.Count + 1];
		package[0] = _state;

		for (int i = 0; i < _allPlayers.Count; i++)
		{
			object[] piece = new object[4];
			piece[0] = _allPlayers[i].Name;
			piece[1] = _allPlayers[i].Actor;
			piece[2] = _allPlayers[i].Kills;
			piece[3] = _allPlayers[i].Deaths;

			package[i + 1] = piece;
		}

		PhotonNetwork.RaiseEvent(
			(byte)EventCodes.ListPlayers,
			package,
			new RaiseEventOptions { Receivers = ReceiverGroup.All },
			new SendOptions { Reliability = true });
	}

	public void ListPlayersReceive(object[] dataReceived)
	{
		_allPlayers.Clear();

		_state = (GameState)dataReceived[0];

		for (int i = 1; i < dataReceived.Length; i++)
		{
			object[] piece = (object[])dataReceived[i];
			PlayerInfo player = new PlayerInfo(
				(string)piece[0],
				(int)piece[1],
				(int)piece[2],
				(int)piece[3]);

			_allPlayers.Add(player);

			if (PhotonNetwork.LocalPlayer.ActorNumber == player.Actor)
				_index = i - 1;
		}
		StateCheck();
	}

	public void UpdateStatSend(int actorSending, int statToUpdate, int amountToChange)
	{
		object[] package = new object[] { actorSending, statToUpdate, amountToChange };

		PhotonNetwork.RaiseEvent(
			(byte)EventCodes.UpdateStat,
			package,
			new RaiseEventOptions { Receivers = ReceiverGroup.All },
			new SendOptions { Reliability = true });
	}

	public void UpdateStatReceive(object[] dataReceived)
	{
		int actor = (int)dataReceived[0];
		int statType = (int)dataReceived[1];
		int amount = (int)dataReceived[2];

		for (int i = 0; i < _allPlayers.Count; i++)
		{
			if (_allPlayers[i].Actor == actor)
			{
				switch (statType)
				{
					case 0:
						_allPlayers[i].Kills += amount;
						Debug.Log($"Player {_allPlayers[i].Name} : kills {_allPlayers[i].Kills}");
						break;

					case 1:
						_allPlayers[i].Deaths += amount;
						Debug.Log($"Player {_allPlayers[i].Name} : deaths {_allPlayers[i].Deaths}");
						break;
				}

				if (i == _index)
					UpdateStatsDisplay();

				if (UIManager.Instance._leaderboard.activeInHierarchy)
					ShowLeaderboard();

				break;
			}
		}
		ScoreCheck();
	}

	public void NextMatchSend()
	{
		PhotonNetwork.RaiseEvent(
			(byte)EventCodes.NextMatch,
			null,
			new RaiseEventOptions { Receivers = ReceiverGroup.All },
			new SendOptions { Reliability = true });
	}

	public void NextMatchReceive()
	{
		_state = GameState.Playing;
		UIManager.Instance._endScreen.SetActive(false);
		UIManager.Instance._leaderboard.SetActive(false);

		foreach(PlayerInfo player in _allPlayers)
		{
			player.Kills = 0;
			player.Deaths = 0;
		}
		UpdateStatsDisplay();
		PlayerSpawner.Instance.SpawnPlayer();
		SetupTimer();
	}

	public void TimerSend()	//master client only
	{
		object[] package = new object[] { (int)_currentMatchTime, _state };
		PhotonNetwork.RaiseEvent(
			(byte)EventCodes.TimerSync,
			package,
			new RaiseEventOptions { Receivers = ReceiverGroup.Others },
			new SendOptions { Reliability = true });
	}

	public void TimerReceive(object[] dataReceived)
	{
		_currentMatchTime = (int)dataReceived[0];
		_state = (GameState)dataReceived[1];
		UpdateTimerDisplay();
		UIManager.Instance._timerText.gameObject.SetActive(true);
	}

	public void RemovePlayerSend()
	{
		object[] package = new object[] { PhotonNetwork.LocalPlayer.NickName };
		PhotonNetwork.RaiseEvent(
			(byte)EventCodes.RemovePlayer,
			package,
			new RaiseEventOptions { Receivers = ReceiverGroup.Others },
			new SendOptions { Reliability = true });
	}

	public void RemovePlayerReceive(object[] dataReceived)
	{
		string name = (string)dataReceived[0];

		for (int i = 0; i < _allPlayers.Count; i++)
		{
			if (_allPlayers[i].Name == name)
			{
				_allPlayers.RemoveAt(i);
				break;
			}
		}
	}
	#endregion

	#region public Methods

	public void UpdateStatsDisplay()
	{
		if(_allPlayers.Count > _index)
		{
			UIManager.Instance._killsText.text = $"Kills:     {_allPlayers[_index].Kills}";
			UIManager.Instance._deathsText.text = $"Deaths: {_allPlayers[_index].Deaths}";
		}
		else
		{
			UIManager.Instance._killsText.text = $"Kills:    {0}";
			UIManager.Instance._deathsText.text = $"Deaths: {0}";
		}
	}

	public void SetupTimer()
	{
		if (_matchLength > 0)
		{
			_currentMatchTime = _matchLength;
			UpdateTimerDisplay();
		}
	}

	public void UpdateTimerDisplay()
	{
		var timeToDisplay = System.TimeSpan.FromSeconds(_currentMatchTime);
		UIManager.Instance._timerText.text = timeToDisplay.Minutes.ToString("00") + ":" + timeToDisplay.Seconds.ToString("00");
	}
	#endregion

	#region Private Methods

	void ShowLeaderboard()
	{
		UIManager.Instance._leaderboard.SetActive(true);

		foreach (LeaderboardPlayer player in _lboardPlayers)
			Destroy(player.gameObject);

		_lboardPlayers.Clear();
		UIManager.Instance._leaderboardPlayerDisplay.gameObject.SetActive(false);

		List<PlayerInfo> sortedPlayers = SortPlayers(_allPlayers);

		foreach(PlayerInfo player in sortedPlayers)
		{
			LeaderboardPlayer newPlayerDisplay = Instantiate(UIManager.Instance._leaderboardPlayerDisplay, UIManager.Instance._leaderboardPlayerDisplay.transform.parent);

			newPlayerDisplay.SetDetails(player.Name, player.Kills, player.Deaths);
			newPlayerDisplay.gameObject.SetActive(true);
			_lboardPlayers.Add(newPlayerDisplay);
		}
	}

	List<PlayerInfo> SortPlayers(List<PlayerInfo> players)
	{
		List<PlayerInfo> sorted = new List<PlayerInfo>();

		while(sorted.Count < players.Count)
		{
			int highest = -1;
			PlayerInfo selectedPlayer = players[0];

			foreach(PlayerInfo player in players)
			{
				if (!sorted.Contains(player))
				{
					if (player.Kills > highest)
					{
						selectedPlayer = player;
						highest = player.Kills;
					}
				}
			}
			sorted.Add(selectedPlayer);
		}

		return sorted;
	}

	void ScoreCheck()
	{
		bool winnerFound = false;

		foreach(PlayerInfo player in _allPlayers)
		{
			if (player.Kills >= _killsToWin && _killsToWin > 0)
			{
				winnerFound = true;
				break;
			}
		}

		if (winnerFound)
		{
			if (PhotonNetwork.IsMasterClient && _state != GameState.Ending)
			{
				_state = GameState.Ending;
				ListPlayersSend();
			}
		}
	}

	void StateCheck()
	{
		if (_state == GameState.Ending)
			EndGame();
	}

	void EndGame()
	{
		_state = GameState.Ending;

		if (PhotonNetwork.IsMasterClient)
		{
			PhotonNetwork.DestroyAll();
		}

		UIManager.Instance._endScreen.SetActive(true);
		ShowLeaderboard();
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;

		Camera.main.transform.position = _mapCamPoint.position;
		Camera.main.transform.rotation = _mapCamPoint.rotation;

		StartCoroutine(EndRoutine());
	}

	IEnumerator EndRoutine()
	{
		yield return new WaitForSeconds(_waitAfterEnding);

		if (!_perpetual)
		{
			PhotonNetwork.AutomaticallySyncScene = false;
			PhotonNetwork.LeaveRoom();
		}
		else
		{
			if (PhotonNetwork.IsMasterClient)
			{
				if(!Launcher.Instance._changeMapsBetweenMaps)
					NextMatchSend();
				else
				{
					int newLevel = Random.Range(0, Launcher.Instance._allMaps.Length);

					if (Launcher.Instance._allMaps[newLevel] == SceneManager.GetActiveScene().name)
						NextMatchSend();
					else
						PhotonNetwork.LoadLevel(Launcher.Instance._allMaps[newLevel]);
				}
			}
		}
	}
	#endregion
}

[System.Serializable]
public class PlayerInfo
{
	public string Name;
	public int Actor, Kills, Deaths;

	public PlayerInfo(string name, int actor, int kills, int deaths)
	{
		Name = name;
		Actor = actor;
		Kills = kills;
		Deaths = deaths;
	}
}
