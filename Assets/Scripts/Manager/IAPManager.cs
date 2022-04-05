using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class IAPManager : Singleton<IAPManager>, IStoreListener
{
    public System.Action onSuccess;
    public System.Action onFail;

    private static IStoreController m_StoreController;                  // ���� ������ �����ϴ� �Լ��� ����
    private static IExtensionProvider m_StoreExtensionProvider;     // ���� �÷����� ���� Ȯ�� ó���� ����

    public const string dia_product_1 = "com.ablegames.sidetab.diaset1";
    public const string dia_product_2 = "com.ablegames.sidetab.diaset2";
    public const string dia_product_3 = "com.ablegames.sidetab.diaset3";
    public const string dia_product_4 = "com.ablegames.sidetab.diaset4";
    public const string dia_product_5 = "com.ablegames.sidetab.diaset5";
    public const string weekly_product = "com.ablegames.sidetab.weeklypackage";

    public static IAPManager m_Instance;

    public bool IsInitialized => m_StoreController != null && m_StoreExtensionProvider != null;

    protected override void AwakeInstance()
    {
    }

    protected override void DestroyInstance() { }

    void Start()
    {
        InitializePurchasing();
    }

    void InitializePurchasing()
    {
        if (IsInitialized)
            return;

        // ���ο� ������ �����ϴ� ����.
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        foreach (var shopData in LevelData.Instance.ShopDatas)
        {
            if(shopData.productID != null && shopData.productID != "")
                builder.AddProduct(shopData.productID, ProductType.Consumable);
        }
        //builder.AddProduct(dia_product_1, UnityEngine.Purchasing.ProductType.Consumable);
        //builder.AddProduct(dia_product_2, UnityEngine.Purchasing.ProductType.Consumable);
        //builder.AddProduct(dia_product_3, UnityEngine.Purchasing.ProductType.Consumable);
        //builder.AddProduct(dia_product_4, UnityEngine.Purchasing.ProductType.Consumable);
        //builder.AddProduct(dia_product_5, UnityEngine.Purchasing.ProductType.Consumable);
        //builder.AddProduct(weekly_product, UnityEngine.Purchasing.ProductType.Consumable);

        UnityPurchasing.Initialize(this, builder);
    }

    // �ȵ���̵�� �ڵ����� ������ �Ǳ� ������ IOS�ʸ� ������ ����. IOS�� ���� ����� �������� ���� �����ؾ� �Ѵ�. ���� ���� ���� ��ư�� IOS������ ���� �ؾ� �ϴ��� �˾ƺ���. �� ���� �϶�� �ϴ���.
    public void RestorePurchases()
    {
        if (!IsInitialized) return;

        if (Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            Debug.Log("������� ����");

            var apple = m_StoreExtensionProvider.GetExtension<IAppleExtensions>();

            apple.RestoreTransactions((result) =>
            {
                Debug.Log($" ���� ���� �õ� ��� {result} ");
            });
        }
        {
            Debug.Log($" �������� �ʴ� �÷����Դϴ�. Current = {Application.platform} ");
        }

    }

    /// <summary>
    /// ���ſ� ��ǰ�� ���� �߾����� ���θ� ���� - NonConsumable, Subscription�� ������ ������ �����ü� ������ ��밡��. ���� ���� Consumable�� ��� �Ұ�.
    /// </summary>
    /// <param name="productID"></param>
    public bool HadPurchased(string productID)
    {
        if (!IsInitialized) return false;

        // if (CodelessIAPStoreListener.Instance.StoreController.products.WithID(productID).hasReceipt)

        var product = m_StoreController.products.WithID(productID);


        if (product != null)
        {
            return product.hasReceipt;
        }

        return false;

    }

    public void BuyProductID(string productID, System.Action _onSuccess, System.Action _onFail)
    {
        onSuccess = _onSuccess;
        onFail = _onFail;

        if (IsInitialized)
        {
            // �ش� ID�� �ش��ϴ� ��ǰ ������Ʈ�� ��ȯ�Ѵ�.
            Product product = m_StoreController.products.WithID(productID);

            //product.receipt // ���� ���� Consumable�� ���������� ��ȯ���� �ʴ´�. ���� ��ǰ�̳� ��Ų���� ��ǰ�� ���������� ��ȯ �ȴ�. 

            // ��ǰ�� ���� �԰�, �� ��ǰ�� ���������� ���Ű� ������ ��ǰ���� Ȯ��.
            if (product != null && product.availableToPurchase)
            {
                Debug.Log($"���� �õ� ��ǰ�� ID : {product.definition.id}");

                // ���� ����
                m_StoreController.InitiatePurchase(productID);

            }
            else
            {
                Debug.Log("���� �õ� �Ұ�");
                onFail?.Invoke();
            }
        }
        else
        {
            Debug.Log("�ʱ�ȭ ����");
            onFail?.Invoke();
        }
    }


    /// <summary>
    /// �ʱ�ȭ�� ����ġ�� ȣ��
    /// </summary>
    /// <param name="controller">���� ������ �����ϴ� �Լ��� ����</param>
    /// <param name="extensions">���� �÷����� ���� Ȯ�� ó���� ����</param>
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        m_StoreController = controller;
        m_StoreExtensionProvider = extensions;
    }

    /// <summary>
    /// �ʱ�ȭ ����
    /// </summary>
    /// <param name="error">���� ����</param>
    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log($"�ʱ�ȭ ���� {error}");
    }


    /// <summary>
    /// ���� �Ϸ�� �ƴϰ� ���� �Ϸ� ������ ȣ��Ǵ� �Լ�. ���� �Ϸ� ������ ó�� �ؾ� �� �κ��� ������ ���⼭ ó���Ѵ�. ���Ű� ���� �Ϸ�� ��Ȳ�̴�.
    /// �����߰� Ȥ�� ��Ų�߰� ���� ó��.
    /// </summary>
    /// <param name="purchaseEvent">������ ��ǰ�� ����</param>
    /// <returns></returns>
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
    {
        Debug.Log($"������ ��ǰ�� ������ �� �� �ִ�. ID : {purchaseEvent.purchasedProduct.definition.id}");

        BackEndServerManager.Instance.PurchaseReceipt(purchaseEvent.purchasedProduct);

        if (onSuccess != null)
            onSuccess();

        //if (purchaseEvent.purchasedProduct.definition.id == coin_product_1)
        //{
        //    //if (PlayerPrefs.HasKey("ads") == false)
        //    //{
        //    //    PlayerPrefs.SetInt("ads", 0);
        //    //    panelAds.SetActive(false);
        //    //    AdsCore.S.HidBanner();    // Advertisment.Banner.Hide();
        //    //    AdsCore.S.StopAllCoroutines();
        //    //}
        //    //Debug.Log("��� �߰� ó��");

        //    Product_Coin50();
        //}

        // ����ó�� �ϷḦ ����.
        return PurchaseProcessingResult.Complete;
    }


    /// <summary>
    /// ���� ����
    /// </summary>
    /// <param name="product">������ ��ǰ�� ����</param>
    /// <param name="failureReason">�� ���� �ߴ����� ���� ����</param>
    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        if (onFail != null)
            onFail();

        Debug.Log($"���� ���� : {failureReason}");
    }

    public Product GetProduct(string _productId)
    {
        return m_StoreController.products.WithID(_productId);
    }

    public Product[] GetProductAll()
    {
        return m_StoreController.products.all;
    }

    public string GetPrice(string _productId)
    {
        //Debug.Log($" {Application.systemLanguage} , { GetProduct(_productId).metadata.localizedPrice } , " +
        //    $"{GetProduct(_productId).metadata.isoCurrencyCode} ,{GetProduct(_productId).metadata.localizedPriceString} , " +
        //    $"{GetProduct(_productId).metadata.localizedTitle}");

        //return string.Format("{0} {1}", GetProduct(_productId).metadata.localizedPrice, GetProduct(_productId).metadata.isoCurrencyCode);
        return $"{GetProduct(_productId).metadata.localizedPriceString}";
    }

    public decimal GetPriceToDecimal(string _productId)
    {
        return GetProduct(_productId).metadata.localizedPrice;
    }

    // �Ʒ� ���� �κ��� ���� �ʿ��Ҷ� ���� �� �� �ֵ��� ����.
    /*
    public void RestoreVariable() // ���� ������ Ȯ�� �� �˸� ǥ��.
    {
        if (PlayerPrefs.HasKey("ads"))
        {
            btnNotAds.SetActive(false);
        }
        if (PlayerPrefs.HasKey("vip"))
        {
            btnVip.SetActive(false);
            btnVip_afterBuy.Setactive(true);
        }
        if (PlayerPrefs.HasKey("ads") && PlayerPrefs.HasKey("vip"))
        {
            vipBanner.SetActive(true);
        }
    }
    public void RestoreHadItem()  // ���� �����ϰ� �ٽ� ��ġ�Ͽ��� ��� ������ �������� �����ؾ� �Ҷ� ����. 
    {
        if (PlayerPrefs.HasKey("firstStart") == false)
        {
            PlayerPrefs.SetInt("firstStart");
            RestoreMyProduct();
        }
    }
    public void RestoreMyProduct()    // �������� ����. - NonConsumable, Subscription�� ������ ������ �����ü� ������ ��밡��. ���� ���� Consumable�� ��� �Ұ�
    {
        if (CodelessIAPStoreListener.Instance.StoreController.products.WithID("noads").hasReceipt)
            Product_NoAds();
        if (CodelessIAPStoreListener.Instance.StoreController.products.WithID("vip").hasReceipt)
            Product_VIP();
    
        RestoreVariable(); 
    }
    */
}
