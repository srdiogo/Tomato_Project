using UnityEngine;

public class Item : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private string _id = ""; public string id { get { return _id; } }
    private string _networkId = ""; public string networkId { get { return _networkId; }  set { _networkId = value; } }
}
