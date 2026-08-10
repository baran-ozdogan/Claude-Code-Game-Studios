using UnityEngine;

/// <summary>
/// Paylaşılan mesafe-tabanlı head-bob/footstep faz akümülatörü (GDD "Head-bob/ayak
/// sesi senkronu", TR-fpc-014). SAAT ZAMANINA değil KAT EDİLEN MESAFEYE göre ilerler —
/// API yüzeyi bilerek `deltaTime` almaz, böylece "zaman geçse de mesafe delta'sı
/// sıfırken faz değişmez" yapısal olarak garantidir (runtime dallanmaya gerek yok).
/// Tek paylaşılan `Phase` — head-bob eğrisi VE ayak sesi tetiklemesi aynı kaynaktan
/// okur, ayrı zamanlayıcılar yok (desync yapısal olarak imkânsız). Erişilebilirlik
/// slider'ından (S) TAMAMEN bağımsız — bu sınıf S parametresini hiç görmez; S yalnız
/// `MovementMathFormulas.HeadBobAmplitude`'ın ÇIKTISINI ölçekler (manifest guardrail,
/// design/ux/accessibility-requirements.md §5). Story 003'ün sürücüsü her karede
/// gerçek pozisyon deltasının büyüklüğünü besler; MonoBehaviour yaşam döngüsü yok,
/// saf C# — isik-volume'un `ShiftProgressMachine` ayrımıyla aynı desen.
/// </summary>
public sealed class MovementPhaseAccumulator
{
    /// <summary>Kat edilen toplam mesafe (m) — head-bob eğrisi VE ayak sesi tetiklemesinin TEK paylaşılan kaynağı.</summary>
    public float Phase { get; private set; }

    /// <summary>
    /// Bir kare ilerlet: Phase += mesafe deltasının büyüklüğü. Negatif girdi guard'lı
    /// (büyüklük her zaman ≥0 olmalı — çağıran `Vector3.Distance`/`.magnitude` besler).
    /// </summary>
    public void Advance(float distanceDelta)
    {
        Phase += Mathf.Max(0f, distanceDelta);
    }
}
