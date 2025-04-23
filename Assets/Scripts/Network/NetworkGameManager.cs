using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System;

public class NetworkGameManager : MonoBehaviour
{
    public static NetworkGameManager Instance;

    private string _currentPlayer = "X"; // Start with player X
    private int _gridSize = 15; // Changed from 15 to 10
    private NetworkCell[,] _grid;
    private bool _gameOver = false;

    private AIBot _aiBot;

    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private TextMeshProUGUI _gameOverText;
    [SerializeField] private Button _restartButton;
    
    // Add camera movement instructions UI
    [SerializeField] private TextMeshProUGUI _instructionsText;

    public NetworkCell[,] Grid => _grid;

    // Event for when the game ends
    public event Action<string> OnGameOver;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _aiBot = gameObject.AddComponent<AIBot>();
            
            // Create UI elements if they don't exist
            InitializeUI();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeUI()
    {
        if (_gameOverPanel == null)
        {
            // Find or create canvas
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            // Create GameOver Panel - use proper UI element creation
            _gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform));
            _gameOverPanel.transform.SetParent(canvas.transform, false);
            
            RectTransform panelRect = _gameOverPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            // Create a centered container for the dialog - FIXED: ensure RectTransform is added on creation
            GameObject dialogContainer = new GameObject("DialogContainer", typeof(RectTransform));
            dialogContainer.transform.SetParent(_gameOverPanel.transform, false);
            
            RectTransform containerRect = dialogContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(300, 200);
            
            // Add background image to container
            Image containerBg = dialogContainer.AddComponent<Image>();
            containerBg.color = new Color(0, 0, 0, 0.8f);
            
            // Create text - ensure proper component creation
            GameObject textObj = new GameObject("GameOverText", typeof(RectTransform));
            textObj.transform.SetParent(dialogContainer.transform, false);
            
            _gameOverText = textObj.AddComponent<TextMeshProUGUI>();
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0.7f);
            textRect.anchorMax = new Vector2(1, 0.9f);
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);
            _gameOverText.alignment = TextAlignmentOptions.Center;
            _gameOverText.fontSize = 24;
            
            // Create restart button - ensure proper component creation
            GameObject buttonObj = new GameObject("RestartButton", typeof(RectTransform));
            buttonObj.transform.SetParent(dialogContainer.transform, false);
            
            _restartButton = buttonObj.AddComponent<Button>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 1f);
            
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.3f, 0.2f);
            buttonRect.anchorMax = new Vector2(0.7f, 0.4f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            
            // Create button text - ensure proper component creation
            GameObject buttonTextObj = new GameObject("Text", typeof(RectTransform));
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            
            TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
            buttonText.text = "Restart Game";
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontSize = 18;
            
            // Add button listener
            _restartButton.onClick.AddListener(RestartGame);
            
            _gameOverPanel.SetActive(false);
        }
        
        // Add instructions for camera movement
        if (_instructionsText == null)
        {
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                // Create instructions text - ensure proper component creation
                GameObject instructionsObj = new GameObject("InstructionsText", typeof(RectTransform));
                instructionsObj.transform.SetParent(canvas.transform, false);
                
                _instructionsText = instructionsObj.AddComponent<TextMeshProUGUI>();
                RectTransform textRect = instructionsObj.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0, 1);
                textRect.anchorMax = new Vector2(1, 1);
                textRect.pivot = new Vector2(0.5f, 1);
                textRect.offsetMin = new Vector2(10, -50);
                textRect.offsetMax = new Vector2(-10, -10);
                
                _instructionsText.text = "Use WASD or Arrow Keys to move the camera";
                _instructionsText.alignment = TextAlignmentOptions.Center;
                _instructionsText.fontSize = 14;
                _instructionsText.color = Color.white;
            }
        }
    }

    public void SetupGrid(NetworkCell[,] grid)
    {
        _grid = grid;
        _gameOver = false;
        
        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(false);
        }
        
        // Reset the first player to X
        _currentPlayer = "X";
    }

    public string GetCurrentPlayer()
    {
        return _currentPlayer;
    }

    public void ProcessTurn(NetworkCell cell)
    {
        if (_gameOver) return;

        if (CheckWin(cell))
        {
            EndGame($"{_currentPlayer} wins!");
            return;
        }

        if (CheckDraw())
        {
            EndGame("Game is a draw!");
            return;
        }

        // Switch player
        _currentPlayer = _currentPlayer == "X" ? "O" : "X";

        //TODO: Add PvP logic here
    }

    private void EndGame(string message)
    {
        _gameOver = true;
        Debug.Log(message);
        
        // Show the game over UI
        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(true);
            if (_gameOverText != null)
            {
                _gameOverText.text = message;
            }
        }
        
        // Trigger event
        OnGameOver?.Invoke(message);
    }

    public void RestartGame()
    {
        // Clear the board
        foreach (var cell in _grid)
        {
            cell.ResetCell();
        }
        
        // Reset game state
        _gameOver = false;
        _currentPlayer = "X";
        
        // Hide the game over panel
        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(false);
        }
        
        Debug.Log("Game restarted");
    }

    private bool CheckWin(NetworkCell cell)
    {
        Vector2Int pos = GetCellPosition(cell);
        if (pos.x == -1 && pos.y == -1) return false;

        return CheckDirection(pos, Vector2Int.right) || 
               CheckDirection(pos, Vector2Int.up) || 
               CheckDirection(pos, new Vector2Int(1, 1)) || 
               CheckDirection(pos, new Vector2Int(1, -1));
    }

    private bool CheckDirection(Vector2Int startPos, Vector2Int direction)
    {
        string player = _grid[startPos.x, startPos.y].GetSymbol();
        if (string.IsNullOrEmpty(player)) return false;

        int count = 1;

        // Forward direction
        for (int i = 1; i < 5; i++)
        {
            int x = startPos.x + i * direction.x;
            int y = startPos.y + i * direction.y;

            if (IsInBounds(x, y) && _grid[x, y].GetSymbol() == player)
                count++;
            else
                break;
        }

        // Backward direction
        for (int i = 1; i < 5; i++)
        {
            int x = startPos.x - i * direction.x;
            int y = startPos.y - i * direction.y;

            if (IsInBounds(x, y) && _grid[x, y].GetSymbol() == player)
                count++;
            else
                break;
        }

        return count >= 5;
    }

    private bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < _gridSize && y >= 0 && y < _gridSize;
    }

    private bool CheckDraw()
    {
        foreach (var cell in _grid)
        {
            if (string.IsNullOrEmpty(cell.GetSymbol()))
                return false;
        }
        return true;
    }

    private Vector2Int GetCellPosition(NetworkCell cell)
    {
        for (int x = 0; x < _gridSize; x++)
        {
            for (int y = 0; y < _gridSize; y++)
            {
                if (_grid[x, y] == cell)
                    return new Vector2Int(x, y);
            }
        }
        return new Vector2Int(-1, -1);
    }
}
