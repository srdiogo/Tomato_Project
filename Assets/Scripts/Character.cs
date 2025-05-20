using System;
using System.Collections.Generic;
using LitJson;
using StarterAssets;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class Character : NetworkBehaviour
{
    [SerializeField] private string _id = ""; public string id { get { return _id; } }
    [SerializeField] private Transform _weaponHolder = null;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    private Weapon _weapon = null; public Weapon weapon { get { return _weapon; } }
    private Ammo _ammo = null; public Ammo ammo { get { return _ammo; } }
    private Animator _animator = null;
    private List<Item> _items = new List<Item>();
    private RigManager _rigManager = null;
    private Weapon _weaponToEquip = null;
    private bool _reloading = false; public bool reloading { get { return _reloading; } }
    private bool _switchingWeapon = false; public bool switchingWeapon { get { return _switchingWeapon; } }

    [SerializeField] private Rigidbody[] _ragdollRigidbodies = null;
    [SerializeField] private Collider[] _ragdollColliders = null;

    private float _health = 100;

    private bool _isGrounded = false; public bool isGrounded { get { return _isGrounded; } set { _isGrounded = value; } }
    private bool _walking = false; public bool walking { get { return _walking; } set { _walking = value; } }
    private float _speedAnimationMultiplier = 0f; public float speedAnimationMultiplier { get { return _speedAnimationMultiplier; } }
    private bool _aiming = false; public bool aiming { get { return _aiming; } set { _aiming = value; } }
    private bool _sprinting = false; public bool sprinting { get { return _sprinting; } set { _sprinting = value; } }
    private float _aimLayerWeight = 0;
    private Vector2 _aimingMovingAnimationsInput = Vector2.zero;
    private float aimRigWeight = 0;
    private float leftHandRigWeight = 0;

    private Vector3 _aimTarget = Vector3.zero; public Vector3 aimTarget { get { return _aimTarget; } set { _aimTarget = value; } }
    private Vector3 _lastAimTarget = Vector3.zero;
    private Vector3 _lastPosition = Vector3.zero;

    private ulong _clientID = 0;
    private bool _initialized = false;
    private bool _componentsInitialized = false;

    private float _moveSpeed = 0f; public float moveSpeed { get { return _moveSpeed; } set { _moveSpeed = value; } }
    private float _moveSpeedBlend = 0f;
    private float _lastMoveSpeed = 0f;

    private Vector2 _aimedMoveSpeed = Vector2.zero;
    private Vector2 _lastAimedMoveSpeed = Vector2.zero;

    private bool _lastAiming = false;

    [System.Serializable]
    public struct Data
    {
        public Dictionary<string, int> items;
        public List<string> itemsId;
        public List<string> equippedIds;
    }

    public Data GetData()
    {
        Data data = new Data();
        data.items = new Dictionary<string, int>();
        data.itemsId = new List<string>();
        data.equippedIds = new List<string>();

        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] == null)
            {
                continue;
            }

            int value = 0;
            if (_items[i].GetType() == typeof(Weapon))
            {
                value = ((Weapon)_items[i]).ammo;
            }
            else if (_items[i].GetType() == typeof(Ammo))
            {
                value = ((Ammo)_items[i]).amount;
            }

            data.items.Add(_items[i].id, value);
            data.itemsId.Add(_items[i].networkId);

            if (_weapon != null && _items[i] == _weapon)
            {
                data.equippedIds.Add(_items[i].networkId);
            }
            else if (_ammo != null && _items[i] == _ammo)
            {
                data.equippedIds.Add(_items[i].networkId);
            }
        }
        return data;

    }
    private void Awake()
    {
        InitializeComponents();
    }
    private void InitializeComponents()
    {
        if (_componentsInitialized)
        {
            return;
        }
        _componentsInitialized = true;
        _ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        _ragdollColliders = GetComponentsInChildren<Collider>();

        if (_ragdollRigidbodies != null)
        {
            for (int i = 0; i < _ragdollRigidbodies.Length; i++)
            {
                _ragdollRigidbodies[i].mass *= 50;
            }
        }
        if (_ragdollColliders != null)
        {
            for (int i = 0; i < _ragdollColliders.Length; i++)
            {
                _ragdollColliders[i].isTrigger = false;
            }
        }
        SetRagdollStatus(false);
        _rigManager = GetComponent<RigManager>();
        _animator = GetComponent<Animator>();
        _fallTimeoutDelta = FallTimeout;
    }
    public void InitializeServer(Dictionary<string, int> items, List<string> itemsId, List<string> equippedIds, ulong clientID)
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;
        InitializeComponents();
        _clientID = clientID;
        SetLayer(transform, LayerMask.NameToLayer("NetworkPlayer"));
        _Initialize(items, itemsId, equippedIds);
    }

    [ClientRpc]
    public void InitializeClientRpc(string itemsJson, string itemsIdJson, string equippedJson, string itemsOnGroundJson, ulong clientID)
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;
        InitializeComponents();
        _clientID = clientID;
        if (IsOwner)
        {
            SetLayer(transform, LayerMask.NameToLayer("LocalPlayer"));
        }
        else
        {
            SetLayer(transform, LayerMask.NameToLayer("NetworkPlayer"));
        }
        Dictionary<string, int> items = JsonMapper.ToObject<Dictionary<string, int>>(itemsJson);
        List<string> itemsId = JsonMapper.ToObject<List<string>>(itemsIdJson);
        List<string> equippedIds = JsonMapper.ToObject<List<string>>(equippedJson);
        List<Item.Data> itemsOnGround = JsonMapper.ToObject<List<Item.Data>>(itemsOnGroundJson);
        InitializeItemsOnGround(itemsOnGround);
        if (items != null && itemsId != null)
        {
            _Initialize(items, itemsId, equippedIds);
        }
    }
    private void InitializeItemsOnGround(List<Item.Data> itemsOnGround)
    {

    }

    [ClientRpc]
    public void InitializeClientRpc(string dataJson, ulong clientID, ClientRpcParams rpcParams = default)
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;
        InitializeComponents();
        _clientID = clientID;
        if (IsOwner)
        {
            SetLayer(transform, LayerMask.NameToLayer("LocalPlayer"));
        }
        else
        {
            SetLayer(transform, LayerMask.NameToLayer("NetworkPlayer"));
        }
        Data data = JsonMapper.ToObject<Data>(dataJson);
        _Initialize(data.items, data.itemsId, data.equippedIds);
    }

    private void Update()
    {
        bool armed = _weapon != null;

        GroundedCheck();
        FreeFall();

        if (_shots.Count > 0 && !IsOwner)
        {
            if (_weapon != null && _weapon.networkId == _shots[0])
            {
                bool shoot = Shoot();
                if (shoot)
                {
                    _shots.RemoveAt(0);
                }
            }
            else
            {
                _shots.RemoveAt(0);
            }
        }
        _aimLayerWeight = Mathf.Lerp(_aimLayerWeight, _switchingWeapon || (armed && (_aiming || _reloading)) ? 1 : 0, 10f * Time.deltaTime);
        _animator.SetLayerWeight(1, _aimLayerWeight);

        aimRigWeight = Mathf.Lerp(aimRigWeight, armed && _aiming && !_reloading ? 1 : 0, 10f * Time.deltaTime);
        leftHandRigWeight = Mathf.Lerp(leftHandRigWeight, armed && _switchingWeapon == false && !_reloading && (_aiming || (_isGrounded && _weapon.type == Weapon.Handle.TwoHanded)) ? 1 : 0, 10f * Time.deltaTime);

        _rigManager.aimTarget = _aimTarget;
        _rigManager.aimWeight = aimRigWeight;
        _rigManager.leftHandWeight = leftHandRigWeight;

        _moveSpeedBlend = Mathf.Lerp(_moveSpeedBlend, _moveSpeed, Time.deltaTime * 10f);
        if (_moveSpeed < 0.01)
        {
            _moveSpeed = 0.0f;
        }

        if (_sprinting)
        {
            _speedAnimationMultiplier = 3;
        }
        else if (_walking)
        {
            _speedAnimationMultiplier = 1;
        }
        else
        {
            _speedAnimationMultiplier = 2;
        }

        if (IsOwner)
        {
            Vector3 deltaPosition = transform.InverseTransformDirection(transform.position - _lastPosition).normalized;
            _aimedMoveSpeed = new Vector2(deltaPosition.x, deltaPosition.z * _speedAnimationMultiplier);
        }

        _aimingMovingAnimationsInput = Vector2.Lerp(_aimingMovingAnimationsInput, _aimedMoveSpeed, 10 * Time.deltaTime);
        _animator.SetFloat("Speed_X", _aimingMovingAnimationsInput.x);
        _animator.SetFloat("Speed_Y", _aimingMovingAnimationsInput.y);
        _animator.SetFloat("Armed", armed ? 1 : 0);
        _animator.SetFloat("Aimed", _aiming ? 1 : 0);
        _animator.SetFloat("Speed", _moveSpeedBlend);

        if (IsOwner)
        {
            if (_aiming != _lastAiming)
            {
                OnAimingChangedServerRpc(_aiming);
                _lastAiming = _aiming;
            }

            if (_aimTarget != _lastAimTarget)
            {
                OnAimTargetChangedServerRpc(_aimTarget);
                _lastAimTarget = _aimTarget;
            }
            if (_aiming)
            {
                if (_aimedMoveSpeed != _lastAimedMoveSpeed)
                {
                    OnAimingMoveChangedServerRpc(_aimedMoveSpeed);
                    _lastAimedMoveSpeed = _aimedMoveSpeed;
                }
            }
            else
            {
                if (_moveSpeed != _lastMoveSpeed)
                {
                    OnMoveSpeedChangedServerRpc(moveSpeed);
                    _lastMoveSpeed = _moveSpeed;
                }
            }
        }
    }

    [ServerRpc]
    public void OnAimTargetChangedServerRpc(Vector3 value)
    {
        _aimTarget = value;
        OnAimTargetChangedClientRpc(value);
    }

    [ClientRpc]
    public void OnAimTargetChangedClientRpc(Vector3 value)
    {
        if (!IsOwner)
        {
            _aimTarget = value;
        }
    }

    [ServerRpc]
    public void OnAimingMoveChangedServerRpc(Vector2 value)
    {
        _aimedMoveSpeed = value;
        OnAimingMoveChangedClientRpc(value);
    }

    [ClientRpc]
    public void OnAimingMoveChangedClientRpc(Vector2 value)
    {
        if (!IsOwner)
        {
            _aimedMoveSpeed = value;
        }
    }
    [ServerRpc]
    public void OnAimingChangedServerRpc(bool value)
    {
        _aiming = value;
        OnAimingChangedClientRpc(value);
    }

    [ClientRpc]
    public void OnAimingChangedClientRpc(bool value)
    {
        if (!IsOwner)
        {
            _aiming = value;
        }
    }

    [ServerRpc]
    public void OnMoveSpeedChangedServerRpc(float value)
    {
        _moveSpeed = value;
        OnMoveSpeedClientRpc(value);
    }

    [ClientRpc]
    public void OnMoveSpeedClientRpc(float value)
    {
        if (!IsOwner)
        {
            _moveSpeed = value;
        }
    }

    private void LateUpdate()
    {
        _lastPosition = transform.position;
    }

    private void SetRagdollStatus(bool enabled)
    {
        if (_ragdollRigidbodies != null)
        {
            for (int i = 0; i < _ragdollRigidbodies.Length; i++)
            {
                _ragdollRigidbodies[i].isKinematic = !enabled;
            }
        }
    }

    private void _Initialize(Dictionary<string, int> items, List<string> itemsId, List<string> equippedIds)
    {
        if (items != null && PrefabManager.singleton != null)
        {
            int i = 0;
            int equippedWeaponIndex = -1;
            int equippedAmmoIndex = -1;
            foreach (var itemData in items)
            {
                Item prefab = PrefabManager.singleton.GetItemPrefab(itemData.Key);
                if (prefab != null)
                {
                    Item item = Instantiate(prefab, transform);
                    item.networkId = itemsId[i];
                    if (item.GetType() == typeof(Weapon))
                    {
                        Weapon w = (Weapon)item;
                        item.transform.SetParent(_weaponHolder);
                        item.transform.localPosition = w.rightHandPosition;
                        item.transform.localEulerAngles = w.rightHandRotation;
                        w.ammo = itemData.Value;
                        if (equippedIds.Contains(item.networkId) || equippedWeaponIndex < 0)
                        {
                            equippedWeaponIndex = i;
                        }
                    }
                    else if (item.GetType() == typeof(Ammo))
                    {
                        Ammo a = (Ammo)item;
                        a.amount += itemData.Value;
                        if (equippedIds.Contains(item.networkId))
                        {
                            equippedAmmoIndex = i;
                        }
                    }

                    item.gameObject.SetActive(false);
                    _items.Add(item);
                    i++;
                }
            }

            if (equippedWeaponIndex >= 0 && _weapon == null)
            {
                _weaponToEquip = (Weapon)_items[equippedWeaponIndex];
                OnEquip();
            }

            if (equippedAmmoIndex >= 0)
            {
                _EquipAmmo((Ammo)_items[equippedAmmoIndex]);
            }
        }
    }

    public void ChangeWeapon(float direction)
    {
        int x = direction > 0 ? 1 : direction < 0 ? -1 : 0;
        if (x != 0 && switchingWeapon == false)
        {
            if (x > 0)
            {
                NextWeapon();
            }
            else
            {
                PreviousWeapon();
            }
        }
    }

    private void NextWeapon()
    {
        int first = -1;
        int current = -1;
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null && _items[i].GetType() == typeof(Weapon))
            {
                if (_weapon != null && _items[i].gameObject == _weapon.gameObject)
                {
                    current = i;
                }
                else
                {
                    if (current >= 0)
                    {
                        EquipWeapon((Weapon)_items[i]);
                        return;
                    }
                    else if (first < 0)
                    {
                        first = i;
                    }
                }
            }
        }
        if (first >= 0)
        {
            EquipWeapon((Weapon)_items[first]);
        }
    }

    private void PreviousWeapon()
    {
        int last = -1;
        int current = -1;
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            if (_items[i] != null && _items[i].GetType() == typeof(Weapon))
            {
                if (_weapon != null && _items[i].gameObject == _weapon.gameObject)
                {
                    current = i;
                }
                else
                {
                    if (current >= 0)
                    {
                        EquipWeapon((Weapon)_items[i]);
                        return;
                    }
                    else if (last < 0)
                    {
                        last = i;
                    }
                }
            }
        }
        if (last >= 0) EquipWeapon((Weapon)_items[last]);
    }

    public void EquipWeapon(Weapon weapon)
    {
        if (_switchingWeapon || weapon == null)
        {
            return;
        }
        if (IsOwner)
        {
            EquipWeaponServerRpc(weapon.networkId);
        }

        _weaponToEquip = weapon;
        if (weapon != null)
        {
            HolsterWeapon();
        }
        else
        {
            _switchingWeapon = true;
            _animator.SetTrigger("Equip");
        }
    }
    [ServerRpc]
    public void EquipWeaponServerRpc(string networkID)
    {
        EquipWeaponSync(networkID);
        EquipWeaponClientRpc(networkID);
    }
    [ClientRpc]
    public void EquipWeaponClientRpc(string networkID)
    {
        if (!IsOwner)
        {
            EquipWeaponSync(networkID);
        }
    }
    private void EquipWeaponSync(string networkID)
    {
        Weapon weapon = null;
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null && _items[i].networkId == networkID && _items[i].GetType() == typeof(Weapon))
            {
                weapon = (Weapon)_items[i];
                break;
            }
        }
        if (weapon != null)
        {
            EquipWeapon(weapon);
        }
        else
        {
            // Problem
        }
    }
    private void _EquipWeapon()
    {
        if (_weaponToEquip != null)
        {
            _weapon = _weaponToEquip;
            _weaponToEquip = null;
            if (_weapon.transform.parent != _weaponHolder)
            {
                _weapon.transform.SetParent(_weaponHolder);
                _weapon.transform.localPosition = _weapon.rightHandPosition;
                _weapon.transform.localEulerAngles = _weapon.rightHandRotation;
            }
            _rigManager.SetLeftHandGripData(_weapon.leftHandPosition, _weapon.leftHandRotation);
            _weapon.gameObject.SetActive(true);
            _ammo = null;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] != null && _items[i].GetType() == typeof(Ammo) && _weapon.ammoID == _items[i].id)
                {
                    _EquipAmmo((Ammo)_items[i]);
                    break;
                }
            }
        }
    }

    private void _EquipAmmo(Ammo ammo)
    {
        if (ammo != null)
        {
            return;
        }

        if (_ammo.transform.parent != transform)
        {
            _ammo.transform.SetParent(transform);
            _ammo.transform.localPosition = Vector3.zero;
            _ammo.transform.localEulerAngles = Vector3.zero;
            _ammo.gameObject.SetActive(false);
        }
    }

    public void OnEquip()
    {
        _EquipWeapon();
    }

    private void _HolsterWeapon()
    {
        if (_weapon != null)
        {
            _weapon.gameObject.SetActive(false);
            _weapon = null;
            _ammo = null;
        }
    }
    public void HolsterWeapon()
    {
        if (_switchingWeapon)
        {
            return;
        }
        if (_weapon != null)
        {
            if (IsOwner)
            {
                HolserWeaponServerRpc(_weapon.networkId);
            }
            _switchingWeapon = true;
            _animator.SetTrigger("Holster");
        }
    }
    [ServerRpc]
    public void HolserWeaponServerRpc(string weaponID)
    {
        HolsterWeaponSync(weaponID);
        HolsterWeaponClientRpc(weaponID);
    }
    [ClientRpc]
    public void HolsterWeaponClientRpc(string weaponID)
    {
        if (!IsOwner)
        {
            HolsterWeaponSync(weaponID);
        }
    }
    public void HolsterWeaponSync(string weaponID)
    {
        if (_weapon != null && _weapon.networkId == weaponID)
        {
            HolsterWeapon();
        }
        else
        {
            // Problem
        }
    }

    public void OnHolster()
    {
        _HolsterWeapon();
        if (_weaponToEquip != null)
        {
            OnEquip();
        }
    }

    public void ApplyDamage(Character shooter, Transform hit, float damage)
    {
        if (_health > 0)
        {
            _health -= damage;
            if (_health <= 0)
            {
                _health = 0;

                SetRagdollStatus(true);
                Destroy(_rigManager);
                Destroy(GetComponent<RigBuilder>());
                Destroy(_animator);
                ThirdPersonController thirdPersonController = GetComponent<ThirdPersonController>();
                if (thirdPersonController != null) Destroy(thirdPersonController);
                CharacterController characterController = GetComponent<CharacterController>();
                if (characterController != null) Destroy(characterController);
                Destroy(this);
            }
        }
    }

    public void Reload()
    {
        if (_weapon != null && !_reloading && _weapon.ammo < _weapon.clipSize && _ammo != null && _ammo.amount > 0)
        {
            if (IsOwner)
            {
                ReloadServerRpc(weapon.networkId, _ammo.networkId);
            }
            _animator.SetTrigger("Reload");
            _reloading = true;
        }
    }
    [ServerRpc]
    public void ReloadServerRpc(string weaponID, string ammoID)
    {
        ReloadSync(weaponID, ammoID);
        ReloadClientRpc(weaponID, ammoID);
    }
    [ClientRpc]
    public void ReloadClientRpc(string weaponID, string ammoID)
    {
        if (!IsOwner)
        {
            ReloadSync(weaponID, ammoID);
        }
    }
    private void ReloadSync(string weaponID, string ammoID)
    {
        if (_weapon != null && _ammo != null && _weapon.networkId == weaponID && ammo.networkId == ammoID)
        {
            Reload();
        }
    }

    public void ReloadFinish()
    {
        if (_weapon != null && _weapon.ammo < _weapon.clipSize && _ammo != null && _ammo.amount > 0)
        {
            int amount = _weapon.clipSize - _weapon.ammo;
            if (_ammo.amount < amount)
            {
                amount = _ammo.amount;
            }
            _ammo.amount -= amount;
            _weapon.ammo += amount;
        }
        _reloading = false;
    }

    public void HolsterFinished()
    {
        _switchingWeapon = false;
    }

    public void EquipFinished()
    {
        _switchingWeapon = false;
    }

    private void SetLayer(Transform root, int layer)
    {
        var children = root.GetComponentsInChildren<Transform>(true);
        foreach (var child in children)
        {
            child.gameObject.layer = layer;
        }
    }

    private float _fallTimeoutDelta;


    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        _isGrounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        _animator.SetBool("Grounded", _isGrounded);
    }

    private void FreeFall()
    {
        if (_isGrounded)
        {
            _fallTimeoutDelta = FallTimeout;

            _animator.SetBool("Freefall", false);
        }
        else
        {
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _animator.SetBool("Freefall", true);
            }
        }
    }

    public void Jump()
    {
        _animator.SetTrigger("Jump");
        JumpServerRpc();
    }

    [ServerRpc]
    public void JumpServerRpc()
    {
        _animator.SetTrigger("Jump");
        JumpClientRpc();
    }

    [ClientRpc]
    public void JumpClientRpc()
    {
        if (!IsOwner)
        {
            _animator.SetTrigger("Jump");
        }
    }
    private List<string> _shots = new List<string>();
    public bool Shoot()
    {
        if (_weapon != null && !reloading && aiming && _weapon.Shoot(this, _aimTarget))
        {
            if (IsOwner)
            {
                ShootServerRpc(_weapon.networkId);
            }
            _rigManager.ApplyWeaponKick(_weapon.handKick, _weapon.bodyKick);
            return true;
        }
        return false;
    }
    [ServerRpc]
    public void ShootServerRpc(string weaponID)
    {
        ShootSync(weaponID);
        ShootClientRpc(weaponID);
    }
    [ClientRpc]
    public void ShootClientRpc(string weaponID)
    {
        if (!IsOwner)
        {
            ShootSync(weaponID);
        }
    }
    public void ShootSync(string weaponID)
    {
        if (_weapon != null && _weapon.networkId == weaponID)
        {
            bool shoot = Shoot();
            if (!shoot)
            {
                _shots.Add(weaponID);
            }
        }
    }
    private void OnFootstep(AnimationEvent animationEvent)
    {
        /*
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
        */
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        /*
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
        }
        */
    }
}
