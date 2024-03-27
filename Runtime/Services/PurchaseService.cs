using System;
using System.Collections.Generic;
using LittleBit.Modules.IAppModule.Commands.Factory;
using LittleBit.Modules.IAppModule.Data.ProductWrappers;
using LittleBit.Modules.IAppModule.Data.Purchases;
using UnityEngine;

namespace LittleBit.Modules.IAppModule.Services
{
    public class PurchaseService : IService
    {
        public event Action OnInitialized;
        public event Action<string> OnPurchaseSuccess;
        public bool IsInitialized { get; private set; }

        private readonly PurchaseHandler _purchaseHandler;

        private readonly IIAPService _iapService;
        
        private bool PurchaseRestored
        {
            get => PlayerPrefs.GetInt("PurchaseRestored", 0) == 1;
            set => PlayerPrefs.SetInt("PurchaseRestored", value ? 1 : 0);
        }
        
        public PurchaseService(IIAPService iapService,
            PurchaseCommandFactory purchaseCommandFactory,
            List<OfferConfig> offerConfigs)
        {
            _iapService = iapService;
            _purchaseHandler = new PurchaseHandler(this, iapService, purchaseCommandFactory, offerConfigs);
            _iapService.OnPurchasingSuccess += (s) => OnPurchaseSuccess?.Invoke(s);
            Subscribe();
        }

        public void Purchase(OfferConfig offer, Action<bool> callback) => _purchaseHandler.Purchase(offer, callback);

        public void UnlockContent(OfferConfig offer) => _purchaseHandler.Purchase(offer, null, true);
        
        public IProductWrapper GetProductWrapper(string id) => _iapService.GetProductWrapper(id);

        public IProductWrapper GetProductWrapper(OfferConfig offerConfig) => GetProductWrapper(offerConfig.Id);

        private void Subscribe()
        {
            if (_iapService.IsInitialized)
            {
                OnInitializationComplete();
                return;
            }
            
            _iapService.OnInitializationComplete += OnInitializationComplete;
        }

        private void OnInitializationComplete()
        {
            IsInitialized = true;

            if (!PurchaseRestored)
            {
                _iapService.RestorePurchasedProducts(Callback);
            }
            
            OnInitialized?.Invoke();
        }

        private void Callback(bool obj, string message)
        {
            if (obj)
            {
                PurchaseRestored = true;
                
                Debug.LogError("Restore complete!");
                Debug.LogError(message);
            }
        }
    }
}