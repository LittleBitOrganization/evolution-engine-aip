using System;
using System.Collections.Generic;
using LittleBit.Modules.IAppModule.Data;
using Purchase;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.MiniJSON;
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
            Action<bool, RecieptHandler> callback)
        {
            bool validPurchase = false;
            Dictionary<string, object> wrapper = null;
            
            TextAsset jsonFile = Resources.Load<TextAsset>("BillingMode");

            if (jsonFile != null)
            {
                BillingModeData billingModeData = JsonUtility.FromJson<BillingModeData>(jsonFile.text);

                Debug.LogError("Store is " + billingModeData.androidStore);
                
                wrapper = Json.Deserialize(args.purchasedProduct.receipt) as Dictionary<string, object>;
                
                if (billingModeData.androidStore.Equals("AmazonAppStore"))
                {
                    validPurchase = true;
                }
                else
                {
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
                                //
                                // Debug.Log(" product transactionID " + args.purchasedProduct.transactionID);
                                // Debug.Log(" product definition.id " + args.purchasedProduct.definition.id);
                                // Debug.Log(" product definition.storeSpecificId" + args.purchasedProduct.definition.storeSpecificId);
                                // Debug.Log(" google productID " + google.productID);
                                // Debug.Log(" google transactionID " + google.transactionID);
                                // Debug.Log(" google purchaseState " + google.purchaseState);
                                // Debug.Log(" google purchaseToken " + google.purchaseToken);
                            }

                            AppleInAppPurchaseReceipt apple = productReceipt as AppleInAppPurchaseReceipt;
                            if (null != apple)
                            {
                                if (args.purchasedProduct.appleProductIsRestored || 
                                    (string.Equals(args.purchasedProduct.definition.storeSpecificId, apple.productID) &&
                                     string.Equals(args.purchasedProduct.transactionID, apple.transactionID)))
                                {
                                    validPurchase = true;
                                }
                                
                                // Debug.Log(" validPurchase " + validPurchase);
                                // Debug.Log(" product transactionID " + args.purchasedProduct.transactionID);
                                // Debug.Log(" product definition.id " + args.purchasedProduct.definition.id);
                                // Debug.Log(" product is restored "  + args.purchasedProduct.appleProductIsRestored);
                                // Debug.Log(" product definition.storeSpecificId " + args.purchasedProduct.definition.storeSpecificId);
                                // Debug.Log(" apple transactionID " + apple.transactionID);
                                // Debug.Log(" apple transaction originalTransactionIdentifier " + apple.originalTransactionIdentifier);
                                // Debug.Log(" apple transaction subscriptionExpirationDate " + apple.subscriptionExpirationDate);
                                // Debug.Log(" apple transaction cancellationDate " + apple.cancellationDate);
                                // Debug.Log(" apple transaction quantity "  + apple.quantity);
                            }
                        }
                    }
            
                    catch (Exception e)
                    {
                        Debug.LogError("Invalid receipt!");

                        callback?.Invoke(false, null);
                    }
                }
            }
            else
            {
                Debug.LogError("Failed to load JSON file from resources.");
            }
            
            
            
            if(args.purchasedProduct.definition.type == ProductType.NonConsumable)
                callback?.Invoke(validPurchase && PlayerPrefs.GetInt(args.purchasedProduct.definition.id, 0) == 0, new RecieptHandler(wrapper));
            else
                callback?.Invoke(validPurchase, new RecieptHandler(wrapper));
            
            PlayerPrefs.SetInt(args.purchasedProduct.definition.id, 1);
            
            return PurchaseProcessingResult.Complete;
        }
    }
}