using System;
using UnityEngine.Purchasing;

namespace LittleBit.Modules.IAppModule.Services.TransactionsRestorers
{
    public interface ITransactionsRestorer
    {
        public void Restore(IExtensionProvider extensionProvider, Action<bool, string> callback);
    }
}