using LittleBit.Modules.IAppModule.Commands.Factory;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Purchasing;

namespace LittleBit.Modules.IAppModule.Data.Products
{
    public abstract class ProductConfig : ScriptableObject, IProduct
    {
        [SerializeField] private ProductType productType = ProductType.Consumable;

        [SerializeField, DisableIf(nameof(_overrideIos))] private string id;
        
        [SerializeField] private bool _overrideIos = false;
        [SerializeField, ShowIf(nameof(_overrideIos))] private string _iosId;
    

        
        public string Id
        {
            get
            {
#if UNITY_IOS
                if (_overrideIos)
                {
                    return _iosId;
                }
                else
                {
                    return id;
                }
#endif
                return id;
            }
            protected set => id = value;
        }

        public ProductType ProductType => productType;

        public abstract void HandlePurchase(PurchaseCommandFactory purchaseCommandFactory);
    }
}