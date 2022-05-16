using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOverTime : MonoBehaviour
{
	#region Fields & Properties

	[SerializeField] float _lifetime = 1.5f;

	#endregion

	#region Unity Methods

	void Start() 
	{
		Destroy(gameObject, _lifetime);
	}
	#endregion
}
