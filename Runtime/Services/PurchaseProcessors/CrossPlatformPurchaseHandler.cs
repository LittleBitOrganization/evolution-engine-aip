using System;
using com.adjust.sdk.purchase;
using LittleBit.Modules.IAppModule.Data;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

namespace LittleBit.Modules.IAppModule.Services.PurchaseProcessors
{
    public class CrossPlatformPurchaseHandler : IPurchaseHandler
    {
        private readonly CrossPlatformTangles _crossPlatformTangles;
        
        public CrossPlatformPurchaseHandler(CrossPlatformTangles crossPlatformTangles)
        {
            _crossPlatformTangles = crossPlatformTangles;
        }
        
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args,
            Action<bool> callback)
        {
            bool validPurchase = false;
            
            try
            {
                
                
#if DEBUG_STOREKIT_TEST
                var validator = new CrossPlatformValidator(_crossPlatformTangles.GetGoogleData(),
                    _crossPlatformTangles.GetAppleTestData(), Application.identifier);
                 
#else
                var validator =
                    new CrossPlatformValidator(_crossPlatformTangles.GetGoogleData(),
                        _crossPlatformTangles.GetAppleData(), Application.identifier);
#endif
                
                var purchaseReciepts = validator.Validate(args.purchasedProduct.receipt);

                foreach (var productReceipt in purchaseReciepts)
                {
                    GooglePlayReceipt google = productReceipt as GooglePlayReceipt;
                    if (null != google)
                    {
                        if (string.Equals(args.purchasedProduct.transactionID,google.purchaseToken) &&
                            string.Equals(args.purchasedProduct.definition.storeSpecificId, google.productID))
                        {
                            validPurchase = true;
                        }

                        if ((int) google.purchaseState == 4)
                        {
                            Debug.Log("Deferred IAP, Not bought yet!");
                            return PurchaseProcessingResult.Pending;
                        }
                        
                        Debug.Log(" product transactionID " + args.purchasedProduct.transactionID);
                        Debug.Log(" product definition.id " + args.purchasedProduct.definition.id);
                        Debug.Log(" product definition.storeSpecificId" + args.purchasedProduct.definition.storeSpecificId);
                        Debug.Log(" google productID " + google.productID);
                        Debug.Log(" google transactionID " + google.transactionID);
                        Debug.Log(" google purchaseState " + google.purchaseState);
                        Debug.Log(" google purchaseToken " + google.purchaseToken);
                    }

                    AppleInAppPurchaseReceipt apple = productReceipt as AppleInAppPurchaseReceipt;
                    if (null != apple)
                    {
                        //TODO: Restored purchase can send event to analytics
                        if (args.purchasedProduct.appleProductIsRestored || 
                            (string.Equals(args.purchasedProduct.definition.storeSpecificId, apple.productID) &&
                             string.Equals(args.purchasedProduct.transactionID, apple.transactionID)))
                        {
                            validPurchase = true;
                        }
                        
                        Debug.Log(" validPurchase " + validPurchase);
                        Debug.Log(" product transactionID " + args.purchasedProduct.transactionID);
                        Debug.Log(" product definition.id " + args.purchasedProduct.definition.id);
                        Debug.Log(" product is restored "  + args.purchasedProduct.appleProductIsRestored);
                        Debug.Log(" product definition.storeSpecificId " + args.purchasedProduct.definition.storeSpecificId);
                        Debug.Log(" apple transactionID " + apple.transactionID);
                        Debug.Log(" apple transaction originalTransactionIdentifier " + apple.originalTransactionIdentifier);
                        Debug.Log(" apple transaction subscriptionExpirationDate " + apple.subscriptionExpirationDate);
                        Debug.Log(" apple transaction cancellationDate " + apple.cancellationDate);
                        Debug.Log(" apple transaction quantity "  + apple.quantity);
                        
                    }
                }
            }
            
            catch (Exception e)
            {
                Debug.LogError("Invalid receipt!");

                callback?.Invoke(false);
            }
            
            if(args.purchasedProduct.definition.type == ProductType.NonConsumable)
                callback?.Invoke(validPurchase && PlayerPrefs.GetInt(args.purchasedProduct.definition.id, 0) == 0);
            else
                callback?.Invoke(validPurchase);
            

            PlayerPrefs.SetInt(args.purchasedProduct.definition.id, 1);
            
            return PurchaseProcessingResult.Complete;
        }
    }
}