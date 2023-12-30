using TMPro;
using UnityEngine;

public class HouseMenu : MonoBehaviour
{
    [SerializeField] RectTransform highlight;
    [SerializeField] TMP_Text upgradePriceText;
    [SerializeField] TMP_Text repairPriceText;

    int index = 0;

    bool locked = false;

    public void UpgradePrices()
    {
        Debug.Log("Prices upgraded");
        repairPriceText.text = House.Instance.RepairPrice.ToString();
        upgradePriceText.text = House.Instance.UpgradePrice.ToString();
    }

    public void Move(int value)
    {
        if(value==0)
        {
            locked = false;
        }
        else if(!locked)
        {
            if (value > 0)
            {
                locked = true;
                index++;
                if (index > 2)
                {
                    index = 0;
                }
            }
            else if (value < 0)
            {
                locked = true;
                index--;
                if(index < 0)
                {
                    index = 2;
                }
            }

            HighlightIndex();
        }        
    }

    public void Choose()
    {
        switch(index)
        {
            case 0:
                House.Instance.Upgrade();
                UpgradePrices();
                break;
            case 1: 
                House.Instance.Repair();
                break;
        }
    }

    void HighlightIndex()
    {
        int x=0;
        switch(index)
        {
            case 0:
                x = -200;
                break;
            case 2:
                x = 200;
                break;
        }
        highlight.anchoredPosition = new(x, 0);
    }
}
