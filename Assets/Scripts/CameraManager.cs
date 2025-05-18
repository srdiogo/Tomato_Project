using System;
using UnityEngine;
using Cinemachine;


public class CameraManager : MonoBehaviour
{
    [SerializeField][Range(0, 5)] private float _defaultSensitivity = 1.5f; public static float defaultSensitivity {get { return singleton._defaultSensitivity; }}
    [SerializeField][Range(0, 5)] private float _aimingSensitivity = 0.5f; public static float aimingSensitivity { get { return singleton._aimingSensitivity; } }
    [SerializeField] private Camera _camera = null; public static Camera mainCamera { get { return singleton._camera; } }
    [SerializeField] private CinemachineVirtualCamera _playerCamera  = null; public static CinemachineVirtualCamera playerCamera { get {return singleton._playerCamera;} }
    [SerializeField] private CinemachineVirtualCamera _aimingCamera = null; public static CinemachineVirtualCamera aimingCamera { get { return singleton._aimingCamera; } }
    [SerializeField] private CinemachineBrain _cameraBrain = null;
    [SerializeField] private LayerMask _aimLayer;
    
    private static CameraManager _singleton = null;

    public static CameraManager singleton
    {
        get
        {
            if (_singleton == null)
            {
                _singleton = FindFirstObjectByType<CameraManager>();
            }
            return _singleton;
        }
    }
    
    private bool _aiming = false; public bool aiming { get { return _aiming; } set { _aiming = value; } }
    private Vector3 _aimTargetPoint = Vector3.zero; public Vector3 aimTargetPoint { get { return _aimTargetPoint; } set { _aimTargetPoint = value; } }
    public float sensitivity { get {return _aiming ? _aimingSensitivity : defaultSensitivity; } }

    private void Awake()
    {
        _cameraBrain.m_DefaultBlend.m_Time = 0.1f;
    }

    private void Update()
    {
        _aimingCamera.gameObject.SetActive(_aiming);
        SetAimTarget();
    }

    private void SetAimTarget()
    {
        Ray ray = _camera.ScreenPointToRay(new Vector2(Screen.width / 2f, Screen.height / 2f));
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _aimLayer))
        {
            _aimTargetPoint = hit.point;
        }
        else
        {
            _aimTargetPoint = ray.GetPoint(1000f);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_aimTargetPoint, 0.1f);
    }
#endif
}
