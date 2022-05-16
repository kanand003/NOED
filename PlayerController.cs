using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviourPun
{
	#region Fields & Properties

	[Header("General")]
	[SerializeField] Material[] _allSkins;
	[Header("Movement")]
	[SerializeField] Transform _viewPoint;
	[SerializeField] float _mouseSensitivityX = 1f, _mouseSensitivityY = 1f;
	[SerializeField] float _moveSpeed = 5f, _runSpeed = 8f;
	[SerializeField] CharacterController _theController;
	[SerializeField] float _jumpForce = 12f, _gravityMod = 2.5f;
	[SerializeField] Transform _groundCheckPoint;
	[SerializeField] LayerMask _groundLayers;
	[SerializeField] Animator _anim;
	[SerializeField] GameObject _playerModel;
	[SerializeField] AudioSource _footstepWalk, _footstepRun;
	public bool _invertLook;
	[Header("Shooting")]
	[SerializeField] GameObject _bulletImpact;
	//[SerializeField] float _timeBetweenShots = 0.1f;
	[SerializeField] float _maxHeat = 10f, /*_heatPerShot = 1f,*/ _coolRate = 4f, _overheatCoolRate = 5f;
	[Header("Guns")]
	[SerializeField] Gun[] _allGuns;
	[SerializeField] float _muzzleDisplayTime;
	[SerializeField] Transform _modelGunPoint, _gunHolder;
	[SerializeField] float _adsSpeed = 5f;
	[SerializeField] Transform _adsOutPoint, _adsInPoint;
	[Header("FX")]
	[SerializeField] GameObject _playerHitFX;
	[Header("Health System")]
	[SerializeField] int _maxHealth = 100;

	//Movement
	bool _isGrounded;
	float _activeMoveSpeed;
	float _verticalRotStore;
	Vector2 _mouseInput;
	Vector3 _moveDir, _movement;

	Camera _theCam;

	//Shooting
	float _shotCounter, _heatCounter;
	bool _overheated;

	//Guns
	int _selectedGun;
	float _muzzleCounter;

	//Health
	int _currentHealth;

	#endregion

	#region Getters


	#endregion

	#region Unity Methods

	void Start() 
	{
		Cursor.lockState = CursorLockMode.Locked;
		_theCam = Camera.main;
		//SwitchGun();
		photonView.RPC(nameof(RPCSetGun), RpcTarget.All, _selectedGun);
		_currentHealth = _maxHealth;

		//Transform spawnLoc = SpawnManager.Instance.GetSpawnPoint();
		//transform.position = spawnLoc.position;
		//transform.rotation = spawnLoc.rotation;

		if (photonView.IsMine)
		{
			_playerModel.SetActive(false);
			UIManager.Instance._weaponTempSlider.maxValue = 10f;
			UpdateHealthbar();
		}
		else
		{
			_gunHolder.parent = _modelGunPoint;
			_gunHolder.localPosition = Vector3.zero;
			_gunHolder.localRotation = Quaternion.identity;
		}

		_playerModel.GetComponent<Renderer>().material = _allSkins[photonView.Owner.ActorNumber % _allSkins.Length];
	}
	
	void Update() 
	{
		if (!photonView.IsMine) return;

		_mouseInput = new Vector2(Input.GetAxisRaw("Mouse X") * _mouseSensitivityX, Input.GetAxisRaw("Mouse Y") * _mouseSensitivityY);

		transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + _mouseInput.x, transform.rotation.eulerAngles.z);

		_verticalRotStore += _mouseInput.y;
		_verticalRotStore = Mathf.Clamp(_verticalRotStore, -60f, 60f);

		if (_invertLook)
		{
			_viewPoint.rotation = Quaternion.Euler(_verticalRotStore, _viewPoint.transform.rotation.eulerAngles.y, _viewPoint.transform.rotation.eulerAngles.z);
		}
		else
		{
			_viewPoint.rotation = Quaternion.Euler(-_verticalRotStore, _viewPoint.transform.rotation.eulerAngles.y, _viewPoint.transform.rotation.eulerAngles.z);
		}

		_moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

		if (Input.GetKey(KeyCode.LeftShift))
		{
			_activeMoveSpeed = _runSpeed;

			if (!_footstepRun.isPlaying && _moveDir != Vector3.zero)
			{
				_footstepRun.Play();
				_footstepWalk.Stop();
			}
		}
		else
		{
			_activeMoveSpeed = _moveSpeed;

			if (!_footstepWalk.isPlaying && _moveDir != Vector3.zero)
			{
				_footstepWalk.Play();
				_footstepRun.Stop();
			}
		}

		if (_moveDir == Vector3.zero || !_isGrounded)
		{
			_footstepWalk.Stop();
			_footstepRun.Stop();
		}

		float yVelocity = _movement.y;

		_movement = ((transform.forward * _moveDir.z) + (transform.right * _moveDir.x)).normalized * _activeMoveSpeed;

		_movement.y = yVelocity;

		if (_theController.isGrounded)
			_movement.y = 0;

		_isGrounded = Physics.Raycast(_groundCheckPoint.position, Vector3.down, 0.25f, _groundLayers);

		if (Input.GetButtonDown("Jump") && _isGrounded)
			_movement.y = _jumpForce;

		_movement.y += Physics.gravity.y * Time.deltaTime * _gravityMod;

		_theController.Move(_movement * Time.deltaTime);

		//Animations...
		_anim.SetBool("grounded", _isGrounded);
		_anim.SetFloat("speed", _moveDir.magnitude);

		//Gun sight zooming...
		if (Input.GetMouseButton(1))
		{
			_theCam.fieldOfView = Mathf.Lerp(_theCam.fieldOfView, _allGuns[_selectedGun]._adsZoom, _adsSpeed * Time.deltaTime);
			_gunHolder.position = Vector3.Lerp(_gunHolder.position, _adsInPoint.position, _adsSpeed * Time.deltaTime);
		}
		else
		{
			_theCam.fieldOfView = Mathf.Lerp(_theCam.fieldOfView, 60f, _adsSpeed * Time.deltaTime);
			_gunHolder.position = Vector3.Lerp(_gunHolder.position, _adsOutPoint.position, _adsSpeed * Time.deltaTime);
		}

		//Cursor lock/unlock...
		if (Input.GetKeyDown(KeyCode.Escape) )
			Cursor.lockState = CursorLockMode.None;
		else if(Cursor.lockState==CursorLockMode.None)
		{
			if (Input.GetMouseButtonDown(0) && !UIManager.Instance._optionsScreen.activeInHierarchy)
				Cursor.lockState = CursorLockMode.Locked;
		}

		//Muzzle flash counter...
		if (_allGuns[_selectedGun]._muzzleFlash.activeInHierarchy)
		{
			_muzzleCounter -= Time.deltaTime;

			if(_muzzleCounter <= 0)
				_allGuns[_selectedGun]._muzzleFlash.SetActive(false);
		}

		//shooting...
		if (!_overheated)
		{
			if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
				Shoot();

			if (Input.GetMouseButton(0) && _allGuns[_selectedGun]._isAutomatic)
			{
				_shotCounter -= Time.deltaTime;

				if (_shotCounter <= 0 && !EventSystem.current.IsPointerOverGameObject())
					Shoot();
			}

			_heatCounter -= _coolRate * Time.deltaTime;
		}
		else
		{
			_heatCounter -= _overheatCoolRate * Time.deltaTime;

			if (_heatCounter <= 0)
			{
				_overheated = false;
				UIManager.Instance._overheatedText.gameObject.SetActive(false);
			}
		}

		if(_heatCounter < 0)
			_heatCounter = 0;

		UIManager.Instance._weaponTempSlider.value = _heatCounter;

		//gun selection...
		if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
		{
			_selectedGun++;

			if (_selectedGun >= _allGuns.Length)
				_selectedGun = 0;

			//SwitchGun();
			photonView.RPC(nameof(RPCSetGun), RpcTarget.All, _selectedGun);
		}
		else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
		{
			_selectedGun--;

			if (_selectedGun < 0)
				_selectedGun = _allGuns.Length - 1;

			//SwitchGun();
			photonView.RPC(nameof(RPCSetGun), RpcTarget.All, _selectedGun);
		}

		for (int i = 0; i < _allGuns.Length; i++)
		{
			if (Input.GetKeyDown((i + 1).ToString()))
			{
				_selectedGun = i;
				//SwitchGun();
				photonView.RPC(nameof(RPCSetGun), RpcTarget.All, _selectedGun);
			}
		}
	}

	void LateUpdate()
	{
		if (!photonView.IsMine) return;

		if (MatchManager.Instance._state == MatchManager.GameState.Playing)
		{
			_theCam.transform.position = _viewPoint.position;
			_theCam.transform.rotation = _viewPoint.rotation;
		}
		else
		{
			_theCam.transform.position = MatchManager.Instance._mapCamPoint.position;
			_theCam.transform.rotation = MatchManager.Instance._mapCamPoint.rotation;
		}
	}

	#endregion

	#region Photon Methods

	[PunRPC]
	public void RPCDealDamage(string damager, int damageAmount, int actor)
	{
		TakeDamage(damager, damageAmount, actor);
	}

	[PunRPC]
	public void RPCSetGun(int gunToSwitchTo)
	{
		if (gunToSwitchTo < _allGuns.Length)
		{
			_selectedGun = gunToSwitchTo;
			SwitchGun();
		}
	}
	#endregion

	#region Public Methods

	public void TakeDamage(string damager, int damageAmount, int actor)
	{
		//Debug.Log($"Damage Taken: { damageAmount}");

		if (photonView.IsMine)
		{
			//Debug.Log($"{photonView.Owner.NickName} has been hit by {damager}");

			_currentHealth = Mathf.Max(_currentHealth - damageAmount, 0);
			UpdateHealthbar();

			if(_currentHealth == 0)
			{
				PlayerSpawner.Instance.Die(damager);
				MatchManager.Instance.UpdateStatSend(actor, 0, 1);
			}
		}
	}
	#endregion

	#region Private Methods

	void Shoot()
	{
		Ray ray = _theCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
		ray.origin = _theCam.transform.position;

		if(Physics.Raycast(ray, out RaycastHit hit))
		{
			//Debug.Log($"We hit {hit.collider.name}");
			if (hit.collider.CompareTag("Player"))
			{
				//Debug.Log($"Hit {hit.collider.gameObject.GetPhotonView().Owner.NickName}");

				PhotonNetwork.Instantiate(_playerHitFX.name, hit.point, Quaternion.identity);

				hit.collider.gameObject.GetPhotonView().RPC(nameof(RPCDealDamage), RpcTarget.All, photonView.Owner.NickName, _allGuns[_selectedGun]._shotDamage, PhotonNetwork.LocalPlayer.ActorNumber);
			}
			else
			{
				GameObject impactObject = Instantiate(_bulletImpact, hit.point + (hit.normal * 0.002f), Quaternion.LookRotation(hit.normal, Vector3.up));

				Destroy(impactObject, 10f);
			}
		}

		_shotCounter = _allGuns[_selectedGun]._timeBetweenShots;
		_heatCounter += _allGuns[_selectedGun]._heatPerShot;

		if (_heatCounter >= _maxHeat)
		{
			_heatCounter = _maxHeat;
			_overheated = true;
			UIManager.Instance._overheatedText.gameObject.SetActive(true);
		}

		_allGuns[_selectedGun]._muzzleFlash.SetActive(true);
		_muzzleCounter = _muzzleDisplayTime;

		_allGuns[_selectedGun]._shotSound.Stop();
		_allGuns[_selectedGun]._shotSound.Play();
	}

	void SwitchGun()
	{
		foreach (Gun gun in _allGuns)
			gun.gameObject.SetActive(false);

		_allGuns[_selectedGun].gameObject.SetActive(true);
		_allGuns[_selectedGun]._muzzleFlash.SetActive(false);
	}

	void UpdateHealthbar()
	{
		UIManager.Instance._healthSlider.maxValue = _maxHealth;
		UIManager.Instance._healthSlider.value = _currentHealth;
	}
	#endregion
}
