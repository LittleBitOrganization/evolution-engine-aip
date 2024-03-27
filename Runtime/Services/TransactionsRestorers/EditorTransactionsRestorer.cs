using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace LittleBit.Modules.IAppModule.Services.TransactionsRestorers
{
    public class EditorTransactionsRestorer : ITransactionsRestorer
    {
        public void Restore(IExtensionProvider extensionProvider, Action<bool, string> callback)
        {
            Debug.LogError("Can't restore purchases on editor platform:(");
            callback?.Invoke(true, String.Empty);
        }
    }
}