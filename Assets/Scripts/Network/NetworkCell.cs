using Photon.Pun;
using TMPro;
using UnityEngine;
using Photon.Realtime;


public class NetworkCell : MonoBehaviour, IPunObservable
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Color _baseColor, _offsetColor;
    [SerializeField] private GameObject _highlight;
    [SerializeField] private TextMeshPro _symbol;

    // New field to reference TMPro font asset - assign in inspector
    [SerializeField] private TMP_FontAsset _fontAsset;

    private bool _isOccupied = false;
    private string _currentPlayer = "";
    private bool _isOffset;

    // Cache original properties
    private Vector3 _symbolScale;
    private Color _symbolBaseColor;

    public void Init(bool isOffset)
    {
        _isOffset = isOffset;
        _spriteRenderer.color = isOffset ? _offsetColor : _baseColor;
        
        // Initialize and validate TextMeshPro component
        if (_symbol != null)
        {
            _symbolScale = _symbol.transform.localScale;
            _symbolBaseColor = _symbol.color;
            
            // Critical TMP setup to ensure visibility
            _symbol.text = ""; // Start with empty text
            
            // Make font significantly larger to account for canvas scaling
            _symbol.fontSize = 90f; // Increased from 5f to 20f to make it visible
            
            // Set appropriate alignment and overflow settings
            _symbol.enableAutoSizing = false;
            _symbol.textWrappingMode = TextWrappingModes.NoWrap;
            _symbol.overflowMode = TextOverflowModes.Overflow;
            _symbol.alignment = TextAlignmentOptions.Center;
            _symbol.verticalAlignment = VerticalAlignmentOptions.Middle;
            _symbol.horizontalAlignment = HorizontalAlignmentOptions.Center;
            
            // Ensure proper material and font
            if (_symbol.fontSharedMaterial == null || _symbol.font == null)
            {
                Debug.LogWarning("TMP Material or Font missing - attempting to fix on " + gameObject.name);
                _symbol.font = TMP_Settings.defaultFontAsset;
            }
            
            // Position the text properly in the cell (centered)
            _symbol.transform.localPosition = new Vector3(0, 0, -0.1f);
            
            // Update material and make sure it's visible
            _symbol.UpdateMeshPadding();
            _symbol.enabled = true;
            _symbol.gameObject.SetActive(true);
            _symbol.alpha = 0f; // Start invisible
            
            // Force a mesh update to initialize properly
            _symbol.ForceMeshUpdate(true, true);
            
            // Now that it's initialized, hide the GameObject
            _symbol.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("TextMeshPro component not assigned on Cell!");
        }
    }

    void OnMouseEnter()
    {
        if (!_isOccupied)
        {
            _highlight.SetActive(true);
        }
    }

    void OnMouseExit()
    {
        _highlight.SetActive(false);
    }

    void OnMouseDown()
    {
        // Only call MakeMove if it's the player's turn
        if (NetworkGameManager.Instance != null)
        {
            MakeMove();
        }
        else
        {
            // Optional: Add a visual or audio cue that it's not the player's turn
            Debug.Log("Not your turn!");
        }
    }

    public void MakeMove()
    {
        // Double-check that it's not occupied AND it's the current player's turn
        if (!_isOccupied && NetworkGameManager.Instance != null)
        {
            // For PvP, get the current player from the game manager
            string currentPlayer = NetworkGameManager.Instance.GetCurrentPlayer();
            PlayerCore thisPlayer = null;
            PlayerCore[] pc = FindObjectsOfType<PlayerCore>();
            foreach (PlayerCore p in pc)
            {
                if (p.gameObject.GetComponent<PhotonView>().IsMine)
                {
                    thisPlayer = p;
                    break;
                }
            }

            // Check if the current player is the same as this player's turn
            if (thisPlayer.turnID != currentPlayer)
            {
                Debug.Log("Not your turn!");
                return;
            }

            // Call the RPC to set the cell
            GetComponent<PhotonView>().RPC("SetCell", RpcTarget.AllBuffered, currentPlayer, true);

            //// Ensure the symbol is visible and properly configured
            //if (_symbol != null)
            //{
            //    // Activate the symbol object first
            //    _symbol.gameObject.SetActive(true);
                
            //    // Use a much larger font size to ensure visibility
            //    _symbol.fontSize = 90f; // Increased significantly
                
            //    // Set the text and color
            //    _symbol.text = _currentPlayer;
            //    _symbol.color = _currentPlayer == "X" ? Color.red : Color.blue;
                
            //    // Ensure the symbol is fully visible
            //    _symbol.alpha = 1f;
                
            //    // Position text in center of cell at proper depth
            //    _symbol.transform.localPosition = new Vector3(0, 0, -0.1f);
                
            //    // Force update the mesh with complete rebuild
            //    _symbol.ForceMeshUpdate(true, true);
                
            //    // Debug information
            //    Debug.Log($"Set cell text to '{_currentPlayer}' with size {_symbol.fontSize}");
            //}

            NetworkGameManager.Instance.ProcessTurn(this);
        }
    }

    [PunRPC]
    public void SetCell(string playerID, bool isOccupied)
    {
        _isOccupied = isOccupied;
        _currentPlayer = playerID;
        if (_symbol != null)
        {
            _symbol.text = _currentPlayer;
            _symbol.color = _currentPlayer == "X" ? Color.red : Color.blue;
            _symbol.gameObject.SetActive(_isOccupied);
            _symbol.alpha = 1f; // Ensure it's fully visible
        }
        else
        {
            Debug.LogError("TextMeshPro component not assigned on Cell!");
        }
    }

    public string GetSymbol()
    {
        return _isOccupied ? _currentPlayer : "";
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We own this player: send the others our data
            object[] data = new object[3];
            data[0] = _isOccupied;
            data[1] = _currentPlayer;
            data[2] = _isOffset;
            stream.SendNext(data);          
        }
        else { 
            object[] receivedData = (object[])stream.ReceiveNext();
            _isOccupied = (bool)receivedData[0];
            _currentPlayer = (string)receivedData[1];
            _isOffset = (bool)receivedData[2];
            // Update the cell's visual state based on received data
            if (_symbol != null)
            {
                _symbol.text = _currentPlayer;
                _symbol.color = _currentPlayer == "X" ? Color.red : Color.blue;
                _symbol.gameObject.SetActive(_isOccupied);
                _symbol.alpha = 1f; // Ensure it's fully visible
            }
            else
            {
                Debug.LogError("TextMeshPro component not assigned on Cell!");
            }
        }
    }
}