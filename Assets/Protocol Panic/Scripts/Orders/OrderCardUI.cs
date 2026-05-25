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

        _rewardText.text = "$" + reward;

        _potionImage.color =
            IngredientColorUtility.GetColor(type);
    }
}