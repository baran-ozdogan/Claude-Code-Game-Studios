using UnityEngine;

/// <summary>
/// Mekanik etkisi OLMAYAN dekor etkileşim nesnesi — kapı kolu, ışık anahtarı,
/// termostat, temizlik arabası freni (GDD Core Rules'un kendi örnekleri).
/// Var olma sebebi TAMAMEN kamuflaj: yaklaşma-yavaşlaması TÜM bayraklı
/// `IInteractable`lara uygulanır, bu yüzden registry "gürültülü" kalmazsa
/// yavaşlamanın kendisi bir anı-tetikleyici dedektörüne dönüşür (TR-fpc-016, AC17).
///
/// **Neden ayrı bir işaretçi bileşen (POZİTİF işaretleme)**: `IInteractable`
/// arayüzü gerçek/decoy ayrımını yapısal olarak SIZDIRMAZ (art-bible §3.1 kamuflaj
/// gereksinimi) — yani "gerçek tipleri hariç tut" diye bir build kontrolü yazılamaz;
/// üstelik gerçek içerik tipleri (`MemoryTriggerObject`/`CarryItemPickup`) henüz
/// yazılmamış epic'lere ait. Bu yüzden decoy'lar `GetComponent&lt;DecoyInteractable&gt;()`
/// ile POZİTİF olarak işaretlenir. Bu işaretleme YALNIZ edit-time/build-time
/// içerik doğrulaması içindir — çalışma zamanında hiçbir tüketici bunu okumaz,
/// dolayısıyla oyuncuya sızabilecek bir ayrım da üretmez.
/// </summary>
public sealed class DecoyInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string _promptText = string.Empty;

    /// <summary>Her zaman Instant — decoy'un Hold'u olmaz (GDD: "minimal/tatsız bir tepki").</summary>
    public InteractionType Type => InteractionType.Instant;

    /// <summary>Instant olduğu için yok sayılır.</summary>
    public float HoldDuration => 0f;

    public bool CanInteract => true;

    public string PromptText => _promptText;

    public void OnFocusEnter() { }
    public void OnFocusExit() { }

    /// <summary>KASITLI olarak boş — decoy'un hiçbir mekanik etkisi yoktur.</summary>
    public void OnInteract() { }

    public void OnHoldProgress(float t) { }
    public void OnHoldComplete() { }
    public void OnHoldCancelled() { }
    public void OnHoldBlocked() { }

    public bool SuppressDefaultHoldFill => false;

    // Registry kaydı diğer tüm interactable'larla AYNI (manifest: yalnız OnEnable/OnDisable) —
    // decoy'un taper'a katkısı zaten kamuflajın ta kendisi.
    private void OnEnable() => InteractableRegistry.Register(this);
    private void OnDisable() => InteractableRegistry.Deregister(this);
}
