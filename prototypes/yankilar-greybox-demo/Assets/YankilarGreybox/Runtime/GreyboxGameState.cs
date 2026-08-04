// PROTOTYPE ONLY — deliberately compact state for a disposable vertical slice.
using UnityEngine;

namespace Yankilar.Greybox
{
    public enum GreyboxFloor { Service, Ballroom, Anomaly }

    public sealed class GreyboxGameState : MonoBehaviour
    {
        private static readonly string[] ItemIds = { "linen", "champagne", "flowers" };
        private static readonly string[] ItemNames = { "masa örtüsü kutusu", "şampanya kasası", "çiçek aranjmanı" };

        [SerializeField] private GreyboxPlayer player;
        [SerializeField] private Transform carryAnchor;

        private GameObject carriedVisual;
        private string prompt = string.Empty;
        private string message = "Gece vardiyası başladı.";
        private float messageUntil;
        private bool briefingRead;
        private bool memorySeen;
        private bool anomalyResolved;
        private bool elevatorBusy;
        private bool completed;
        private int deliveredCount;
        private float startedAt;

        public bool HasCarriedItem { get; private set; }
        public bool IsMemoryRequired => deliveredCount == 1 && !memorySeen;
        public bool NeedsBreakerRepair => deliveredCount == 2 && !anomalyResolved;
        public bool ElevatorBusy => elevatorBusy;
        public bool IsComplete => completed;
        public int DeliveredCount => deliveredCount;
        public string CurrentItemId => deliveredCount < ItemIds.Length ? ItemIds[deliveredCount] : string.Empty;
        public string CurrentItemName => deliveredCount < ItemNames.Length ? ItemNames[deliveredCount] : string.Empty;

        private void Start() => startedAt = Time.time;

        public void SetPrompt(string value) => prompt = value;

        public void ReadBriefing()
        {
            briefingRead = true;
            ShowMessage("İlk iş: " + CurrentItemName + ". Depodaki etiketleri takip et.");
        }

        public bool CanPickUp(string itemId, out string reason)
        {
            if (!briefingRead) { reason = "Önce girişteki görev panosunu oku."; return false; }
            if (IsMemoryRequired) { reason = "Koridordaki kadeh gözüne takılıyor. Önce ona bakmalısın."; return false; }
            if (NeedsBreakerRepair) { reason = "Asansör arızası çözülmeden işe devam edemezsin."; return false; }
            if (HasCarriedItem) { reason = "Aynı anda yalnızca bir yük taşıyabilirsin."; return false; }
            if (itemId != CurrentItemId) { reason = "Görev listesinde sıradaki eşya bu değil."; return false; }
            reason = string.Empty;
            return true;
        }

        public void PickUp(string itemName, Color color)
        {
            HasCarriedItem = true;
            player.SetCarrying(true);
            carriedVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carriedVisual.name = "Carried_" + itemName;
            carriedVisual.transform.SetParent(carryAnchor, false);
            carriedVisual.transform.localPosition = new Vector3(0.28f, -0.42f, 0.95f);
            carriedVisual.transform.localScale = new Vector3(0.42f, 0.32f, 0.42f);
            carriedVisual.GetComponent<Collider>().enabled = false;
            carriedVisual.GetComponent<Renderer>().material.color = color;
            ShowMessage(itemName + " alındı. Asansörle balo salonuna götür.");
        }

        public void DeliverItem()
        {
            if (!HasCarriedItem) { ShowMessage("Teslim edecek bir eşyan yok."); return; }
            HasCarriedItem = false;
            player.SetCarrying(false);
            if (carriedVisual != null) Destroy(carriedVisual);
            deliveredCount++;
            if (deliveredCount >= ItemIds.Length)
            {
                completed = true;
                ShowMessage("Son teslimat da tamamlandı. Balo salonu ilk kez sessizleşiyor.");
                return;
            }
            ShowMessage("Teslim edildi. Depoya dön; sıradaki iş seni bekliyor.");
        }

        public void SeeMemory()
        {
            memorySeen = true;
            ShowMessage("Kadehteki yansıma sana ait değil. İşe dönmek zorundasın.");
        }

        public void RepairBreaker()
        {
            anomalyResolved = true;
            ShowMessage("Asansör tekrar çalışıyor. Servis katına dön.");
        }

