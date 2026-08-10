using System;
using UnityEngine;

/// <summary>Hareket kilidinin kapsamı — hangi girdi kanallarının donacağı (birinci-sahis-kontrolcu.md).</summary>
public enum MovementLockScope
{
    /// <summary>Hem Move hem Look donar — kamera dışarıdan sürülebilir.</summary>
    Full,

    /// <summary>Yalnız Move donar, Look oyuncu kontrolünde kalır.</summary>
    MoveOnly,
}

/// <summary>
/// Oyuncunun dışa açtığı salt-okunur durum sözleşmesi + referans-sayımlı hareket
/// kilidi (ADR-0003 — birebir). `PlayerStateProvider` implement eder;
/// `FirstPersonController` (Story 003) alanları yazar, tüketiciler yalnız bu
/// arayüzü okur. Bu projenin en çok okunan Foundation sözleşmesi — Etkileşim,
/// Asansör, Görev/Taşıma, Anı-Tetikleyici, Sahne Kesmeli Anlatı hepsi buna bağlı.
/// </summary>
public interface IPlayerState
{
    Transform EyeCamera { get; }
    Vector3 Velocity { get; }
    bool IsGrounded { get; }
    bool MovementLocked { get; }
    bool IsCarrying { get; }

    /// <summary>MovementLocked'ın ucuz alias'ı — Etkileşim'in Hold-öncesi mutual-exclusion ön-kontrolü için.</summary>
    bool IsLocked { get; }

    event Action MovementLockChanged;
}
