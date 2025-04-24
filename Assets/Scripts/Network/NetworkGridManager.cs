using Photon.Pun;
using UnityEngine;

public class NetworkGridManager : MonoBehaviour
{
    [SerializeField] private int _width = 15;  // Reduced from 15 to 10
    [SerializeField] private int _height = 15; // Reduced from 15 to 10
    [SerializeField] private NetworkCell _cellPrefab;
    [SerializeField] private Transform _cam;
    
    // Camera movement settings
    [SerializeField] private float _cameraSpeed = 5f;
    [SerializeField] private float _cameraSmoothTime = 0.2f;
    
    private Vector3 _cameraTargetPosition;
    private Vector3 _cameraVelocity = Vector3.zero;
    private float _cameraBoundaryMargin = 0.5f;

    void Start()
    {
        EnsureGameManagerExists();
        GenerateGrid();
        InitializeCamera();
    }
    
    void Update()
    {
        HandleCameraMovement();
    }

    private void InitializeCamera()
    {
        // Set initial camera position to center of board
        _cameraTargetPosition = new Vector3(_width / 2f - 0.5f, _height / 2f - 0.5f, -10);
        _cam.transform.position = _cameraTargetPosition;
    }
    
    private void HandleCameraMovement()
    {
        if (_cam == null)
        {
            Debug.LogError("Camera reference is missing.");
            return;
        }

        if (!_cam.GetComponent<PhotonView>().IsMine)
        {
            // Allow camera movement only for the local player
            return;
        }


        // Get input for camera movement
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        
        // Apply movement to camera target position
        if (horizontalInput != 0 || verticalInput != 0)
        {
            Vector3 movement = new Vector3(horizontalInput, verticalInput, 0) * _cameraSpeed * Time.deltaTime;
            _cameraTargetPosition += movement;
            
            // Clamp to grid boundaries with margin
            _cameraTargetPosition.x = Mathf.Clamp(_cameraTargetPosition.x, _cameraBoundaryMargin, _width - _cameraBoundaryMargin - 1);
            _cameraTargetPosition.y = Mathf.Clamp(_cameraTargetPosition.y, _cameraBoundaryMargin, _height - _cameraBoundaryMargin - 1);
            
            // Keep the z position unchanged
            _cameraTargetPosition.z = -10;
        }
        
        // Smoothly move camera to target position
        _cam.transform.position = Vector3.SmoothDamp(
            _cam.transform.position, 
            _cameraTargetPosition, 
            ref _cameraVelocity, 
            _cameraSmoothTime
        );
    }

    private void EnsureGameManagerExists()
    {
        if (NetworkGameManager.Instance == null)
        {
            var existingManager = FindAnyObjectByType<NetworkGameManager>();
            if (existingManager != null)
            {
                Debug.Log("Found existing NetworkGameManager in scene");
                return;
            }
            else
            {
                Debug.Log("Creating a new NetworkGameManager GameObject");
                new GameObject("NetworkGameManager").AddComponent<NetworkGameManager>();
            }
        }
    }

    void GenerateGrid()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Not the master client, skipping grid generation.");
            return;
        }
        if (NetworkGameManager.Instance == null)
        {
            Debug.LogError("NetworkGameManager.Instance is still null after attempting to create it. Check script execution order.");
            return;
        }

        var grid = new NetworkCell[_width, _height];

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var cell = PhotonNetwork.Instantiate("NetworkCell", new Vector3(x, y), Quaternion.identity)
                    .GetComponent<NetworkCell>();
                cell.name = $"NetworkCell ({x}, {y})";

                bool isOffset = (x + y) % 2 != 0;
                cell.Init(isOffset);

                grid[x, y] = cell;
            }
        }

        // Camera is now controlled via HandleCameraMovement
        
        NetworkGameManager.Instance.SetupGrid(grid);
    }
}
