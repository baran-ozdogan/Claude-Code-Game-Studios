/// <summary>
/// The single registration point for every build-blocking validation check.
/// System epics add their check instance to this list — a fourth independent
/// IPreprocessBuildWithReport implementation is forbidden (control manifest).
///
/// Planned owners (added by each system's own epic, not here — Story 006 ships
/// only the shell; see README.md in this folder for the ADR-referenced list):
///   TODO(epic:anlati-durum-ipucu-takibi):  ADR-0007 üçlüsü — ClueDefinition içerikleri,
///                                          orphaned requiredShiftId, Addressable "ClueRegistry" key (TR-anlati-008)
///   TODO(epic:isik-volume-durum-sistemi):  ADR-0005 sahne-scan seti — Volume-trigger-box overlap,
///                                          Baked-light, shared-light (TR-isik-016/021)
///   TODO(epic:gorev-tasima-dongusu):       ADR-0013 — TaskListDef vs sahne per-round item-count cross-check (TR-gorev-018)
///   TODO(epic:ani-tetikleyici-etkilesim):  ADR-0014 6'lısı — MemoryTriggerDef/scene eşlemesi,
///                                          reachability (yerleşmemiş def), count formülü (TR-ani-tetik-007/010)
///   TODO(epic:diyalog-anlati-icerigi):     ADR-0012 — ValidateMaxCallbacksPerScene (TR-diyalog-005)
///   TODO(epic:sahne-kesmeli-anlati):       ADR-0015 — NightConfigDef tutarlılık check'leri
///   TODO(epic:birinci-sahis-kontrolcu):    TR-fpc-016 decoy check'i
/// </summary>
internal static class BuildValidationRegistry
{
    internal static readonly IBuildCheck[] Checks = { };
}
