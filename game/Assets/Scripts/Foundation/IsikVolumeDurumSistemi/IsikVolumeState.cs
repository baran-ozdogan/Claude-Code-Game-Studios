using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IIsikVolumeState implementasyonu (ADR-0005 addendum) — plain C# sınıf, Unity
/// yaşam döngüsü yok. shiftId → bölge routing tablosu üzerinden ince delegasyon
/// katmanı: per-zone durum bölgede kalır, burada yalnız yönlendirme + TEK event
/// raise yolu yaşar. Bölgeler OnEnable'da kendilerini kaydeder / OnDisable'da
/// silinir (ADR-0009'un scene-scoped kayıt şekli; Story 003).
/// </summary>
public sealed class IsikVolumeState : IIsikVolumeState
{
    // Sıcak-yol sorgular allocation'sız (manifest guardrail): TryGetValue,
    // LINQ/iterator yok.
    private readonly Dictionary<string, IShiftZoneHandle> _zonesByShiftId =
        new Dictionary<string, IShiftZoneHandle>();

    public event Action<string, ShiftState, Vector3, float> OnShiftStateChanged;

    public bool TriggerShift(string shiftId, ShiftConfig config)
    {
        if (shiftId != null && _zonesByShiftId.TryGetValue(shiftId, out IShiftZoneHandle zone))
        {
            return zone.TriggerShift(config);
        }

        // Bilinmeyen/yüklü-olmayan shiftId → no-op, throw yok (addendum).
        return false;
    }

    public void RevertShift(string shiftId)
    {
        if (shiftId != null && _zonesByShiftId.TryGetValue(shiftId, out IShiftZoneHandle zone))
        {
            zone.RevertShift();
        }
        // Bilinmeyen shiftId → sessiz no-op (TR-isik-005).
    }

    // Bilinmeyen shiftId sorguları Dormant-eşdeğeri default döner: false/false/0f.
    public bool IsShiftActive(string shiftId) =>
        shiftId != null && _zonesByShiftId.TryGetValue(shiftId, out IShiftZoneHandle zone) && zone.IsShiftActive;

    public bool IsShiftPersistent(string shiftId) =>
        shiftId != null && _zonesByShiftId.TryGetValue(shiftId, out IShiftZoneHandle zone) && zone.IsShiftPersistent;

    public float GetStingerAudioRadius(string shiftId) =>
        shiftId != null && _zonesByShiftId.TryGetValue(shiftId, out IShiftZoneHandle zone)
            ? zone.StingerAudioRadius
            : 0f;

    /// <summary>
    /// Bölge OnEnable'da çağırır. Duplicate shiftId yüklü sahnelerde zaten
    /// build-blocked (ADR-0005 Edge Cases); yine de olursa tanımlı davranış
    /// SON-KAZANIR + uyarı logu (Story 001 AC-7 — testte belgeli).
    /// </summary>
    internal void RegisterZone(IShiftZoneHandle zone)
    {
        if (_zonesByShiftId.TryGetValue(zone.ShiftId, out IShiftZoneHandle existing)
            && !ReferenceEquals(existing, zone))
        {
            Debug.LogWarning(
                $"[IsikVolumeDurumSistemi] Duplicate shiftId register: '{zone.ShiftId}' — " +
                "build-time doğrulama bunu engellemeliydi; son kayıt kazanır (Story 001 AC-7).");
        }

        _zonesByShiftId[zone.ShiftId] = zone;
    }

    /// <summary>
    /// Bölge OnDisable'da çağırır. Yalnız hâlâ yönlendirilen handle'sa siler —
    /// son-kazanır duplicate semantiğiyle tutarlı (bayat eski kayıt, yenisini
    /// tablodan düşüremez).
    /// </summary>
    internal void DeregisterZone(IShiftZoneHandle zone)
    {
        if (_zonesByShiftId.TryGetValue(zone.ShiftId, out IShiftZoneHandle existing)
            && ReferenceEquals(existing, zone))
        {
            _zonesByShiftId.Remove(zone.ShiftId);
        }
    }

    /// <summary>
    /// TEK raise yolu (TR-isik-019) — bir ShiftZone her gerçek durum geçişinde
    /// (OnDestroy tamamlama garantisi dahil) bunu çağırır; başka hiçbir yerden
    /// OnShiftStateChanged fırlatılmaz.
    /// </summary>
    internal void RaiseShiftStateChanged(string shiftId, ShiftState newState, Vector3 zoneCenter, float radius)
        => OnShiftStateChanged?.Invoke(shiftId, newState, zoneCenter, radius);

    /// <summary>
    /// IN-PLACE session reset (ADR-0015 rejimi — instance asla değiştirilmez):
    /// yalnız routing tablosu temizlenir; bölgeler her oturumda kendi OnEnable'ları
    /// ile yeniden kaydolur. Event'in delegate listesi BİLEREK dokunulmaz —
    /// constructor-time aboneler (GeceOturumDurumu/AnlatiDurumIpucuTakibi) ve
    /// kalıcı MonoBehaviour abonesi (AdaptifSesController) process başına bir kez
    /// bağlanır, her ResetAll'ı atlatır.
    /// </summary>
    internal void ResetOnLoad()
    {
        _zonesByShiftId.Clear();
    }
}
