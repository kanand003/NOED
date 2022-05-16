using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardPlayer : MonoBehaviour
{
	#region Fields & Properties

	[SerializeField] TMP_Text _playerNameText, _killsText, _deathsText;

	#endregion

	#region Getters


	#endregion

	#region Unity Methods


	#endregion

	#region Public Methods

	public void SetDetails(string name, int kills, int deaths)
	{
		_playerNameText.text = name;
		_killsText.text = kills.ToString();
		_deathsText.text = deaths.ToString();
	}
	#endregion

	#region Private Methods


	#endregion
}
