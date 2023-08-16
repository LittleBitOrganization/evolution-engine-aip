using System;
using UnityEngine.Scripting;

namespace LittleBit.Modules.IAppModule.Commands.Factory
{
    public class PurchaseCommandFactory
    {
        private ICreator _creator;

        [Preserve]
        public PurchaseCommandFactory(ICreator creator)
        {
            _creator = creator;
            
        }

        public T Create<T>(object[] args = null) where T : IPurchaseCommand
        {
            var @params = args ?? Array.Empty<object>();

            return _creator.Instantiate<T>(@params);
        }
    }
}