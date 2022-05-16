using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
	#region Fields & Properties

	public bool _isAutomatic;
	public float _timeBetweenShots = 0.1f, _heatPerShot = 1f;
	public GameObject _muzzleFlash;
	public int _shotDamage;
	public float _adsZoom;
	public AudioSource _shotSound;

	#endregion
}
