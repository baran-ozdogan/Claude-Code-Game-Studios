/// <summary>
/// Sahne geçişinin herkese açık durumu (GDD States and Transitions).
///
/// Normal dizi: `Idle → Preloading → Ready → Swapping → Complete → Idle`.
/// Hiçbir durum ATLANMAZ ve sırası değişmez — GDD AC-1 bunu tek tek assert eder.
///
/// `Failed`, "BU istek başarısız oldu" demektir, "yönetici kalıcı olarak bozuk"
/// DEĞİL: `onFailed` çağrıldıktan hemen sonra otomatik `Idle`'a dönülür
/// (GDD AC-11a). Bu dönüş belgelenmemiş olsaydı tek bir bozuk sahne referansı
/// oturumun geri kalanındaki HER geçişi kalıcı olarak soft-lock'layabilirdi —
/// GDD'nin kendi ifadesiyle "üretim durdurucu bir boşluk, kozmetik değil".
/// </summary>
public enum TransitionState
{
    Idle,
    Preloading,
    Ready,

    /// <summary>YALNIZCA `SetActiveScene` — sıfır-kare. Eski sahnenin unload'ı bu adımın parçası DEĞİLDİR.</summary>
    Swapping,

    Complete,

    /// <summary>Terminal ama KALICI DEĞİL — `onFailed`'den hemen sonra `Idle`'a döner.</summary>
    Failed,
}
