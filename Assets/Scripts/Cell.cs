using TMPro;
using UnityEngine;

public class Cell : MonoBehaviour
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
        if (GameManager.Instance != null && GameManager.Instance.GetCurrentPlayer() == "X")
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
        // Double-check that it's not occupied AND it's the current player's turn (or AI is calling this)
        if (!_isOccupied && GameManager.Instance != null)
        {
            // For human player, verify it's their turn before proceeding
            string currentPlayer = GameManager.Instance.GetCurrentPlayer();
            
            // If this is a human click (not AI), verify it's the human's turn
            if (currentPlayer == "O" && !IsAIMove())
            {
                Debug.Log("Not your turn!");
                return;
            }
            
            _isOccupied = true;
            _currentPlayer = currentPlayer;
            
            // Ensure the symbol is visible and properly configured
            if (_symbol != null)
            {
                // Activate the symbol object first
                _symbol.gameObject.SetActive(true);
                
                // Use a much larger font size to ensure visibility
                _symbol.fontSize = 90f; // Increased significantly
                
                // Set the text and color
                _symbol.text = _currentPlayer;
                _symbol.color = _currentPlayer == "X" ? Color.red : Color.blue;
                
                // Ensure the symbol is fully visible
                _symbol.alpha = 1f;
                
                // Position text in center of cell at proper depth
                _symbol.transform.localPosition = new Vector3(0, 0, -0.1f);
                
                // Force update the mesh with complete rebuild
                _symbol.ForceMeshUpdate(true, true);
                
                // Debug information
                Debug.Log($"Set cell text to '{_currentPlayer}' with size {_symbol.fontSize}");
            }

            GameManager.Instance.ProcessTurn(this);
        }
    }

    // Helper method to determine if this move is being made by the AI
    private bool IsAIMove()
    {
        // Get the call stack to check if AIBot.MakeMove is calling this function
        System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
        
        // Check if any frame in the call stack is from the AIBot class
        for (int i = 0; i < stackTrace.FrameCount; i++)
        {
            var method = stackTrace.GetFrame(i).GetMethod();
            if (method.DeclaringType != null && method.DeclaringType.Name == "AIBot")
            {
                return true;
            }
        }
        
        return false;
    }

    public string GetSymbol()
    {
        return _isOccupied ? _currentPlayer : "";
    }

    public void MakeTemporaryMove(string player)
    {
        _isOccupied = true;
        _currentPlayer = player;
        
        if (_symbol != null)
        {
            // Activate and make visible
            _symbol.gameObject.SetActive(true);
            _symbol.alpha = 1f;
            
            // Set large text size to ensure visibility
            _symbol.fontSize = 30f;
            
            // Set text and color
            _symbol.text = player;
            _symbol.color = player == "X" ? Color.red : Color.blue;
            
            // Ensure text is centered at proper depth
            _symbol.transform.localPosition = new Vector3(0, 0, -0.1f);
            
            // Force complete update
            _symbol.ForceMeshUpdate(true, true);
        }
    }

    public void UndoTemporaryMove()
    {
        _isOccupied = false;
        _currentPlayer = "";
        
        if (_symbol != null)
        {
            _symbol.text = "";
            _symbol.gameObject.SetActive(false);
        }
    }

    public void ResetCell()
    {
        _isOccupied = false;
        _currentPlayer = "";
        _symbol.text = "";
        _symbol.gameObject.SetActive(false);
        _highlight.SetActive(false);
        
        // Reset color to original
        _spriteRenderer.color = _isOffset ? _offsetColor : _baseColor;
    }
}
