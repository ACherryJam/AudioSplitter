using System;
using System.Linq;

namespace Celeste.Mod.AudioSplitter.Utility
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class ModuleEventAttribute : Attribute
    {
        public void Call()
        {
            var classes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass);

            foreach (var type in classes)
            {
                var methods = classes
                    .SelectMany(x => x.GetMethods())
                    .Where(x => x.GetCustomAttributes(this.GetType(), false).Any());

                foreach (var method in methods)
                {
                    if (method.IsStatic)
                    {
                        method.Invoke(null, null);
                        continue;
                    }
                }
            }
        }
    }
}
