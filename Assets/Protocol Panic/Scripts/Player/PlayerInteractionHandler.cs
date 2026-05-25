using UnityEngine;

public class PlayerInteractionHandler : MonoBehaviour
{
    private NetworkPlayer _player;
    private PlayerInteractableDetector _detector;

    public void Initialize(
        NetworkPlayer player,
        PlayerInteractableDetector detector
    )
    {
        _player = player;
        _detector = detector;
    }

    public void Interact()
    {
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
        if (_detector.NearbyTrashBin == null)
            return false;

        if (_player.HasIngredient())
            _player.ClearIngredient();

        return true;
    }

    private bool HandleDeliveryInteraction()
    {
        if (_detector.NearbyDeliveryZone == null)
            return false;

        if (!_player.HasIngredient())
            return true;

        if (OrderManager.Instance == null)
            return true;

        bool delivered =
            OrderManager.Instance.TryDeliver(
                _player.HeldIngredient,
                _player.HeldPotionState
            );

        if (delivered)
            _player.ClearIngredient();

        return true;
    }

    private bool HandleCauldronInteraction()
    {
        if (_detector.NearbyCauldron == null)
            return false;

        if (!_player.HasIngredient())
        {
            bool success =
                _detector.NearbyCauldron.TryTakePotion(
                    out IngredientType potion,
                    out PotionState state
                );

            if (success)
            {
                _player.HeldIngredient =
                    potion;

                _player.HeldPotionState =
                    state;
            }

            return true;
        }

        bool placed =
            _detector.NearbyCauldron.TryAddPotion(
                _player.HeldIngredient
            );

        if (placed)
            _player.ClearIngredient();

        return true;
    }

    private bool HandleMixerInteraction()
    {
        if (_detector.NearbyMixer == null)
            return false;

        if (!_player.HasIngredient())
        {
            if (_detector.NearbyMixer.CurrentColor ==
                IngredientType.None)
            {
                return true;
            }

            _player.HeldIngredient =
                _detector.NearbyMixer.CurrentColor;

            _player.HeldPotionState =
                PotionState.Raw;

            _detector.NearbyMixer.CurrentColor =
                IngredientType.None;

            return true;
        }

        bool success =
            _detector.NearbyMixer.TryAddIngredient(
                _player.HeldIngredient
            );

        if (success)
            _player.ClearIngredient();

        return true;
    }

    private void HandleDispenserInteraction()
    {
        if (_detector.NearbyDispenser == null)
            return;

        _player.HeldIngredient =
            _detector.NearbyDispenser.IngredientType;

        _player.HeldPotionState =
            PotionState.Raw;
    }
}