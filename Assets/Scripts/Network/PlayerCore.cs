using Photon.Pun;
using UnityEngine;

public class PlayerCore : MonoBehaviourPunCallbacks, IPunObservable
{
    public string turnID = "X";


    [PunRPC]
    public void SetTurnID(string id)
    {
        if (GetComponent<PhotonView>().IsMine)
        {
            turnID = id;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We own this player: send the others our data
            stream.SendNext(turnID);
        }
        else
        {
            // Network player, receive data
            this.turnID = (string)stream.ReceiveNext();
        }
    }
}
