using Photon.Pun;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject _player;
    [SerializeField] private GameObject _gameManager;
    [SerializeField] private GameObject _gridManager;

    private void Start()
    {
        if (_gameManager == null)
        {
            Debug.LogError("GameManager reference is missing.");
            return;
        }
        if (_gridManager == null)
        {
            Debug.LogError("GridManager reference is missing.");
            return;
        }

        // Check if master
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Instantiate(_gameManager.name, new Vector3(0, 0, 0), Quaternion.identity, 0);
            PhotonNetwork.Instantiate(_gridManager.name, new Vector3(0, 0, 0), Quaternion.identity, 0);
        }

        // Spawn player
        GameObject player = PhotonNetwork.Instantiate(_player.name, new Vector3(0, 0, 0), Quaternion.identity, 0);

        // Count players
        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;

    }
}
