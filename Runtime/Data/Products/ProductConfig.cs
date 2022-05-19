﻿using LittleBit.Modules.IAppModule.Commands.Factory;
using UnityEngine;
using UnityEngine.Purchasing;

namespace LittleBit.Modules.IAppModule.Data.Products
{
    public abstract class ProductConfig : ScriptableObject
    {
        [SerializeField] private ProductType productType = ProductType.Consumable;

        [SerializeField] private string id;
        
        public string Id { get; protected set; }
        public ProductType ProductType => productType;

        public abstract void HandlePurchase(PurchaseCommandFactory purchaseCommandFactory);
    }
}