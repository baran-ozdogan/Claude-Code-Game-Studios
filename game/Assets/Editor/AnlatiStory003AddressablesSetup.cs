using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// One-shot batch setup for anlati Story 003 — the project's FIRST Addressables
/// consumer (ADR-0007). Creates the Addressables settings if missing, creates an
/// empty `ClueRegistry` asset, and marks it Addressable under the fixed key
/// `"ClueRegistry"` that `EnsureRegistryLoaded()` resolves. Run via:
///   Unity.exe -batchmode -quit -executeMethod AnlatiStory003AddressablesSetup.Run
/// Deletable once the settings + asset are committed.
///
/// The registry ships EMPTY — `ClueDefinition` content is authored later
/// (content/level-design phase). An empty registry is a valid state: no clue can
/// complete, which is exactly correct before any clue exists.
/// </summary>
public static class AnlatiStory003AddressablesSetup
{
    private const string RegistryAssetPath = "Assets/Settings/ClueRegistry.asset";
    private const string AddressableKey = "ClueRegistry";

    public static void Run()
    {
        // GetSettings(true) creates AddressableAssetSettings + the default group
        // if the project has never been initialized (this project has not).
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        if (settings == null)
        {
            Debug.LogError("[AnlatiStory003AddressablesSetup] AddressableAssetSettings oluşturulamadı.");
            EditorApplication.Exit(1);
            return;
        }

        // AssetDatabase üzerinden — `Directory.CreateDirectory` göreli yolla
        // AssetDatabase'i baypas eder ve `CreateAsset` öncesi bir Refresh gerektirir.
        // (Bu betik başkalarının kopyalayacağı bir şablon; doğru form burada kalsın.)
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
        }

        var registry = AssetDatabase.LoadAssetAtPath<ClueRegistry>(RegistryAssetPath);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<ClueRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryAssetPath);
            AssetDatabase.SaveAssets();
        }

        string guid = AssetDatabase.AssetPathToGUID(RegistryAssetPath);
        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
        if (entry == null)
        {
            Debug.LogError("[AnlatiStory003AddressablesSetup] ClueRegistry Addressable girdisi oluşturulamadı.");
            EditorApplication.Exit(1);
            return;
        }

        entry.address = AddressableKey;
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, postEvent: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AnlatiStory003AddressablesSetup] '{RegistryAssetPath}' Addressable, anahtar='{entry.address}'.");
    }
}
