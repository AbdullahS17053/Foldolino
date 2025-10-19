using UnityEngine;
using UnityEngine.Purchasing;

public class InAppManager : MonoBehaviour
{
    private void Start()
    {
        if (PlayerPrefs.GetInt("BuyPremium", 0) == 1)
            LocalDrawManager.instance.premiumBtn.SetActive(false);
    }

    public void OnPurchaseSuccess()
    {
        PlayerPrefs.SetInt("BuyPremium", 1);
        LocalDrawManager.instance.premiumBtn.SetActive(false);
    }

    public void Restore(Product product)
    {
        if (product.definition.id.Equals("com.Foldolino.BuyPremium"))
        {
            OnPurchaseSuccess();
        }
    }

    public void HostGame()
    {
        if (PlayerPrefs.GetInt("BuyPremium", 0) == 1)
            LocalDrawManager.instance.hostPanel.SetActive(true);
        else
            LocalDrawManager.instance.premiumPanel.SetActive(true);
    }
}
