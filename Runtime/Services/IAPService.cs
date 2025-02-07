using System;
using System.Collections.Generic;
using LittleBit.Modules.IAppModule.Data;
using LittleBit.Modules.IAppModule.Data.ProductWrappers;
using LittleBit.Modules.IAppModule.Data.Purchases;
using LittleBit.Modules.IAppModule.Services.PurchaseProcessors;
using LittleBit.Modules.IAppModule.Services.TransactionsRestorers;
using LittleBitGames.Environment.Events;
using LittleBitGames.Environment.Purchase;
using Purchase;
using UnityEngine;
using UnityEngine.Purchasing;

namespace LittleBit.Modules.IAppModule.Services
{
    public partial class IAPService : IService, IStoreListener, IIAPService,IIAPRevenueEvent
    {
        //ToDo понять что это такое)
        private const string CartType = "Shop";
        private const string Signature = "VVO";
        private const string ItemType = "Offer";

        private bool _isRestorePurchase;
        
        private ConfigurationBuilder _builder;
        private IStoreController _controller;
        private IExtensionProvider _extensionProvider;
        private AppStore _appStore;
        
        private readonly ProductCollections _productCollection;
        private readonly ITransactionsRestorer _transactionsRestorer;
        private readonly IPurchaseHandler _purchaseHandler;
        private readonly List<OfferConfig> _offerConfigs;
        private readonly List<IPurchaseValidator> _purchaseValidators;
        public event Action<string, RecieptHandler> OnPurchasingSuccess;
        public event Action<string> OnPurchasingFailed;
        public event Action<bool, string> OnPurchasingRestored;
        public event Action OnInitializationComplete;

        public bool IsInitialized { get; private set; }

        public bool PurchaseRestored
        {
            get => PlayerPrefs.GetInt("PurchaseRestored", 0) == 1;
            private set => PlayerPrefs.SetInt("PurchaseRestored", value ? 1 : 0);
        }

        public IAPService(ITransactionsRestorer transactionsRestorer,
            IPurchaseHandler purchaseHandler, List<OfferConfig> offerConfigs, List<IPurchaseValidator> purchaseValidators)
        {
            _productCollection = new ProductCollections();
            _purchaseHandler = purchaseHandler;
            _offerConfigs = offerConfigs;
            _purchaseValidators = purchaseValidators;
            _transactionsRestorer = transactionsRestorer;
            Init();
         
            UnityPurchasing.Initialize(this, _builder);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _extensionProvider = extensions;
            _controller = controller;

            _productCollection.AddUnityIAPProductCollection(controller.products);

            OnPurchasingRestored += (complete, message) =>
            {
                if (complete)
                {
                    PurchaseRestored = true;

                    Debug.LogError("Restore complete!");
                    Debug.LogError(message);
                }
                
                _isRestorePurchase = false;
            };
            OnInitializationComplete?.Invoke();
            IsInitialized = true;
        }

        private void Init()
        {
            _builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            _appStore = StandardPurchasingModule.Instance().appStore;
            
            _offerConfigs.ForEach(offer =>
            {
                _builder.AddProduct(offer.Id, offer.ProductType);
                
                _productCollection.AddConfig(offer);
            });

#if IAP_DEBUG || UNITY_EDITOR

            OnInitializationComplete?.Invoke();
            IsInitialized = true;
#endif
        }

        public void Purchase(string id, bool freePurchase)
        {
            foreach (var purchaseValidator in _purchaseValidators)
            {
                purchaseValidator.Reset();
            }
            
#if IAP_DEBUG || UNITY_EDITOR
            var product = (GetProductWrapper(id) as EditorProductWrapper);

            if (product is null) return;
            
            if (!product.Metadata.CanPurchase) return;
            
          
            product!.Purchase();
            OnPurchasingSuccess?.Invoke(id, null);
            PurchasingProductSuccess(id, null);
#else

            var product = _controller.products.WithID(id);

            if (product is {availableToPurchase: false}) return;

            if (freePurchase)
            {
                OnPurchasingSuccess?.Invoke(id, null);
                PurchasingProductSuccess(id, null);
                return;
            }

            
            _controller.InitiatePurchase(product);

#endif
        }

        public IProductWrapper GetProductWrapper(string id)
        {
#if IAP_DEBUG || UNITY_EDITOR
            return GetDebugProductWrapper(id);
#else
            try
            {
                return GetRuntimeProductWrapper(id);
            }
            catch
            {
                Debug.LogError($"Can't create runtime product wrapper with id:{id}");
                return null;
            }
#endif
        }

        private RuntimeProductWrapper GetRuntimeProductWrapper(string id) =>
            _productCollection.GetRuntimeProductWrapper(id);

        private EditorProductWrapper GetDebugProductWrapper(string id) =>
            _productCollection.GetEditorProductWrapper(id);

        public void RestorePurchasedProducts()
        {
            if (!PurchaseRestored)
            {
                _isRestorePurchase = true;
                
                if(_appStore == AppStore.GooglePlay || _appStore == AppStore.AppleAppStore)
                    _transactionsRestorer.Restore(_extensionProvider, OnPurchasingRestored);
            }
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError("Initialization failed - !" + error);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError("Initialization failed - !"+error+ ". Message " + message);
        }

        private async void OtherValidate(PurchaseEventArgs purchaseEvent, RecieptHandler receipt)
        {
            var id = purchaseEvent.purchasedProduct.definition.id;
            
            foreach (var purchaseValidator in _purchaseValidators)
            {
                var result = await purchaseValidator.ValidateAsync();
                if (result == false)
                {
                    OnPurchasingFailed?.Invoke(id);
                    return;
                }
            }
         
            OnPurchasingSuccess?.Invoke(id, receipt);
            _controller.ConfirmPendingPurchase(purchaseEvent.purchasedProduct);
            PurchasingProductSuccess(id, receipt);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            var result = _purchaseHandler.ProcessPurchase(purchaseEvent, (success, receipt) =>
            {
                var id = purchaseEvent.purchasedProduct.definition.id;
                if (success)
                {
#if IAP_DEBUG || UNITY_EDITOR
                    (GetProductWrapper(id) as EditorProductWrapper)!.Purchase();
#endif
                    OtherValidate(purchaseEvent, receipt);
                    
                }
                else
                    OnPurchasingFailed?.Invoke(id);
            });

            return result;
        }
        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            OnPurchasingFailed?.Invoke(product.definition.id);
            Debug.LogError("Purchasing failed!");
        }

        public event Action<IDataEventEcommerce> OnPurchasingProductSuccess;

        private void PurchasingProductSuccess(string productId, RecieptHandler receipt)
        {
            var product = GetProductWrapper(productId);
            var metadata = product.Metadata;
            var definition = product.Definition;
            var stringReceipt = product.TransactionData.Receipt;

            if (!_isRestorePurchase)
            {
                var data = new DataEventEcommerce(
                    metadata.CurrencyCode,
                    (double) metadata.LocalizedPrice,
                    ItemType, definition.Id,
                    CartType, stringReceipt,
                    Signature, product.TransactionData.TransactionId,
                    receipt);       
                OnPurchasingProductSuccess?.Invoke(data);
            }
        }
    }
}
