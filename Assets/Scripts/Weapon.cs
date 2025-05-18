using System;
using UnityEngine;

public class Weapon : Item
{
    [Header("Settings")] [SerializeField] private Handle _type = Handle.TwoHanded; public Handle type { get { return _type; } }
    [SerializeField] private float _damage = 1f;
    [SerializeField] private float _fireRate = 0.2f;
    [SerializeField] private int _clipSize = 30;

    [SerializeField] private float _handKick = 5f; public float handKick { get { return _handKick; } }
    [SerializeField] private float _bodyKick = 3f; public float bodyKick { get { return _bodyKick; } }
    [SerializeField] private Vector3 _leftHandPosition = Vector3.zero; public Vector3 leftHandPosition { get { return _leftHandPosition; } }
    [SerializeField] private Vector3 _leftHandRotation = Vector3.zero; public Vector3 leftHandRotation { get { return _leftHandRotation; } }
    [SerializeField] private Vector3 _rightHandPosition = Vector3.zero; public Vector3 rightHandPosition { get { return _rightHandPosition; } }
    [SerializeField] private Vector3 _rightHandRotation = Vector3.zero; public Vector3 rightHandRotation { get { return _rightHandRotation; } }
    public enum Handle
    {
        OneHanded = 1, TwoHanded = 2
    }
    
    private float _fireTimer = 0f;

    private void Awake()
    {
        _fireTimer = Time.realtimeSinceStartup;
    }

    public bool Shoot(Character character, Vector3 target)
    {
        float passedTime = Time.realtimeSinceStartup - _fireTimer;

        if (passedTime >= _fireRate)
        {
            _fireTimer = Time.realtimeSinceStartup;
            //instanciar bala
            return true;
        }
        return false;
    }
}
