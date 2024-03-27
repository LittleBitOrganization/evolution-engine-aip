using System;
using System.Collections.Generic;
using LittleBit.Modules.IAppModule.Data.ProductWrappers;
using LittleBit.Modules.IAppModule.Data.Purchases;
using LittleBit.Modules.IAppModule.Services.PurchaseProcessors;
using LittleBit.Modules.IAppModule.Services.TransactionsRestorers;
using LittleBitGames.Environment.Events;
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
        
        private ConfigurationBuilder _builder;
        private IStoreController _controller;
        private IExtensionProvider _extensionProvider;

        private readonly ITransactionsRestorer _transactionsRestorer;
        private readonly IPurchaseHandler _purchaseHandler;
        private readonly List<OfferConfig> _offerConfigs;
        public event Action<string> OnPurchasingSuccess;
        public event Action<string> OnPurchasingFailed;
        public event Action OnInitializationComplete;

        public bool IsInitialized { get; private set; }

        private readonly ProductCollections _productCollection;

        public IAPService(ITransactionsRestorer transactionsRestorer,
            IPurchaseHandler purchaseHandler, List<OfferConfig> offerConfigs)
        {
            _productCollection = new ProductCollections();
            _purchaseHandler = purchaseHandler;
            _offerConfigs = offerConfigs;
            _transactionsRestorer = transactionsRestorer;
            Init();
         
            UnityPurchasing.Initialize(this, _builder);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _extensionProvider = extensions;
            _controller = controller;

            _productCollection.AddUnityIAPProductCollection(controller.products);

            OnInitializationComplete?.Invoke();
            IsInitialized = true;
        }

        private void Init()
        {
            _builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

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
#if IAP_DEBUG || UNITY_EDITOR
            var product = (GetProductWrapper(id) as EditorProductWrapper);

            if (product is null) return;
            
            if (!product.Metadata.CanPurchase) return;
            
            product!.Purchase();
            OnPurchasingSuccess?.Invoke(id);
            PurchasingProductSuccess(id);
#else

            var product = _controller.products.WithID(id);

            if (product is {availableToPurchase: false}) return;

            if (freePurchase)
            {
                OnPurchasingSuccess?.Invoke(id);
                PurchasingProductSuccess(id);
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

        public void RestorePurchasedProducts(Action<bool, string> callback) =>
            _transactionsRestorer.Restore(_extensionProvider, callback);

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError("Initialization failed - !" + error);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError("Initialization failed - !"+error+ ". Message " + message);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            var result = _purchaseHandler.ProcessPurchase(purchaseEvent, (success) =>
            {
                var id = purchaseEvent.purchasedProduct.definition.id;

                if (success)
                {
#if IAP_DEBUG || UNITY_EDITOR
                    (GetProductWrapper(id) as EditorProductWrapper)!.Purchase();
#endif
                    OnPurchasingSuccess?.Invoke(id);
                    _controller.ConfirmPendingPurchase(purchaseEvent.purchasedProduct);
                    PurchasingProductSuccess(id);
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

        private void PurchasingProductSuccess(string productId)
        {
            var product = GetProductWrapper(productId);
            var metadata = product.Metadata;
            var definition = product.Definition;
            var receipt = product.TransactionData.Receipt;

            var data = new DataEventEcommerce(
                metadata.CurrencyCode,
                (double) metadata.LocalizedPrice,
                ItemType, definition.Id,
                CartType, receipt,
                Signature);       
            OnPurchasingProductSuccess?.Invoke(data);
        }
    }
}