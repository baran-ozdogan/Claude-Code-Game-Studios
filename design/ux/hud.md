# HUD Yerleşimi — Tek Sayfa Spec

> **Kaynak**: gate-check 2026-08-09 koşulu #2; Story 005 ile birlikte yazıldı (2026-08-09)
> **Uygulama**: `game/Assets/UI/MainUI.uxml` + `MainUI.uss` (ADR-0002 tek paylaşılan UIDocument, persistent UI sahnesi)
> **Kapsam**: MVP'nin TAMAMI — 4 öğe. Envanter/can/minimap/quest-tracker YOK (art-bible §7 kapsam notu).

## Yerleşim Haritası

```
┌─────────────────────────────────────────────┐
│              #stinger-caption               │  üst-orta, üst %8 bandı
│                                             │
│                                             │
│                    ┌─┐                      │
│                 ┌──┤+├──┐                   │  ekran merkezi (sabit):
│                 │  └─┘  │                   │  #crosshair-container
│                 │ ring  │                   │   ├ #crosshair
│                 └───────┘                   │   └ #hold-fill-ring
│                                             │
│                                             │
│             #dialogue-subtitle              │  alt-orta, alt %10 bandı
└─────────────────────────────────────────────┘
```

## Öğeler

| Öğe (UXML adı) | Sahip sistem | Konum | Başlangıç | Davranış epic'i |
|---|---|---|---|---|
| `crosshair-container` → `crosshair` + `hold-fill-ring` | Etkileşim Sistemi (Core) | Ekran merkezi, mutlak; asla diegetik değil (art-bible §7.1) | Görünmez | etkilesim (ADR-0010: opacity/scale C#'ta, Hold-fill strictly linear) |
| `stinger-caption` | Adaptif Ses Sistemi (Foundation) | Üst-orta, üst ~%8 bandı | Görünmez, boş | ses (ADR-0009: 1-1.5s pencere, playback senkron) |
| `dialogue-subtitle` | Diyalog/Anlatı İçeriği (Core) | Alt-orta, alt ~%10 bandı | Görünmez, boş | diyalog (ADR-0012: içerik/zamanlama sözleşmesi orada) |

## Kurallar (kilitli)

- **USS sınıf önekleri**: `etkilesim-*`, `ses-*`, `diyalog-*` — sahip başına, çakışma yasak (ADR-0002).
- **GDD-kilitli animasyonlar C#'ta** — crosshair/Hold-fill için USS `transition` yasak (ADR-0002 kapsamlı kural).
- **Çakışmazlık**: caption üst bantta, subtitle alt bantta — aynı anda görünebilirler, kesişmezler; crosshair merkezi ikisinden de bağımsız.
- **a11y tabanı** (accessibility-requirements.md §2a/2b): caption/subtitle font tabanı 24px @1080p referans; kullanıcı ölçeklemesi ADR-0009/0012 story'lerinde USS variable'a taşınır.
- **Ölçekleme**: PanelSettings `ScaleWithScreenSize`, referans 1920×1080 (PC-only hedef).
