using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class TMPResourcesLoader : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset _defaultFont;
    
    // Create a singleton instance to easily access the font
    public static TMPResourcesLoader Instance { get; private set; }
    
    // Expose the font asset for other scripts to use
    public TMP_FontAsset DefaultFont => _defaultFont;
    
    void Awake()
    {
        // Set up singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(PreloadTMPResources());
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    IEnumerator PreloadTMPResources()
    {
        // Wait one frame to ensure everything is initialized
        yield return null;
        
        Debug.Log("Preloading TextMeshPro resources");
        
        // First ensure TMP_Settings is loaded
        var settings = Resources.Load<TMP_Settings>("TMP Settings");
        if (settings == null)
            Debug.LogError("TMP Settings could not be loaded - TextMeshPro Essential Resources may not be imported!");
        
        if (_defaultFont == null)
        {
            Debug.LogWarning("No default font assigned to TMPResourcesLoader! Loading default font from resources.");
            
            // Try multiple ways to load the default font
            _defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            
            if (_defaultFont == null)
                _defaultFont = TMP_Settings.defaultFontAsset;
                
            if (_defaultFont == null)
            {
                var allFonts = Resources.LoadAll<TMP_FontAsset>("");
                if (allFonts != null && allFonts.Length > 0)
                    _defaultFont = allFonts[0];
            }
            
            if (_defaultFont == null)
                Debug.LogError("Failed to load any font asset. Text will not display correctly.");
            else
                Debug.Log("Loaded default font: " + _defaultFont.name);
        }
        
        // Create test characters with proper setup
        GameObject tempObject = new GameObject("TMP_Setup_Test");
        TextMeshPro testText = tempObject.AddComponent<TextMeshPro>();
        testText.font = _defaultFont;
        testText.fontSize = 36;
        testText.text = "XO";
        testText.alignment = TextAlignmentOptions.Center;
        testText.color = Color.white;
        testText.enableAutoSizing = false;
        
        // Force mesh update and wait
        testText.ForceMeshUpdate(true, true);
        yield return null;
        
        // Test that it rendered correctly
        Debug.Log($"Test text has {testText.textInfo.characterCount} visible characters");
        
        // Update all existing TMP components
        UpdateAllTextComponents();
        
        // Destroy test object
        Destroy(tempObject, 0.1f);
        
        Debug.Log("TextMeshPro resources preloaded successfully");
    }
    
    // Update all TMP components in the scene with the default font
    public void UpdateAllTextComponents()
    {
        if (_defaultFont == null) return;
        
        // Update all TextMeshPro components
        TextMeshPro[] textMeshes = FindObjectsOfType<TextMeshPro>(true);
        foreach (TextMeshPro text in textMeshes)
        {
            text.font = _defaultFont;
            text.ForceMeshUpdate();
        }
        
        // Update all TextMeshProUGUI components
        TextMeshProUGUI[] textMeshesUI = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in textMeshesUI)
        {
            text.font = _defaultFont;
            text.ForceMeshUpdate();
        }
    }
    
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Update text components when a new scene loads
        StartCoroutine(DelayedTextUpdate());
    }
    
    IEnumerator DelayedTextUpdate()
    {
        yield return null; // Wait one frame
        UpdateAllTextComponents();
    }
    
    // Add a new public method to apply a font to a specific text component
    public void ApplyFontToComponent(TMP_Text textComponent)
    {
        if (textComponent == null || _defaultFont == null) return;
        
        textComponent.font = _defaultFont;
        
        // Fix common TMP issues
        textComponent.enableWordWrapping = false;
        textComponent.overflowMode = TextOverflowModes.Overflow;
        
        // Make sure material is updated
        textComponent.UpdateMeshPadding();
        textComponent.ForceMeshUpdate(true, true);
    }
}
