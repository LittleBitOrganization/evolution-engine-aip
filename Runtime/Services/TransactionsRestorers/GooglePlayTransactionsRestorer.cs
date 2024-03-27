using System;
using UnityEngine.Purchasing;

namespace LittleBit.Modules.IAppModule.Services.TransactionsRestorers
{
    public class GooglePlayTransactionsRestorer : ITransactionsRestorer
    {
        public void Restore(IExtensionProvider extensionProvider, Action<bool, string> callback)
        {
            extensionProvider.GetExtension<IGooglePlayStoreExtensions>().RestoreTransactions((success, message) =>
            {
                callback?.Invoke(success, message);
            });
        }
    }
}