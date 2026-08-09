using UnityEngine;

/// <summary>
/// Işık/Volume sisteminin dört build-blocking sahne-scan check'i (ADR-0005
/// Risks bölümünün üçlüsü + AC21 içerik zorunluluğu; TR-isik-016/020/021).
/// Hepsi BuildValidationRegistry.Checks üzerinden koşar — ikinci bağımsız
/// IPreprocessBuildWithReport yasak (manifest). AC22'nin ClueDefinition çapraz
/// kontrolü anlati epic'inde bu eve eklenecek; StingerAudioRadius>0 zorunluluğu
/// ani-tetikleyici epic'inin check'i (MemoryTriggerDef eşlemesi gerekli).
/// </summary>
internal static class IsikVolumeBuildCheckUtil
{
    /// <summary>Açık sahnedeki tüm ShiftZone'lar (inaktifler dahil — build içeriği hepsi).</summary>
    internal static ShiftZone[] FindZones() =>
        Object.FindObjectsByType<ShiftZone>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);

    /// <summary>
    /// Box collider'ın world-space AABB'si — 8 köşe transform'lanarak, fizik
    /// senkronuna güvenmeden (edit-time deterministik; rotasyonlu kutular dahil).
    /// NOT: AABB-AABB kesişimi DÖNÜK kutular için muhafazakârdır — gerçek OBB'ler
    /// değmese de yakın-dönük çift bayraklanabilir; build blocker için güvenli
    /// yön (yanlış-pozitife 20-40m merkez aralığı rehberi zaten çare).
    /// </summary>
    internal static Bounds WorldBounds(BoxCollider box)
    {
        Vector3 half = box.size * 0.5f;
        var bounds = new Bounds(box.transform.TransformPoint(box.center), Vector3.zero);
        for (int xi = -1; xi <= 1; xi += 2)
        for (int yi = -1; yi <= 1; yi += 2)
        for (int zi = -1; zi <= 1; zi += 2)
        {
            Vector3 corner = box.center + new Vector3(half.x * xi, half.y * yi, half.z * zi);
            bounds.Encapsulate(box.transform.TransformPoint(corner));
        }
        return bounds;
    }
}

/// <summary>
/// (1) ZoneLight dizisindeki her Light, Mode=Mixed olmalı — Baked ışık URP'nin
/// gerçek-zamanlı forward pass'inden tamamen hariçtir; runtime renk/yoğunluk
/// yazımı SESSİZCE etkisiz kalır (GDD "kritik motor-seviyesi kısıt").
/// </summary>
internal sealed class IsikVolumeBakedLightCheck : IBuildCheck
{
    public string Name => "IsikVolume/LightModeMixed";
    public BuildCheckPhase Phase => BuildCheckPhase.SceneScan;

    public void Run(BuildCheckContext context)
    {
        foreach (ShiftZone zone in IsikVolumeBuildCheckUtil.FindZones())
        {
            if (zone._lights == null) continue;
            foreach (ZoneLight entry in zone._lights)
            {
                if (entry.light != null && entry.light.lightmapBakeType != LightmapBakeType.Mixed)
                {
                    context.Fail(
                        $"'{zone.name}' bölgesinin ZoneLight girdisi '{entry.light.name}' " +
                        $"Light Mode={entry.light.lightmapBakeType} — Mixed olmalı. Baked/Realtime ışık " +
                        "shift'e sessizce tepki vermez (TR-isik-016).");
                }
            }
        }
    }
}

/// <summary>
/// (2) İki bölge aynı Light'ı paylaşamaz — iki coroutine aynı ışığa aynı karede
/// çelişen değer yazar, sonuç coroutine sırasına bağlı belirsizlik olur (ADR-0005).
/// </summary>
internal sealed class IsikVolumeSharedLightCheck : IBuildCheck
{
    public string Name => "IsikVolume/NoSharedLights";
    public BuildCheckPhase Phase => BuildCheckPhase.SceneScan;

