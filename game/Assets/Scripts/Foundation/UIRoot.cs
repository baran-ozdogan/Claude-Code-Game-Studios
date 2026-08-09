using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Static accessor for the shared UIDocument (ADR-0002's single "UI" persistent
/// scene; shape established by ADR-0010). Lives on the UI scene's root object
/// alongside the UIDocument. Every owning system (Etkileşim, Adaptif Ses,
/// Diyalog) queries its own named sub-tree via Instance.Root.Q&lt;VisualElement&gt;
/// in OnEnable, cached and null-checked (ADR-0002's shared-UXML mitigation).
/// </summary>
public sealed class UIRoot : MonoBehaviour
{
    // Duplicate-instance guard — same shape as every other persistent-scene
    // singleton in this project (PlayerStateProvider ADR-0003,
    // SceneTransitionManager ADR-0008, AdaptifSesController ADR-0009).
    public static UIRoot Instance { get; private set; }

    [SerializeField] private UIDocument _uiDocument;

    public VisualElement Root => _uiDocument.rootVisualElement;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Duplicate UIRoot.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