        public bool CanRequestElevator(GreyboxFloor from, GreyboxFloor requested, out string reason)
        {
            if (elevatorBusy) { reason = "Asansör zaten hareket ediyor."; return false; }
            if (from == GreyboxFloor.Service && requested == GreyboxFloor.Ballroom && !HasCarriedItem)
            {
                reason = "Boş asansöre binme; sıradaki malzemeyi önce al."; return false;
            }
            if (from == GreyboxFloor.Ballroom && requested == GreyboxFloor.Service && HasCarriedItem)
            {
                reason = "Önce yükü teslim et."; return false;
            }
            if (from == GreyboxFloor.Anomaly && requested == GreyboxFloor.Service && !anomalyResolved)
            {
                reason = "Elektrik panosunu onarmadan kapılar açılmıyor."; return false;
            }
            reason = string.Empty;
            return true;
        }

        public GreyboxFloor GetActualDestination(GreyboxFloor from, GreyboxFloor requested)
        {
            return from == GreyboxFloor.Ballroom && requested == GreyboxFloor.Service && NeedsBreakerRepair
                ? GreyboxFloor.Anomaly : requested;
        }

        public void BeginElevatorRide() { elevatorBusy = true; player.SetGameplayEnabled(false); }
        public void EndElevatorRide(GreyboxFloor destination)
        {
            elevatorBusy = false;
            player.SetGameplayEnabled(true);
            if (destination == GreyboxFloor.Anomaly)
                ShowMessage("Asansör bilinmeyen bir katta durdu. Panodaki kırmızı ışığı bul.");
            else if (destination == GreyboxFloor.Service && IsMemoryRequired)
                ShowMessage("Koridor eskisinden daha soğuk. Kadeh ışığı yakalıyor.");
            else if (destination == GreyboxFloor.Ballroom)
                ShowMessage("Balo salonu hazır değil; yükü ana masaya bırak.");
        }

        public void ShowMessage(string value) { message = value; messageUntil = Time.time + 5.5f; }

        private string Objective()
        {
            if (completed) return "Vardiya tamamlandı — neyin değiştiğini not et.";
            if (!briefingRead) return "Görev panosunu oku.";
            if (NeedsBreakerRepair) return "Arıza katında elektrik panosunu onar.";
            if (HasCarriedItem) return "Asansörle balo salonuna geç ve yükü teslim et.";
            if (IsMemoryRequired) return "Koridordaki çatlak kadehi incele.";
            return "Sıradaki iş: " + CurrentItemName + ".";
        }

        private void OnGUI()
        {
            GUIStyle label = new GUIStyle(GUI.skin.label) { fontSize = 17, normal = { textColor = Color.white } };
            GUIStyle centered = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            GUIStyle small = new GUIStyle(label) { fontSize = 13, normal = { textColor = new Color(.8f, .8f, .8f) } };
            GUI.Label(new Rect(24, 20, 800, 32), "YANKILAR  |  GECE VARDİYASI", label);
            GUI.Label(new Rect(24, 48, 900, 26), "Teslimatlar: " + deliveredCount + "/3  •  " + Objective(), small);
            if (Time.time < messageUntil || completed)
                GUI.Label(new Rect(0, Screen.height - 94, Screen.width, 30), message, centered);
            if (!string.IsNullOrEmpty(prompt) && !elevatorBusy)
                GUI.Label(new Rect(0, Screen.height - 54, Screen.width, 30), prompt, centered);
            GUI.Label(new Rect(0, Screen.height * .5f - 11, Screen.width, 22), "+", centered);
            if (completed)
            {
                int seconds = Mathf.RoundToInt(Time.time - startedAt);
                GUI.Label(new Rect(0, Screen.height * .5f + 45, Screen.width, 30), "Kısa oyun döngüsü tamamlandı (" + seconds + " sn).", centered);
            }
        }
    }

    [RequireComponent(typeof(AudioSource))]
    public sealed class AmbientHum : MonoBehaviour
    {
        private float phase;
        private bool unsettled;

        public void SetUnsettled(bool value) => unsettled = value;

        private void OnAudioFilterRead(float[] data, int channels)
        {
            float frequency = unsettled ? 54f : 43f;
            float sampleRate = AudioSettings.outputSampleRate;
            for (int i = 0; i < data.Length; i += channels)
            {
                phase += frequency / sampleRate;
                if (phase > 1f) phase -= 1f;
                float value = Mathf.Sin(phase * Mathf.PI * 2f) * .018f;
                if (unsettled) value += Mathf.Sin(phase * Mathf.PI * 8f) * .006f;
                for (int channel = 0; channel < channels; channel++) data[i + channel] = value;
            }
        }
    }
}
