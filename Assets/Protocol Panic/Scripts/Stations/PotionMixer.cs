using Fusion;
using UnityEngine;

public class PotionMixer : NetworkBehaviour
{
    [Networked]
    public IngredientType CurrentColor { get; set; }

    [SerializeField] private Material _potionMaterial;

    private IngredientType _lastVisualColor = IngredientType.None;

    public override void Render()
    {
        UpdateVisual();
    }

    public bool TryAddIngredient(IngredientType ingredient)
    {
        if (ingredient == IngredientType.None)
            return false;

        if (IsSecondary(CurrentColor))
            return false;

        if (CurrentColor == IngredientType.None)
        {
            CurrentColor = ingredient;
            return true;
        }

        if (CurrentColor == ingredient)
            return false;

        IngredientType result = Mix(CurrentColor, ingredient);

        if (result == IngredientType.None)
            return false;

        CurrentColor = result;

        return true;
    }

    private IngredientType Mix(IngredientType a, IngredientType b)
    {
        if ((a == IngredientType.Red && b == IngredientType.Blue) ||
            (a == IngredientType.Blue && b == IngredientType.Red))
            return IngredientType.Purple;

        if ((a == IngredientType.Blue && b == IngredientType.Yellow) ||
            (a == IngredientType.Yellow && b == IngredientType.Blue))
            return IngredientType.Green;

        if ((a == IngredientType.Red && b == IngredientType.Yellow) ||
            (a == IngredientType.Yellow && b == IngredientType.Red))
            return IngredientType.Orange;

        return IngredientType.None;
    }

    private bool IsSecondary(IngredientType type)
    {
        return type == IngredientType.Green
            || type == IngredientType.Orange
            || type == IngredientType.Purple;
    }

    private void UpdateVisual()
    {
        if (_lastVisualColor == CurrentColor)
            return;

        _lastVisualColor = CurrentColor;

        if (_potionMaterial == null)
            return;

        switch (CurrentColor)
        {
            case IngredientType.None:
                _potionMaterial.color = Color.white;
                break;
            case IngredientType.Red:
                _potionMaterial.color = Color.red;
                break;
            case IngredientType.Blue:
                _potionMaterial.color = Color.blue;
                break;
            case IngredientType.Yellow:
                _potionMaterial.color = Color.yellow;
                break;
            case IngredientType.Green:
                _potionMaterial.color = Color.green;
                break;
            case IngredientType.Orange:
                _potionMaterial.color = new Color(1f, 0.5f, 0f);
                break;
            case IngredientType.Purple:
                _potionMaterial.color = new Color(0.5f, 0f, 1f);
                break;
        }
    }
}