using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class NetworkPlayer : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;

    [Header("Hold")]
    [SerializeField] private Transform _holdPoint;

    [Header("Interaction")]
    [SerializeField] private float _interactionRadius = 2f;

    private const float PickupCooldownDuration = 0.25f;

    private CharacterController _characterController;

    [Networked]
    private Vector3 NetworkedPosition { get; set; }

    [Networked]
    private Quaternion NetworkedRotation { get; set; }

    [Networked]
    public IngredientType HeldIngredient { get; set; }

    [Networked]
    public PotionState HeldPotionState { get; set; }

    private GameObject _heldVisual;

    private IngredientType _lastVisualIngredient = IngredientType.None;
    private PotionState _lastVisualState = PotionState.Raw;

    private IngredientDispenser _nearbyDispenser;
    private PotionMixer _nearbyMixer;
    private PotionCauldron _nearbyCauldron;
    private DeliveryZone _nearbyDeliveryZone;
    private TrashBin _nearbyTrashBin;

    private float _pickupCooldown;

    public override void Spawned()
    {
        _characterController = GetComponent<CharacterController>();
        UpdateHeldVisual();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (!GetInput(out PlayerInputData input))
            return;

        HandleMovement(input);
        DetectInteractables();
        UpdateCooldown();

        if (CanInteract(input))
            Interact();
    }

    public override void Render()
    {
        SmoothNetworkTransform();
        UpdateHeldVisual();
    }

    private void HandleMovement(PlayerInputData input)
    {
        Vector3 direction = new(
            input.MovementInput.x,
            0f,
            input.MovementInput.y
        );

        direction.Normalize();

        Vector3 movement = direction * _moveSpeed * Runner.DeltaTime;

        _characterController.Move(movement);

        NetworkedPosition = transform.position;

        if (direction != Vector3.zero)
            NetworkedRotation = Quaternion.LookRotation(direction);
    }

    private void SmoothNetworkTransform()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            NetworkedPosition,
            Runner.DeltaTime * 15f
        );

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            NetworkedRotation,
            Runner.DeltaTime * 15f
        );
    }

    private void DetectInteractables()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            _interactionRadius
        );

        _nearbyDispenser = FindClosest<IngredientDispenser>(hits);
        _nearbyMixer = FindClosest<PotionMixer>(hits);
        _nearbyCauldron = FindClosest<PotionCauldron>(hits);
        _nearbyDeliveryZone = FindClosest<DeliveryZone>(hits);
        _nearbyTrashBin = FindClosest<TrashBin>(hits);
    }

    private T FindClosest<T>(Collider[] hits) where T : Component
    {
        T closest = null;
        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            T component = hit.GetComponent<T>();

            if (component == null)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                component.transform.position
            );

            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            closest = component;
        }

        return closest;
    }

    private void UpdateCooldown()
    {
        _pickupCooldown -= Runner.DeltaTime;
    }

    private bool CanInteract(PlayerInputData input)
    {
        return input.PickupPressed && _pickupCooldown <= 0f;
    }

    private void Interact()
    {
        _pickupCooldown = PickupCooldownDuration;

        if (HandleTrashInteraction())
            return;

        if (HandleDeliveryInteraction())
            return;

        if (HandleCauldronInteraction())
            return;

        if (HandleMixerInteraction())
            return;

        HandleDispenserInteraction();
    }

    private bool HandleTrashInteraction()
    {
        if (_nearbyTrashBin == null)
            return false;

        if (HasIngredient())
            ClearIngredient();

        return true;
    }

    private bool HandleDeliveryInteraction()
    {
        if (_nearbyDeliveryZone == null)
            return false;

        if (!HasIngredient())
            return true;

        if (OrderManager.Instance == null)
            return true;

        bool delivered = OrderManager.Instance.TryDeliver(
            HeldIngredient,
            HeldPotionState
        );

        if (delivered)
            ClearIngredient();

        return true;
    }

    private bool HandleCauldronInteraction()
    {
        if (_nearbyCauldron == null)
            return false;

        if (!HasIngredient())
        {
            bool success = _nearbyCauldron.TryTakePotion(
                out IngredientType potion,
                out PotionState state
            );

            if (success)
            {
                HeldIngredient = potion;
                HeldPotionState = state;
            }

            return true;
        }

        bool placed = _nearbyCauldron.TryAddPotion(HeldIngredient);

        if (placed)
            ClearIngredient();

        return true;
    }

    private bool HandleMixerInteraction()
    {
        if (_nearbyMixer == null)
            return false;

        if (!HasIngredient())
        {
            if (_nearbyMixer.CurrentColor == IngredientType.None)
                return true;

            HeldIngredient = _nearbyMixer.CurrentColor;
            HeldPotionState = PotionState.Raw;

            _nearbyMixer.CurrentColor = IngredientType.None;

            return true;
        }

        bool success = _nearbyMixer.TryAddIngredient(HeldIngredient);

        if (success)
            ClearIngredient();

        return true;
    }

    private void HandleDispenserInteraction()
    {
        if (_nearbyDispenser == null)
            return;

        HeldIngredient = _nearbyDispenser.IngredientType;
        HeldPotionState = PotionState.Raw;
    }

    public bool HasIngredient()
    {
        return HeldIngredient != IngredientType.None;
    }

    public void ClearIngredient()
    {
        if (!HasStateAuthority)
            return;

        HeldIngredient = IngredientType.None;
        HeldPotionState = PotionState.Raw;
    }

    private void UpdateHeldVisual()
    {
        if (!VisualChanged())
            return;

        DestroyHeldVisual();

        if (!HasIngredient())
            return;

        _heldVisual = CreateHeldVisual();
    }

    private bool VisualChanged()
    {
        if (_lastVisualIngredient == HeldIngredient &&
            _lastVisualState == HeldPotionState)
        {
            return false;
        }

        _lastVisualIngredient = HeldIngredient;
        _lastVisualState = HeldPotionState;

        return true;
    }

    private void DestroyHeldVisual()
    {
        if (_heldVisual != null)
            Destroy(_heldVisual);
    }

    private GameObject CreateHeldVisual()
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);

        visual.transform.SetParent(_holdPoint);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * 0.4f;

        Collider collider = visual.GetComponent<Collider>();

        if (collider != null)
            Destroy(collider);

        Renderer renderer = visual.GetComponent<Renderer>();

        Material material = new(
            Shader.Find("Universal Render Pipeline/Lit")
        );

        renderer.material = material;
        renderer.material.color = GetPotionColor();

        return visual;
    }

    private Color GetPotionColor()
    {
        Color color = HeldIngredient switch
        {
            IngredientType.Red => Color.red,
            IngredientType.Blue => Color.blue,
            IngredientType.Yellow => Color.yellow,
            IngredientType.Green => Color.green,
            IngredientType.Orange => new Color(1f, 0.5f, 0f),
            IngredientType.Purple => new Color(0.5f, 0f, 1f),
            _ => Color.white
        };

        if (HeldPotionState == PotionState.Cooked)
            color *= 0.7f;

        if (HeldPotionState == PotionState.Burned)
            color = Color.black;

        return color;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            _interactionRadius
        );
    }
}