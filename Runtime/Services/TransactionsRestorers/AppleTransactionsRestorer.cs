using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace LittleBit.Modules.IAppModule.Services.TransactionsRestorers
{
    public class AppleTransactionsRestorer : ITransactionsRestorer
    {
        public void Restore(IExtensionProvider extensionProvider, Action<bool, string> callback)
        {
            var appleExtensions = extensionProvider.GetExtension<IAppleExtensions>();
            
            appleExtensions.RestoreTransactions((success, message) =>
            {
                callback?.Invoke(success, message);
            });
        }
    }
}