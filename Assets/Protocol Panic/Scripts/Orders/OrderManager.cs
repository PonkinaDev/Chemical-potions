using Fusion;
using TMPro;
using UnityEngine;

public class OrderManager : NetworkBehaviour
{
    public static OrderManager Instance;

    [Header("UI")]
    [SerializeField]
    private Transform _ordersContainer;

    [SerializeField]
    private OrderCardUI _orderCardPrefab;

    [SerializeField]
    private TextMeshProUGUI _moneyText;

    [Networked]
    public IngredientType Order1 { get; set; }

    [Networked]
    public IngredientType Order2 { get; set; }

    [Networked]
    public IngredientType Order3 { get; set; }

    [Networked]
    public int Money { get; set; }

    private IngredientType _lastOrder1;
    private IngredientType _lastOrder2;
    private IngredientType _lastOrder3;

    private int _lastMoney = -1;

    private void Awake()
    {
        Instance = this;
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Money = 0;

            Order1 = GetRandomOrder();
            Order2 = GetRandomOrder();
            Order3 = GetRandomOrder();
        }

        RefreshOrdersUI();

        UpdateMoneyUI();
    }

    public override void Render()
    {
        if (
            _lastOrder1 != Order1 ||
            _lastOrder2 != Order2 ||
            _lastOrder3 != Order3
        )
        {
            RefreshOrdersUI();
        }

        UpdateMoneyUI();
    }

    public bool TryDeliver(
        IngredientType ingredient
    )
    {
        if (ingredient == Order1)
        {
            Money += GetRewardValue(
                ingredient
            );

            Order1 = Order2;
            Order2 = Order3;
            Order3 = GetRandomOrder();

            return true;
        }

        if (ingredient == Order2)
        {
            Money += GetRewardValue(
                ingredient
            );

            Order2 = Order3;
            Order3 = GetRandomOrder();

            return true;
        }

        if (ingredient == Order3)
        {
            Money += GetRewardValue(
                ingredient
            );

            Order3 = GetRandomOrder();

            return true;
        }

        return false;
    }

    private IngredientType GetRandomOrder()
    {
        int random =
            Random.Range(0, 6);

        switch (random)
        {
            case 0:
                return IngredientType.Red;

            case 1:
                return IngredientType.Blue;

            case 2:
                return IngredientType.Yellow;

            case 3:
                return IngredientType.Green;

            case 4:
                return IngredientType.Orange;

            case 5:
                return IngredientType.Purple;
        }

        return IngredientType.Red;
    }

    private void RefreshOrdersUI()
    {
        _lastOrder1 = Order1;
        _lastOrder2 = Order2;
        _lastOrder3 = Order3;

        foreach (Transform child
            in _ordersContainer)
        {
            Destroy(child.gameObject);
        }

        CreateCard(Order1);

        CreateCard(Order2);

        CreateCard(Order3);
    }

    private void CreateCard(
        IngredientType order
    )
    {
        OrderCardUI card =
            Instantiate(
                _orderCardPrefab,
                _ordersContainer
            );

        card.Setup(
            order,
            GetRewardValue(order)
        );
    }

    private int GetRewardValue(
        IngredientType ingredient
    )
    {
        switch (ingredient)
        {
            case IngredientType.Red:
            case IngredientType.Blue:
            case IngredientType.Yellow:
                return 10;

            case IngredientType.Green:
            case IngredientType.Orange:
            case IngredientType.Purple:
                return 20;
        }

        return 0;
    }

    private void UpdateMoneyUI()
    {
        if (_lastMoney == Money)
            return;

        _lastMoney = Money;

        if (_moneyText != null)
        {
            _moneyText.text =
                "$ " + Money;
        }
    }
}