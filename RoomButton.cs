using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Realtime;

public class RoomButton : MonoBehaviour
{
	#region Fields & Properties

	[SerializeField] TMP_Text _buttonText;

	RoomInfo _info;

	#endregion

	#region Getters


	#endregion

	#region Unity Methods

	void Start() 
	{
		
	}
	#endregion

	#region Public Methods

	public void SetButtonDetails(RoomInfo inputInfo)
	{
		_info = inputInfo;

		_buttonText.text = _info.Name;
	}

	public void OpenRoom()
	{
		Launcher.Instance.JoinRoom(_info);
	}
	#endregion

	#region Private Methods


	#endregion
}
