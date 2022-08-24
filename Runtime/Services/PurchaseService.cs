﻿using System;
using System.Collections.Generic;
using LittleBit.Modules.IAppModule.Commands.Factory;
using LittleBit.Modules.IAppModule.Data.ProductWrappers;
using LittleBit.Modules.IAppModule.Data.Purchases;

namespace LittleBit.Modules.IAppModule.Services
{
    public class PurchaseService : IService
    {
        public event Action OnInitialized;
        public bool IsInitialized { get; private set; }

        private readonly PurchaseHandler _purchaseHandler;

        private readonly IIAPService _iapService;

        public PurchaseService(IIAPService iapService,
            PurchaseCommandFactory purchaseCommandFactory,
            List<OfferConfig> offerConfigs)
        {
            _iapService = iapService;
            _purchaseHandler = new PurchaseHandler(this, iapService, purchaseCommandFactory, offerConfigs);

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

            OnInitialized?.Invoke();
        }
    }
}