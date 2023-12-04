using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Purchasing;

namespace LittleBit.Modules.IAppModule.Data.ProductWrappers
{
    public class RuntimeProductWrapper : ProductWrapper
    {
        public RuntimeProductWrapper(Product product)
        {
            TransactionData = new()
            {
                HasReceiptGetter = () => product.hasReceipt,
                ReceiptGetter = () => product.receipt,
                TransactionIdGetter = () => product.transactionID
            };

            Metadata = new()
            {
                CurrencyCode = product.metadata.isoCurrencyCode,
                CurrencySymbol = CurrencyCodeMapper.GetSymbol(product.metadata.isoCurrencyCode),
                LocalizedDescription = product.metadata.localizedDescription,
                LocalizedPrice = product.metadata.localizedPrice,
                LocalizedPriceString = product.metadata.localizedPriceString,
                LocalizedTitle = product.metadata.localizedTitle,
                CanPurchaseGetter = () =>
                    Definition.Type.Equals(ProductType.Consumable) ||
                    !Metadata.IsPurchasedGetter.Invoke(),
                IsPurchasedGetter = () => TransactionData.HasReceipt ||
                                          !string.IsNullOrEmpty(TransactionData.Receipt) ||
                                          !string.IsNullOrEmpty(TransactionData.TransactionId)
            };

            Definition = new()
            {
                Id = product.definition.id,
                Type = product.definition.type
            };
        }
        
        public RuntimeProductWrapper(ProductParams product)
        {
            TransactionData = new()
            {
                HasReceiptGetter = () => product.HasReceipt,
                ReceiptGetter = () => product.Receipt,
                TransactionIdGetter = () => product.TransactionId
            };

            Metadata = new()
            {
                CurrencyCode = product.CurrencyCode,
                CurrencySymbol = CurrencyCodeMapper.GetSymbol(product.CurrencyCode),
                LocalizedDescription = product.LocalizedDescription,
                LocalizedPrice = product.LocalizedPrice,
                LocalizedPriceString = product.LocalizedPriceString,
                LocalizedTitle = product.LocalizedTitle,
                CanPurchaseGetter = () =>
                    Definition.Type.Equals(ProductType.Consumable) ||
                    !Metadata.IsPurchasedGetter.Invoke(),
                IsPurchasedGetter = () => PlayerPrefs.GetInt("MixPurchaseProduct"+Definition.Id,0)>0
            };

            Definition = new()
            {
                Id = product.Id,
                Type = ConvertProductType(product.Type)
            };
        }
        private ProductType ConvertProductType(int itemType)
        {
            switch (itemType)
            {
                case 0: { return ProductType.Consumable;    }
                case 1: { return ProductType.NonConsumable; }
                case 2: { return  ProductType.Subscription; }
                default: {
                    Debug.LogErrorFormat("Can not find the itemType:{0};", itemType);
                    return ProductType.Consumable;
                }
            }
        }
        public void Purchase()
        {
            PlayerPrefs.SetInt("MixPurchaseProduct"+Definition.Id,1);
        }
    }
    public class ProductParams
    {
        public string Id;
        public bool HasReceipt = true;
        public string Receipt = "Receipt";
        public string TransactionId = "TransactionId";
        public string CurrencyCode = "USD";
        public string CurrencySymbol = "$";
        public string LocalizedDescription = "LocalizedDescription";
        public decimal LocalizedPrice = 10;
        public string LocalizedPriceString = "LocalizedPriceString";
        public string LocalizedTitle = "LocalizedTitle";
        public int Type = 0;

        public ProductParams(string id, int type, decimal price)
        {
            Id = id;
            Type = type;
            LocalizedPrice = price;
        }
    }
}