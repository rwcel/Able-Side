using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class IAPManager : Singleton<IAPManager>, IStoreListener
{
    public System.Action onSuccess;
    public System.Action onFail;

    private static IStoreController m_StoreController;                  // 구매 과정을 제어하는 함수를 제공
    private static IExtensionProvider m_StoreExtensionProvider;     // 여러 플랫폼을 위한 확장 처리를 제공

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

        // 새로운 빌더를 생성하는 과정.
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

    // 안드로이드는 자동으로 복구가 되기 때문에 IOS쪽만 복구를 구현. IOS는 복구 기능을 수동으로 직접 구현해야 한다. 이전 구매 복구 버튼을 IOS에서는 구현 해야 하는지 알아보자. 꼭 구현 하라고 하더라.
    public void RestorePurchases()
    {
        if (!IsInitialized) return;

        if (Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            Debug.Log("리스토어 시작");

            var apple = m_StoreExtensionProvider.GetExtension<IAppleExtensions>();

            apple.RestoreTransactions((result) =>
            {
                Debug.Log($" 구매 복구 시도 결과 {result} ");
            });
        }
        {
            Debug.Log($" 지원하지 않는 플랫폼입니다. Current = {Application.platform} ");
        }

    }

    /// <summary>
    /// 과거에 상품을 구매 했었는지 여부를 리턴 - NonConsumable, Subscription만 영수증 정보를 가져올수 있으니 사용가능. 코인 같은 Consumable은 사용 불가.
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
            // 해당 ID에 해당하는 상품 오브젝트를 반환한다.
            Product product = m_StoreController.products.WithID(productID);

            //product.receipt // 코인 같은 Consumable은 영수증값이 반환되지 않는다. 구독 상품이나 스킨같은 상품은 영수증값이 반환 된다. 

            // 상품을 가져 왔고, 그 상품이 정상적으로 구매가 가능한 상품인지 확인.
            if (product != null && product.availableToPurchase)
            {
                Debug.Log($"구매 시도 상품의 ID : {product.definition.id}");

                // 구매 시작
                m_StoreController.InitiatePurchase(productID);

            }
            else
            {
                Debug.Log("구매 시도 불가");
                onFail?.Invoke();
            }
        }
        else
        {
            Debug.Log("초기화 실패");
            onFail?.Invoke();
        }
    }


    /// <summary>
    /// 초기화를 끝마치면 호출
    /// </summary>
    /// <param name="controller">구매 과정을 제어하는 함수를 제공</param>
    /// <param name="extensions">여러 플랫폼을 위한 확장 처리를 제공</param>
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        m_StoreController = controller;
        m_StoreExtensionProvider = extensions;
    }

    /// <summary>
    /// 초기화 실패
    /// </summary>
    /// <param name="error">실패 내용</param>
    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log($"초기화 실패 {error}");
    }


    /// <summary>
    /// 구매 완료는 아니고 구매 완료 직전에 호출되는 함수. 구매 완료 시점에 처리 해야 할 부분이 있으면 여기서 처리한다. 구매가 거의 완료된 상황이다.
    /// 코인추가 혹은 스킨추가 등의 처리.
    /// </summary>
    /// <param name="purchaseEvent">구매한 상품의 정보</param>
    /// <returns></returns>
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
    {
        Debug.Log($"구매한 상품의 정보를 볼 수 있다. ID : {purchaseEvent.purchasedProduct.definition.id}");

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
        //    //Debug.Log("골드 추가 처리");

        //    Product_Coin50();
        //}

        // 구매처리 완료를 리턴.
        return PurchaseProcessingResult.Complete;
    }


    /// <summary>
    /// 구매 실패
    /// </summary>
    /// <param name="product">실패한 상품의 정보</param>
    /// <param name="failureReason">왜 실패 했는지에 대한 이유</param>
    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        if (onFail != null)
            onFail();

        Debug.Log($"구매 실패 : {failureReason}");
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

    // 아래 예시 부분은 추후 필요할때 적용 할 수 있도록 유지.
    /*
    public void RestoreVariable() // 구매 정보를 확인 후 알림 표시.
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
    public void RestoreHadItem()  // 앱을 삭제하고 다시 설치하였을 경우 구매한 아이템을 복원해야 할때 실행. 
    {
        if (PlayerPrefs.HasKey("firstStart") == false)
        {
            PlayerPrefs.SetInt("firstStart");
            RestoreMyProduct();
        }
    }
    public void RestoreMyProduct()    // 아이템을 복원. - NonConsumable, Subscription만 영수증 정보를 가져올수 있으니 사용가능. 코인 같은 Consumable은 사용 불가
    {
        if (CodelessIAPStoreListener.Instance.StoreController.products.WithID("noads").hasReceipt)
            Product_NoAds();
        if (CodelessIAPStoreListener.Instance.StoreController.products.WithID("vip").hasReceipt)
            Product_VIP();
    
        RestoreVariable(); 
    }
    */
}
