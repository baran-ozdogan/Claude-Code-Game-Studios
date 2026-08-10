/// <summary>Bir etkileşimin Instant (tek basış) mı Hold (basılı-tutma) mı olduğu (etkilesim-sistemi.md).</summary>
public enum InteractionType
{
    Instant,
    Hold,
}

/// <summary>
/// Her etkileşimli nesnenin (pickup, decoy, anı-tetikleyici) ortak sözleşmesi
/// (etkilesim-sistemi.md "Aşağı akış arayüzü" — birebir). Foundation'da yaşar
/// (ADR-0004): InteractableRegistry'nin FPC'nin yaklaşma-yavaşlama okumasına
/// katman-ihlalsiz erişebilmesi için katman aşağı taşındı (architecture.md
/// Principle 3 — "fact'in DEPOLANMASI aşağı taşınır, hesaplayan mantık değil").
/// API yüzeyinde gerçek/decoy ayrımını sızdıran hiçbir üye YOK (art-bible.md
/// §3.1 kamuflaj gereksinimi — yapısal olarak zorlanır, disiplinle değil).
/// </summary>
public interface IInteractable
{
    InteractionType Type { get; }

    /// <summary>Hold değilse yok sayılır.</summary>
    float HoldDuration { get; }

    bool CanInteract { get; }

    string PromptText { get; }

    void OnFocusEnter();
    void OnFocusExit();

    /// <summary>Yalnız Instant.</summary>
    void OnInteract();

    /// <summary>Yalnız Hold — her karede ham doğrusal t (0-1).</summary>
    void OnHoldProgress(float t);

    /// <summary>Yalnız Hold — t=1'de.</summary>
    void OnHoldComplete();

    /// <summary>Yalnız Hold — erken bırakma ya da hedef kaybı.</summary>
    void OnHoldCancelled();

    /// <summary>Yalnız Hold — Movement lock başka sistemde zaten tutuluyorken.</summary>
    void OnHoldBlocked();

    /// <summary>
    /// Yalnız Hold, varsayılan false. true dönerse crosshair'in varsayılan
    /// doldurma göstergesi hiç çizilmez — gerçek sıfır-geri-bildirim garantisi.
    /// </summary>
    bool SuppressDefaultHoldFill { get; }
}