    public void Run(BuildCheckContext context)
    {
        var owners = new System.Collections.Generic.Dictionary<Light, ShiftZone>();
        foreach (ShiftZone zone in IsikVolumeBuildCheckUtil.FindZones())
        {
            if (zone._lights == null) continue;
            foreach (ZoneLight entry in zone._lights)
            {
                if (entry.light == null) continue;
                if (owners.TryGetValue(entry.light, out ShiftZone other) && other != zone)
                {
                    context.Fail(
                        $"'{entry.light.name}' ışığı iki bölgede birden referanslı: " +
                        $"'{other.name}' ve '{zone.name}' — bölgeler ışık paylaşamaz (TR-isik-016).");
                }
                owners[entry.light] = zone;
            }
        }
    }
}

/// <summary>
/// (3) İki bölgenin Volume-trigger box'ları kesişemez — her bölge AYNI paylaşılan
/// VolumeProfile'ı ağırlıklar; URP lokal Volume stack'i çakışan iki ağırlığı
/// ortalamaz, ardışık over-lerp'ler (ekran-geneli non-lineer grade patlaması;
/// ADR-0005 unity-specialist BLOCKING bulgusu — spike bu durumu HİÇ test etmedi).
/// </summary>
internal sealed class IsikVolumeBoxOverlapCheck : IBuildCheck
{
    public string Name => "IsikVolume/NoBoxOverlap";
    public BuildCheckPhase Phase => BuildCheckPhase.SceneScan;

    public void Run(BuildCheckContext context)
    {
        ShiftZone[] zones = IsikVolumeBuildCheckUtil.FindZones();
        for (int i = 0; i < zones.Length; i++)
        {
            var boxA = zones[i].GetComponent<BoxCollider>();
            if (boxA == null) continue;
            for (int j = i + 1; j < zones.Length; j++) // O(n²) — sahne başına n küçük (manifest guardrail)
            {
                var boxB = zones[j].GetComponent<BoxCollider>();
                if (boxB == null) continue;
                if (IsikVolumeBuildCheckUtil.WorldBounds(boxA).Intersects(IsikVolumeBuildCheckUtil.WorldBounds(boxB)))
                {
                    context.Fail(
                        $"'{zones[i].name}' ve '{zones[j].name}' bölgelerinin Volume-trigger box'ları kesişiyor — " +
                        "iki bölge aynı paylaşılan VolumeProfile'ı aynı anda ağırlıklarsa URP stack'i ardışık " +
                        "over-lerp üretir (ekran-geneli grade patlaması). Merkez aralığını Box Safety Margin " +
                        "formülüne göre aç (~20-40m rehberi, ADR-0005).");
                }
            }
        }
    }
}

/// <summary>
/// (4) Taranan sahnelerin EN AZ birinde TriggerMode=Automatic bölge olmalı —
/// taşıma rotasındaki zorunlu ambient shift bölgesi Pillar 1 garantisidir
/// (TR-isik-020/021, GDD AC21). Sahneler-arası TOPLAM iddia: BeginWalk gözlemi
/// sıfırlar (yürüyüş başında — ortada patlayan yürüyüş bayat gözlem sızdıramaz),
/// Run biriktirir, FinalizeWalk değerlendirir.
/// </summary>
internal sealed class IsikVolumeAutomaticPresenceCheck : IBuildCheck, IBuildCheckAggregate
{
    private bool _automaticSeen;

    public string Name => "IsikVolume/AutomaticZonePresence";
    public BuildCheckPhase Phase => BuildCheckPhase.SceneScan;

    public void BeginWalk() => _automaticSeen = false;

    public void Run(BuildCheckContext context)
    {
        foreach (ShiftZone zone in IsikVolumeBuildCheckUtil.FindZones())
        {
            if (zone._triggerMode == TriggerMode.Automatic)
            {
                _automaticSeen = true;
                return;
            }
        }
    }

    public void FinalizeWalk(BuildCheckContext context)
    {
        if (!_automaticSeen)
        {
            context.Fail(
                "Taranan hiçbir sahnede TriggerMode=Automatic ShiftZone yok — taşıma rotasında en az bir " +
                "Automatic, Persistent=false, ipucu-taşımayan ortam bölgesi zorunlu MVP içeriğidir " +
                "(Pillar 1 garantisi; TR-isik-020/021, GDD AC21).");
        }
    }
}
