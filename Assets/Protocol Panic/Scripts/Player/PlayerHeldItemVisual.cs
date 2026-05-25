using UnityEngine;

public class PlayerHeldItemVisual : MonoBehaviour
{
    [SerializeField]
    private Transform _holdPoint;

    private GameObject _heldVisual;

    private IngredientType _lastIngredient =
        IngredientType.None;

    private PotionState _lastState =
        PotionState.Raw;

    public void UpdateVisual(
        IngredientType ingredient,
        PotionState potionState
    )
    {
        if (!VisualChanged(
            ingredient,
            potionState))
        {
            return;
        }

        DestroyVisual();

        if (ingredient == IngredientType.None)
            return;

        _heldVisual = CreateVisual(
            ingredient,
            potionState
        );
    }

    private bool VisualChanged(
        IngredientType ingredient,
        PotionState potionState
    )
    {
        if (_lastIngredient == ingredient &&
            _lastState == potionState)
        {
            return false;
        }

        _lastIngredient = ingredient;
        _lastState = potionState;

        return true;
    }

    private void DestroyVisual()
    {
        if (_heldVisual != null)
            Destroy(_heldVisual);
    }

    private GameObject CreateVisual(
        IngredientType ingredient,
        PotionState potionState
    )
    {
        GameObject visual =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        visual.transform.SetParent(_holdPoint);

        visual.transform.localPosition =
            Vector3.zero;

        visual.transform.localRotation =
            Quaternion.identity;

        visual.transform.localScale =
            Vector3.one * 0.4f;

        Collider collider =
            visual.GetComponent<Collider>();

        if (collider != null)
            Destroy(collider);

        Renderer renderer =
            visual.GetComponent<Renderer>();

        Material material = new(
            Shader.Find(
                "Universal Render Pipeline/Lit"
            )
        );

        renderer.material = material;

        renderer.material.color =
            IngredientColorUtility.GetPotionColor(
                ingredient,
                potionState
            );

        return visual;
    }
}