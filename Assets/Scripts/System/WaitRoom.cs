using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaitRoom : MonoBehaviourPunCallbacks
{
    public TMPro.TMP_Text playerCountText;
    public TMPro.TMP_Text winnerText;

    public int minPlayers = 2;
    public int maxPlayers = 2;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (playerCountText != null)
        {
            playerCountText.text = PhotonNetwork.CurrentRoom.PlayerCount + " / " + maxPlayers;
        }

        if (winnerText != null)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("winner"))
            {
                winnerText.text = PhotonNetwork.CurrentRoom.CustomProperties["winner"].ToString() + " wins!";
            }
            else
            {
                winnerText.text = "";
            }
        }
    }

    public void StartGame()
    {
        if (!PhotonNetwork.OfflineMode)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (PhotonNetwork.CurrentRoom.PlayerCount >= minPlayers)
                {
                    PhotonNetwork.LoadLevel("Game");
                }
            }
        }
        else
        {
            PhotonNetwork.LoadLevel("Game");
        }
    }

    public void Back()
    {
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LoadLevel("MainMenu");
    }
}
