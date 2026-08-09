/// <summary>
/// The single registration point for every build-blocking validation check.
/// System epics add their check instance to this list — a fourth independent
/// IPreprocessBuildWithReport implementation is forbidden (control manifest).
///
/// Planned owners (added by each system's own epic, not here — Story 006 ships
/// only the shell; see README.md in this folder for the ADR-referenced list):
///   TODO(epic:anlati-durum-ipucu-takibi):  ADR-0007 üçlüsü — ClueDefinition içerikleri,
///                                          orphaned requiredShiftId, Addressable "ClueRegistry" key (TR-anlati-008)
///   isik-volume-durum-sistemi (EKLENDİ, Story 006): ADR-0005 sahne-scan dörtlüsü —
///                                          Baked-light, shared-light, box-overlap, Automatic-varlık
///                                          (TR-isik-016/020/021; AC22'nin ClueDefinition çaprazı anlati epic'inde eklenecek)
///   TODO(epic:gorev-tasima-dongusu):       ADR-0013 — TaskListDef vs sahne per-round item-count cross-check (TR-gorev-018)
///   TODO(epic:ani-tetikleyici-etkilesim):  ADR-0014 6'lısı — MemoryTriggerDef/scene eşlemesi,
///                                          reachability (yerleşmemiş def), count formülü (TR-ani-tetik-007/010)
///   TODO(epic:diyalog-anlati-icerigi):     ADR-0012 — ValidateMaxCallbacksPerScene (TR-diyalog-005)
///   TODO(epic:sahne-kesmeli-anlati):       ADR-0015 — NightConfigDef tutarlılık check'leri
///   TODO(epic:birinci-sahis-kontrolcu):    TR-fpc-016 decoy check'i
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
    };
}
