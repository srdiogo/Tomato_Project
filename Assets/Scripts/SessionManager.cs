using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using LitJson;
using Unity.Networking.Transport;

public class SessionManager : NetworkBehaviour
{
    private static SessionManager _singleton;

    public static SessionManager singleton
    {
        get
        {
            if (_singleton == null)
            {
                _singleton = FindFirstObjectByType<SessionManager>();
            }

            return _singleton;
        }
    }
    public void StartServer()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.StartServer();
    }

    private void OnClientConnected(ulong clientId)
    {
        ulong[] target = new ulong[1];
        target[0] = clientId;
        ClientRpcParams clientRpcParams = default;
        OnClientConnectedClientRpc(clientRpcParams);
    }
    
    [ClientRpc]
    private void OnClientConnectedClientRpc(ClientRpcParams rpcParams = default)
    {
        // ToDo: passar id da conta
        long accountID = 0;
        SpawnCharacterServerRpc(accountID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnCharacterServerRpc(long accountID, ServerRpcParams serverRpcParams = default)
    {
        Character prefab = PrefabManager.singleton.GetCharacterPrefab("Bot");
        if (prefab != null)
        {
            Vector3 position = new Vector3(UnityEngine.Random.Range(-5f, 5f), 0f, UnityEngine.Random.Range(-5f, 5f));
            
            Character character = Instantiate(prefab, position, Quaternion.identity);
            character.GetComponent<NetworkObject>().SpawnWithOwnership(serverRpcParams.Receive.SenderClientId);

            Dictionary<string, int> items = new Dictionary<string, int> { { "AKM", 1 }, { "AWP", 1 }, { "h", 1000 } };
            List<string> itemsId = new List<string>();
            for (int i = 0; i < items.Count ; i++)
            {
                itemsId.Add(System.Guid.NewGuid().ToString());
            }
            
            string itemsJson = JsonMapper.ToJson(items);
            string itemsIdJson = JsonMapper.ToJson(itemsId);
            
            character.InitializeServer(items, itemsId, serverRpcParams.Receive.SenderClientId);
            character.InitializeClientRpc(itemsJson, itemsIdJson, serverRpcParams.Receive.SenderClientId);
        }
    }
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }
}
