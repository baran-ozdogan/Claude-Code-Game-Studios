# Epics Index

> Last Updated: 2026-08-09
> Engine: Unity 6.5 (6000.5.6f1) — re-pinned 2026-08-09
> Control Manifest Version: 2026-08-09
> Producer gate (PR-EPIC): skipped — producer subagent unavailable in generating session (recorded per project precedent; epic boundaries mirror architecture.md's module table 1:1 + one bootstrap epic)

| Epic | Layer | System | GDD | Governing ADRs | Stories | Status |
|------|-------|--------|-----|----------------|---------|--------|
| [Proje Kurulumu](proje-kurulumu/EPIC.md) | Foundation | Cross-cutting altyapı | — | ADR-0001, 0002 | **6 stories** | **Complete (2026-08-09)** |
| [InteractableRegistry](interactable-registry/EPIC.md) | Foundation | InteractableRegistry | etkilesim-sistemi.md | ADR-0004 | Not yet created | Ready |
| [Birinci Şahıs Kontrolcü](birinci-sahis-kontrolcu/EPIC.md) | Foundation | FPC | birinci-sahis-kontrolcu.md | ADR-0003 | Not yet created | Ready |
| [Işık/Volume Durum Sistemi](isik-volume-durum-sistemi/EPIC.md) | Foundation | Işık/Volume | isik-volume-durum-sistemi.md | ADR-0005 (+addendum) | **6 stories** | Ready |
| [Gece/Oturum Durumu](gece-oturum-durumu/EPIC.md) | Foundation | Session State | gece-oturum-durumu-2026-08-02.md | ADR-0006 | **4 stories** | **Complete (2026-08-09)** |
| [Anlatı Durum/İpucu Takibi](anlati-durum-ipucu-takibi/EPIC.md) | Foundation | Clue Tracking | anlati-durum-ipucu-takibi.md | ADR-0007 | Not yet created | Ready |
| [Seviye/Sahne Geçişi](seviye-sahne-gecisi/EPIC.md) | Foundation | Scene Transition | seviye-sahne-gecisi.md | ADR-0008 | Not yet created | Ready |
| [Adaptif Ses Sistemi](adaptif-ses-sistemi/EPIC.md) | Foundation | Audio | adaptif-ses-sistemi.md | ADR-0009 (+addendum) | Not yet created | Ready |

## Önerilen implementasyon sırası

1. **proje-kurulumu** (her şeyin önkoşulu; gate koşulu #1 burada kapanır)
2. **interactable-registry** → **birinci-sahis-kontrolcu** → **isik-volume-durum-sistemi** (2-4 kısmi paralel gidebilir)
3. **gece-oturum-durumu** → **anlati-durum-ipucu-takibi** (5→6 sıralı; 6, 5'in reset sırasına oturur)
4. **seviye-sahne-gecisi** → **adaptif-ses-sistemi** (8, 7'nin event'ine abone)

Core katmanı epic'leri (`etkilesim-sistemi`, `asansor-kat-erisim`, `diyalog-anlati-icerigi`) Foundation ilerleyince `/create-epics layer: core` ile açılacak.
