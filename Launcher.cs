using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
	#region Fields & Properties

	public static Launcher Instance;

	[Header("Loading Screen")]
	[SerializeField] GameObject _loadingScreen;
	[SerializeField] GameObject _menuButtons;
	[SerializeField] TMP_Text _loadingText;
	[Header("Create Room Screen")]
	[SerializeField] GameObject _createRoomScreen;
	[SerializeField] TMP_InputField _roomNameInput;
	[Header("Room Screen")]
	[SerializeField] GameObject _roomScreen;
	[SerializeField] GameObject _startGameButton;
	[SerializeField] TMP_Text _roomNameText, _playerNameLabel;
	List<TMP_Text> _allPlayerNames = new List<TMP_Text>();
	[Header("Error Screen")]
	[SerializeField] GameObject _errorScreen;
	[SerializeField] TMP_Text _errorText;
	[Header("Room Browser Screen")]
	[SerializeField] GameObject _roomBrowserScreen;
	[SerializeField] RoomButton _theRoomButton;
	List<RoomButton> _allRoomButtons = new List<RoomButton>();
	[Header("Name Input Screen")]
	[SerializeField] GameObject _nameInputScreen;
	[SerializeField] TMP_InputField _nameInput;
	public static bool _hasSetNickname;
	[Header("Misc")]
	public string[] _allMaps;
	public bool _changeMapsBetweenMaps = true;

	[SerializeField] GameObject _roomTestButton;

	[SerializeField] string _gameScene;

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
		CloseMenus();

		_loadingScreen.SetActive(true);
		_loadingText.text = "Connecting to Network...";

		if(!PhotonNetwork.IsConnected)
			PhotonNetwork.ConnectUsingSettings();

#if UNITY_EDITOR
		_roomTestButton.SetActive(true);
#endif

		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}
	#endregion

	#region Photon Callbacks

	public override void OnConnectedToMaster()
	{
		PhotonNetwork.JoinLobby();
		PhotonNetwork.AutomaticallySyncScene = true;
		_loadingText.text = "Joining Lobby...";
	}

	public override void OnJoinedLobby()
	{
		CloseMenus();
		_menuButtons.SetActive(true);

		//PhotonNetwork.NickName = $"Player{Random.Range(0, 1001)}";

		if (!_hasSetNickname)
		{
			CloseMenus();
			_nameInputScreen.SetActive(true);

			if (PlayerPrefs.HasKey("PlayerName"))
				_nameInput.text = PlayerPrefs.GetString("PlayerName");
		}
		else
		{
			PhotonNetwork.NickName = PlayerPrefs.GetString("PlayerName");
		}
	}

	public override void OnJoinedRoom()
	{
		CloseMenus();
		_roomScreen.SetActive(true);
		_roomNameText.text = PhotonNetwork.CurrentRoom.Name;

		ListAllPlayers();

		if (PhotonNetwork.IsMasterClient)
			_startGameButton.SetActive(true);
		else
			_startGameButton.SetActive(false);
	}

	public override void OnCreateRoomFailed(short returnCode, string message)
	{
		_errorText.text = $"Failed to Create the Room: {message}";
		CloseMenus();
		_errorScreen.SetActive(true);
	}

	public override void OnLeftRoom()
	{
		CloseMenus();
		_menuButtons.SetActive(true);
	}

	public override void OnRoomListUpdate(List<RoomInfo> roomList)
	{
		foreach (RoomButton roomButton in _allRoomButtons)
			Destroy(roomButton.gameObject);

		_allRoomButtons.Clear();
		_theRoomButton.gameObject.SetActive(false);

		for (int i = 0; i < roomList.Count; i++)
		{
			if (roomList[i].PlayerCount != roomList[i].MaxPlayers && !roomList[i].RemovedFromList)
			{
				RoomButton newButton = Instantiate(_theRoomButton, _theRoomButton.transform.parent);
				newButton.SetButtonDetails(roomList[i]);
				newButton.gameObject.SetActive(true);
				_allRoomButtons.Add(newButton);
			}
		}
	}

	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		TMP_Text newPlayerLabel = Instantiate(_playerNameLabel, _playerNameLabel.transform.parent);
		newPlayerLabel.text = newPlayer.NickName;
		newPlayerLabel.gameObject.SetActive(true);
		_allPlayerNames.Add(newPlayerLabel);
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		ListAllPlayers();
	}

	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		if (PhotonNetwork.IsMasterClient)
			_startGameButton.SetActive(true);
		else
			_startGameButton.SetActive(false);
	}
	#endregion

	#region Public Methods

	public void OpenCreateRoom()
	{
		CloseMenus();
		_createRoomScreen.SetActive(true);
	}

	public void CreateRoom()
	{
		if (!string.IsNullOrEmpty(_roomNameInput.text))
		{
			RoomOptions options = new RoomOptions();
			options.MaxPlayers = 8;
			PhotonNetwork.CreateRoom(_roomNameInput.text, options);

			CloseMenus();

			_loadingText.text = "Creating Room...";
			_loadingScreen.SetActive(true);
		}
	}

	public void CloseErrorScreen()
	{
		CloseMenus();
		_menuButtons.SetActive(true);
	}

	public void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();
		CloseMenus();
		_loadingText.text = "Leaving Room...";
		_loadingScreen.SetActive(true);
	}

	public void OpenRoomBrowser()
	{
		CloseMenus();
		_roomBrowserScreen.SetActive(true);
	}

	public void CloseRoomBrowser()
	{
		CloseMenus();
		_menuButtons.SetActive(true);
	}

	public void JoinRoom(RoomInfo inputInfo)
	{
		PhotonNetwork.JoinRoom(inputInfo.Name);

		CloseMenus();
		_loadingText.text = "Joining Room...";
		_loadingScreen.SetActive(true);
	}

	public void SetNickname()
	{
		if (!string.IsNullOrEmpty(_nameInput.text))
		{
			PhotonNetwork.NickName = _nameInput.text;

			PlayerPrefs.SetString("PlayerName", _nameInput.text);

			CloseMenus();
			_menuButtons.SetActive(true);
			_hasSetNickname = true;
		}
	}

	public void StartGame()
	{
		//PhotonNetwork.LoadLevel(_gameScene);
		PhotonNetwork.LoadLevel(_allMaps[Random.Range(0, _allMaps.Length)]);
	}

	public void QuitGame()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
		Application.Quit();
	}

	public void QuickJoin()
	{
		RoomOptions options = new RoomOptions();
		options.MaxPlayers = 8;
		PhotonNetwork.CreateRoom("Test", options);
		CloseMenus();
		_loadingText.text = "Creating Test Room...";
		_loadingScreen.SetActive(true);
	}
	#endregion

	#region Private Methods

	void CloseMenus()
	{
		_loadingScreen.SetActive(false);
		_menuButtons.SetActive(false);
		_createRoomScreen.SetActive(false);
		_roomScreen.SetActive(false);
		_errorScreen.SetActive(false);
		_roomBrowserScreen.SetActive(false);
		_nameInputScreen.SetActive(false);
	}

	void ListAllPlayers()
	{
		foreach (TMP_Text player in _allPlayerNames)
			Destroy(player.gameObject);

		_allPlayerNames.Clear();

		Player[] players = PhotonNetwork.PlayerList;
		for (int i = 0; i < players.Length; i++)
		{
			TMP_Text newPlayerLabel = Instantiate(_playerNameLabel, _playerNameLabel.transform.parent);
			newPlayerLabel.text = players[i].NickName;
			newPlayerLabel.gameObject.SetActive(true);
			_allPlayerNames.Add(newPlayerLabel);
		}
	}
	#endregion
}
