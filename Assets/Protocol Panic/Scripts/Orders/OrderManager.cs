using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderManager : NetworkBehaviour
{
    public static OrderManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Transform _orderContainer;
    [SerializeField] private GameObject _orderCardPrefab;
    [SerializeField] private TMP_Text _totalMoneyText;

    [Header("Settings")]
    [SerializeField] private int _maxOrders = 3;

    private const int PrimaryReward = 10;
    private const int SecondaryReward = 20;

    [Networked]
    public int TotalMoney { get; set; }

    [Networked, Capacity(10)]
    public NetworkArray<OrderNetworkData> Orders => default;

    private readonly List<GameObject> _spawnedCards = new();

    public override void Spawned()
    {
        Instance = this;

        if (HasStateAuthority)
            GenerateInitialOrders();

        RefreshUI();
    }

    public override void Render()
    {
        RefreshUI();
    }

    public bool TryDeliver(IngredientType ingredient, PotionState potionState)
    {
        if (!CanDeliver(potionState))
            return false;

        int orderIndex = FindOrderIndex(ingredient);

        if (orderIndex < 0)
            return false;

        CompleteOrder(orderIndex);

        return true;
    }

    private void GenerateInitialOrders()
    {
        for (int i = 0; i < _maxOrders; i++)
            GenerateOrder();
    }

    private bool CanDeliver(PotionState potionState)
    {
        if (!HasStateAuthority)
            return false;

        return potionState == PotionState.Cooked;
    }

    private int FindOrderIndex(IngredientType ingredient)
    {
        for (int i = 0; i < Orders.Length; i++)
        {
            if (Orders[i].Ingredient == ingredient)
                return i;
        }

        return -1;
    }

    private void CompleteOrder(int index)
    {
        TotalMoney += Orders[index].Reward;

        RemoveOrder(index);
        GenerateOrder();
    }

    private void GenerateOrder()
    {
        IngredientType ingredient = GetRandomIngredient();

        OrderNetworkData order = new()
        {
            Ingredient = ingredient,
            Reward = GetReward(ingredient)
        };

        int emptySlot = FindEmptyOrderSlot();

        if (emptySlot >= 0)
        {
            Orders.Set(emptySlot, order);
            return;
        }

        Orders.Set(0, order);
    }

    private int FindEmptyOrderSlot()
    {
        for (int i = 0; i < Orders.Length; i++)
        {
            if (Orders[i].Ingredient == IngredientType.None)
                return i;
        }

        return -1;
    }

    private void RemoveOrder(int index)
    {
        Orders.Set(index, new OrderNetworkData
        {
            Ingredient = IngredientType.None,
            Reward = 0
        });
    }

    private int GetReward(IngredientType ingredient)
    {
        return IsPrimaryIngredient(ingredient)
            ? PrimaryReward
            : SecondaryReward;
    }

    private bool IsPrimaryIngredient(IngredientType ingredient)
    {
        return ingredient == IngredientType.Red ||
               ingredient == IngredientType.Blue ||
               ingredient == IngredientType.Yellow;
    }

    private void RefreshUI()
    {
        UpdateMoneyText();
        RebuildCards();
    }

    private void UpdateMoneyText()
    {
        if (_totalMoneyText != null)
            _totalMoneyText.text = $"Money: ${TotalMoney}";
    }

    private void RebuildCards()
    {
        ClearCards();

        for (int i = 0; i < Orders.Length; i++)
        {
            OrderNetworkData order = Orders[i];

            if (order.Ingredient == IngredientType.None)
                continue;

            CreateCard(order);
        }
    }

    private void CreateCard(OrderNetworkData order)
    {
        GameObject card = Instantiate(
            _orderCardPrefab,
            _orderContainer
        );

        _spawnedCards.Add(card);

        Image potionImage = card.transform
            .Find("PotionImage")
            .GetComponent<Image>();

        TMP_Text rewardText = card.transform
            .Find("RewardText")
            .GetComponent<TMP_Text>();

        potionImage.color = GetIngredientColor(order.Ingredient);
        rewardText.text = $"${order.Reward}";
    }

    private void ClearCards()
    {
        foreach (GameObject card in _spawnedCards)
        {
            if (card != null)
                Destroy(card);
        }

        _spawnedCards.Clear();
    }

    private IngredientType GetRandomIngredient()
    {
        int random = Random.Range(0, 6);

        return random switch
        {
            0 => IngredientType.Red,
            1 => IngredientType.Blue,
            2 => IngredientType.Yellow,
            3 => IngredientType.Green,
            4 => IngredientType.Orange,
            _ => IngredientType.Purple
        };
    }

    private Color GetIngredientColor(IngredientType ingredient)
    {
        return ingredient switch
        {
            IngredientType.Red => Color.red,
            IngredientType.Blue => Color.blue,
            IngredientType.Yellow => Color.yellow,
            IngredientType.Green => Color.green,
            IngredientType.Orange => new Color(1f, 0.5f, 0f),
            IngredientType.Purple => new Color(0.5f, 0f, 1f),
            _ => Color.white
        };
    }

    public struct OrderNetworkData : INetworkStruct
    {
        public IngredientType Ingredient;
        public int Reward;
    }
}