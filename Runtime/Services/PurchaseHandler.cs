using System;
using System.Collections.Generic;
using System.Linq;
using LittleBit.Modules.IAppModule.Commands.Factory;
using LittleBit.Modules.IAppModule.Data.Purchases;
using Purchase;

namespace LittleBit.Modules.IAppModule.Services
{
    internal class PurchaseHandler
    {
        private readonly IIAPService _iapService;
        private readonly PurchaseCommandFactory _purchaseCommandFactory;
        private readonly PurchaseService _purchaseService;
        private readonly List<OfferConfig> _offers;

        private Action<bool> _callback;
        private bool _isPurchasing;

        public PurchaseHandler(PurchaseService purchaseService, IIAPService iapService,
            PurchaseCommandFactory purchaseCommandFactory, List<OfferConfig> offers)
        {
            _offers = offers;
            _purchaseService = purchaseService;
            _iapService = iapService;
            _purchaseCommandFactory = purchaseCommandFactory;

            Subscribe();
        }

        private void Subscribe()
        {
            _iapService.OnPurchasingSuccess += OnPurchasingSuccess;
            _iapService.OnPurchasingFailed += OnPurchasingFailed;
        }

        public void Purchase(OfferConfig offer, Action<bool> callback = null, bool freePurchase = false)
            => Purchase(offer.Id, callback, freePurchase);

        public void Purchase(string id, Action<bool> callback, bool freePurchase)
        {
            if (!_purchaseService.IsInitialized) return;

            if (_isPurchasing) return;

            if (!_iapService.GetProductWrapper(id).Metadata.CanPurchase) return;

            _isPurchasing = true;
            _callback = callback;

            if (freePurchase)
            {
                OnPurchasingSuccess(id, null);
                return;
            }

            _iapService.Purchase(id, freePurchase);
        }
        

        private void OnPurchasingSuccess(string id, RecieptHandler reciept)
        {
            if (!_purchaseService.IsInitialized) return;

            var offer = _offers.FirstOrDefault(x => x.Id == id);

            if (offer == null) return;

            offer.HandlePurchase(_purchaseCommandFactory);

            CompletePurchase(() => { _callback?.Invoke(true); });
        }

        private void OnPurchasingFailed(string id)
            => CompletePurchase(() => { _callback?.Invoke(false); });

        private void CompletePurchase(Action callback)
        {
            callback?.Invoke();

            _callback = null;
            _isPurchasing = false;
        }
    }
}