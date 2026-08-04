// PROTOTYPE ONLY — interaction pieces for the short playable slice.
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Yankilar.Greybox
{
    public interface IGreyboxInteractable
    {
        string Prompt { get; }
        void Interact(GreyboxGameState gameState);
    }

    public sealed class GreyboxInteractor : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private GreyboxGameState gameState;
        [SerializeField] private float reach = 2.5f;
        private IGreyboxInteractable focused;

        private void Update()
        {
            focused = null;
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, reach))
                focused = hit.collider.GetComponentInParent<IGreyboxInteractable>();
            gameState.SetPrompt(focused == null ? string.Empty : focused.Prompt);
            if (focused != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                focused.Interact(gameState);
        }
    }

    public sealed class TaskBoard : MonoBehaviour, IGreyboxInteractable
    {
        public string Prompt => "[E] Gece vardiyası görev panosunu oku";
        public void Interact(GreyboxGameState gameState) => gameState.ReadBriefing();
    }

    public sealed class CarryItem : MonoBehaviour, IGreyboxInteractable
    {
        [SerializeField] private string itemId;
        [SerializeField] private string itemName;
        [SerializeField] private Color carryColor;
        [SerializeField] private Renderer itemRenderer;
        public string Prompt => "[E] " + itemName + " al";

        public void Interact(GreyboxGameState gameState)
        {
            if (!gameState.CanPickUp(itemId, out string reason)) { gameState.ShowMessage(reason); return; }
            itemRenderer.enabled = false;
            GetComponent<Collider>().enabled = false;
            gameState.PickUp(itemName, carryColor);
        }
    }

    public sealed class DeliveryZone : MonoBehaviour, IGreyboxInteractable
    {
        public string Prompt => "[E] Yükü ana masaya bırak";
        public void Interact(GreyboxGameState gameState) => gameState.DeliverItem();
    }

    public sealed class ElevatorTerminal : MonoBehaviour, IGreyboxInteractable
    {
        [SerializeField] private GreyboxFloor from;
        [SerializeField] private GreyboxFloor requestedDestination;
        [SerializeField] private Transform serviceExit;
        [SerializeField] private Transform ballroomExit;
        [SerializeField] private Transform anomalyExit;
        [SerializeField] private GreyboxPlayer player;
        [SerializeField] private Renderer indicator;

        public string Prompt => "[E] Asansörü çağır";
        public void Interact(GreyboxGameState gameState)
        {
            if (!gameState.CanRequestElevator(from, requestedDestination, out string reason)) { gameState.ShowMessage(reason); return; }
            StartCoroutine(Ride(gameState));
        }

        private IEnumerator Ride(GreyboxGameState gameState)
        {
            GreyboxFloor destination = gameState.GetActualDestination(from, requestedDestination);
            gameState.BeginElevatorRide();
            indicator.material.color = new Color(.75f, .18f, .12f);
            gameState.ShowMessage("Kapılar kapanıyor…");
            yield return new WaitForSeconds(1.6f);
            gameState.ShowMessage("Asansör hareket ediyor.");
            yield return new WaitForSeconds(destination == GreyboxFloor.Anomaly ? 3.4f : 2.8f);
            Transform exit = destination == GreyboxFloor.Service ? serviceExit : destination == GreyboxFloor.Ballroom ? ballroomExit : anomalyExit;
            player.transform.SetPositionAndRotation(exit.position, exit.rotation);
            indicator.material.color = new Color(.25f, .55f, .3f);
            yield return new WaitForSeconds(.45f);
            gameState.EndElevatorRide(destination);
        }
    }

    public sealed class MemoryObject : MonoBehaviour, IGreyboxInteractable
    {
        [SerializeField] private Volume coldVolume;
        [SerializeField] private Light[] corridorLights;
        [SerializeField] private AmbientHum ambience;
        [SerializeField] private Renderer objectRenderer;
        private bool triggered;
        private float blend;
        public string Prompt => triggered ? string.Empty : "[E] Çatlak şampanya kadehini incele";

        private void Update()
        {
            if (!triggered) return;
            blend = Mathf.MoveTowards(blend, 1f, Time.deltaTime * .35f);
            coldVolume.weight = blend;
            foreach (Light light in corridorLights)
                light.color = Color.Lerp(new Color(1f, .58f, .27f), new Color(.28f, .64f, .53f), blend);
        }

        public void Interact(GreyboxGameState gameState)
        {
            if (!gameState.IsMemoryRequired) { gameState.ShowMessage("Sıradan bir kadeh. Şimdilik."); return; }
            triggered = true;
            ambience.SetUnsettled(true);
            objectRenderer.material.color = new Color(.55f, .86f, .74f);
            gameState.SeeMemory();
        }
    }

    public sealed class BreakerPanel : MonoBehaviour, IGreyboxInteractable
    {
        [SerializeField] private Renderer lamp;
        public string Prompt => "[E] Elektrik panosunu yeniden başlat";
        public void Interact(GreyboxGameState gameState)
        {
            if (!gameState.NeedsBreakerRepair) { gameState.ShowMessage("Panel şimdilik çalışıyor."); return; }
            lamp.material.color = new Color(.25f, .8f, .3f);
            gameState.RepairBreaker();
        }
    }
}
