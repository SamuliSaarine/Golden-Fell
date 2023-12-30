using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class House : MonoBehaviour, IDamageable
{
    #region Variables

    /* SERIALIZED */

    [SerializeField] SpriteRenderer model;
    [SerializeField] HouseType[] types;

    [SerializeField] Slider healthSlider;
    [SerializeField] TMP_Text goldText;

    /* PRIVATE */

    int currentType = 0;

    int maxHealth;
    float health;

    int maxStorage;
    int gold;

    /* PUBLIC */

    public int RepairPrice { get; set; }
    public int UpgradePrice { get; set; }

    public static House Instance;

    #endregion

    #region Functions

    public void Awake()
    {
        if(Instance != null)
        {
            Destroy(Instance);
        }

        Instance = this;

        InitType();
    }

    public void Repair()
    {
        if(gold >= RepairPrice)
        {
            gold -= RepairPrice;
            ChangeHealth(maxHealth);
        }
    }

    public int AddGold(int amount)
    {
        gold+=amount;
        //Checking if we have collected enough gold to win the game
        if (currentType == types.Length-1 && gold >= maxStorage)
        {
            Debug.Log("Game won");
            StartCoroutine(PlayerManager.Instance.EndGame($"You collected {maxStorage} gold", false));
            return 0;
        }
        else
        {
            int spare = 0;
            if(gold>maxStorage)
            {
                //returning gold that doesn't fit in storage
                spare = gold - maxStorage;
                gold = maxStorage;
            }
            goldText.text = gold.ToString();
            return spare;
        }
        
    }

    public void Upgrade()
    {
        if(gold >= UpgradePrice)
        {
            PlayerManager.Instance.PlayWinSound();
            gold -= UpgradePrice;
            currentType++;
            InitType();
        } 
    }

    void InitType()
    {
        var data = types[currentType].GetData();
        model.sprite = data.Item1;
        maxHealth = data.Item2;
        ChangeHealth(maxHealth);
        maxStorage = data.Item3; 
        if(currentType<types.Length-1)
        {
            UpgradePrice = types[currentType + 1].GetPrice;
        }
        else
        {
            UpgradePrice = maxStorage;
        }
    }

    public void Damage()
    {
        ChangeHealth(health - Time.deltaTime);
    }

    public static float Distance(Vector3 myPos) {
        return Vector3.Distance(myPos,Instance.transform.position);
    }

    void ChangeHealth(float value)
    {
        health = value;

        if (health <= 0)
        {
            StartCoroutine(PlayerManager.Instance.EndGame("House got destroyed"));
            return;
        }

        healthSlider.value = health / maxHealth;
        RepairPrice = (int)((maxHealth - health) / maxHealth * types[currentType].GetPrice);
    }

    private void OnDestroy()
    {
        Destroy(Instance);
        Instance = null;
    }
    #endregion
}

[System.Serializable]
public class HouseType
{
    [SerializeField] Sprite icon;
    [SerializeField] int maxHealth;
    [SerializeField] int maxStorage;
    [SerializeField] int price;

    public (Sprite, int, int) GetData()
    {
        return (icon, maxHealth, maxStorage);
    }

    public int GetPrice { get { return price; } }
}