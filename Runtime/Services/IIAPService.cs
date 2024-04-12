using System;
using LittleBit.Modules.IAppModule.Data.ProductWrappers;

namespace LittleBit.Modules.IAppModule.Services
{
    public interface IIAPService
    {
        public event Action<bool, string> OnPurchasingRestored;
        public event Action<string> OnPurchasingSuccess;
        public event Action<string> OnPurchasingFailed;
        public event Action OnInitializationComplete;
        bool IsInitialized { get; }
        public bool PurchaseRestored { get; }
        public void Purchase(string id, bool freePurchase = false);
        public void RestorePurchasedProducts();
        public IProductWrapper GetProductWrapper(string id);
    }
}