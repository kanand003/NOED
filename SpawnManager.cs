using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
	#region Fields & Properties

	public static SpawnManager Instance;

	[SerializeField] Transform[] _spawnPoints;

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
		foreach (Transform spawn in _spawnPoints)
			spawn.gameObject.SetActive(false);
	}
	#endregion

	#region Public Methods

	public Transform GetSpawnPoint()
	{
		return _spawnPoints[Random.Range(0, _spawnPoints.Length)];
	}
	#endregion

	#region Private Methods


	#endregion
}
