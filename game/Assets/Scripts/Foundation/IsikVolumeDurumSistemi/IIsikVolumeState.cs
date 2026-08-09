using System;
using UnityEngine;

/// <summary>
/// Işık/Volume Durum Sistemi'nin sistem-geneli sözleşmesi (ADR-0005 2026-08-09
/// addendum'undaki BİREBİR şekil — kopya, türetme değil). Tüketiciler yalnız bu
/// arayüze kodlar (ADR-0001); shiftId-anahtarlı çağrılar yüklü sahnelerdeki
/// kayıtlı bölgeye yönlenir.
/// </summary>
public interface IIsikVolumeState
{
    /// <summary>
    /// Kayıtlı bölgeye yönlenir; yeni bir geçiş başlattıysa true, shiftId zaten
    /// aktifse ya da kayıtlı bölge yoksa false (no-op) — TR-isik-005 idempotans.
    /// </summary>
    bool TriggerShift(string shiftId, ShiftConfig config);

    /// <summary>Aktif değilse ya da böyle bir bölge yoksa sessiz no-op (TR-isik-005).</summary>
    void RevertShift(string shiftId);

    bool IsShiftActive(string shiftId);

    /// <summary>Dar kalıcılık sorgusu — Gece/Oturum'un PersistentShiftIds yazımı için (TR-isik-013).</summary>
    bool IsShiftPersistent(string shiftId);

    /// <summary>Bölgeyi son tetikleyen config'in stinger yarıçapı; bilinmeyen shiftId → 0 (TR-isik-014).</summary>
    float GetStingerAudioRadius(string shiftId);

    /// <summary>
    /// Her gerçek durum geçişinde tam bir kez fırlar (TR-isik-019) — payload:
    /// (shiftId, yeni durum, bölge merkezi, radius); zoneCenter/radius ses
    /// sisteminin ikinci sorgu yapmadan mekansal yerleşimi için (GDD).
    /// </summary>
    event Action<string, ShiftState, Vector3, float> OnShiftStateChanged;
}
