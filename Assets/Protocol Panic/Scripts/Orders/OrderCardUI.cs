using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderCardUI : MonoBehaviour
{
    [SerializeField]
    private Image _potionImage;

    [SerializeField]
    private TextMeshProUGUI _rewardText;

    public IngredientType OrderType
    {
        get;
        private set;
    }

    public void Setup(
        IngredientType type,
        int reward
    )
    {
        OrderType = type;

        _rewardText.text =
            "$" + reward;

        switch (type)
        {
            case IngredientType.Red:
                _potionImage.color =
                    Color.red;
                break;

            case IngredientType.Blue:
                _potionImage.color =
                    Color.blue;
                break;

            case IngredientType.Yellow:
                _potionImage.color =
                    Color.yellow;
                break;

            case IngredientType.Green:
                _potionImage.color =
                    Color.green;
                break;

            case IngredientType.Orange:
                _potionImage.color =
                    new Color(1f, 0.5f, 0f);
                break;

            case IngredientType.Purple:
                _potionImage.color =
                    new Color(0.5f, 0f, 1f);
                break;
        }
    }
}