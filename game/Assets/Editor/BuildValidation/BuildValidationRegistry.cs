/// <summary>
/// The single registration point for every build-blocking validation check.
/// System epics add their check instance to this list — a fourth independent
/// IPreprocessBuildWithReport implementation is forbidden (control manifest).
///
/// Planned owners (added by each system's own epic, not here — Story 006 ships
/// only the shell; see README.md in this folder for the ADR-referenced list):
///   anlati-durum-ipucu-takibi (EKLENDİ, Story 004): ADR-0007 dörtlüsü — Addressable "ClueRegistry"
///                                          key, boş RequiredShiftIds, çift clueId (TR-anlati-008/009)
///                                          + AC22 çaprazı (TR-isik-021, isik Story 006'dan devralındı).
///                                          NOT: orphaned requiredShiftId BU LİSTEDE DEĞİL — o non-blocking
///                                          bir uyarı, IBuildCheck değil (Story 005, ADR-0007).
///   isik-volume-durum-sistemi (EKLENDİ, Story 006): ADR-0005 sahne-scan dörtlüsü —
///                                          Baked-light, shared-light, box-overlap, Automatic-varlık
///                                          (TR-isik-016/020/021)
///   TODO(epic:gorev-tasima-dongusu):       ADR-0013 — TaskListDef vs sahne per-round item-count cross-check (TR-gorev-018)
///   TODO(epic:ani-tetikleyici-etkilesim):  ADR-0014 6'lısı — MemoryTriggerDef/scene eşlemesi,
///                                          reachability (yerleşmemiş def), count formülü (TR-ani-tetik-007/010)
///   TODO(epic:diyalog-anlati-icerigi):     ADR-0012 — ValidateMaxCallbacksPerScene (TR-diyalog-005)
///   TODO(epic:sahne-kesmeli-anlati):       ADR-0015 — NightConfigDef tutarlılık check'leri
///   birinci-sahis-kontrolcu (EKLENDİ, Story 006): TR-fpc-016 — her MVP alanı sahnesinde
///                                          en az bir DecoyInteractable (GDD AC17 kamuflaj gereksinimi)
/// </summary>
internal static class BuildValidationRegistry
{
    internal static readonly IBuildCheck[] Checks =
    {
        // isik-volume Story 006 — ADR-0005 sahne-scan dörtlüsü:
        new IsikVolumeBakedLightCheck(),
        new IsikVolumeSharedLightCheck(),
        new IsikVolumeBoxOverlapCheck(),
        new IsikVolumeAutomaticPresenceCheck(),

        // birinci-sahis-kontrolcu Story 006 — TR-fpc-016 decoy içerik gereksinimi:
        new FpcDecoyPresenceCheck(),

        // anlati-durum-ipucu-takibi Story 004 — ADR-0007 dörtlüsü. Parametresiz
        // ctor'lar üretim dikişlerine zincirlenir (testler enjekte eden ctor'u
        // kullanır) — bu dizi düz bir literal olarak KALIR.
        new AnlatiClueRegistryKeyCheck(),
        new AnlatiRequiredShiftIdsCheck(),
        new AnlatiUniqueClueIdCheck(),
        new AnlatiAutomaticZoneNotClueBearingCheck(),
    };
}
