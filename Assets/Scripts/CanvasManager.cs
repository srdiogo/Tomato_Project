using System;
using UnityEngine;
using UnityEngine.UI;
public class CanvasManager : MonoBehaviour
{
    [SerializeField] private Button _serverButton = null;
    [SerializeField] private Button _clientButton = null;

    private void Start()
    {
        _serverButton.onClick.AddListener(StartServer);
        _clientButton.onClick.AddListener(StartClient);    }

    private void StartServer()
    {
        _serverButton.gameObject.SetActive(false);
        _clientButton.gameObject.SetActive(false);
        SessionManager.singleton.StartServer();
    }

    private void StartClient()
    {
        _serverButton.gameObject.SetActive(false);
        _clientButton.gameObject.SetActive(false);
        SessionManager.singleton.StartClient();
    }
}
