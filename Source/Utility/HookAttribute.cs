using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.AudioSplitter.Module;

namespace Celeste.Mod.AudioSplitter.Utility
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HookAttribute : Attribute
    {
        public static void Invoke(Type attribute)
        {
            var methods = typeof(HookAttribute).Assembly
                .GetTypes()
                .Where(t => t.IsClass)
                .SelectMany(c => c.GetMethods())
                .Where(m => m.GetCustomAttributes(attribute).Any());

            // Warn about instance methods that has an attribute
            var instanceMethods = methods.Where(m => !m.IsStatic);
            if (instanceMethods.Any())
            {
                foreach (var method in instanceMethods)
                {
                    Logger.Warn(nameof(AudioSplitterModule), 
                                $"Hook {method.GetType().Name}.{method.Name} of attribute {attribute.Name} is non-static and won't be applied! Fix ASAP!");
                }
            }

            var staticMethods = methods.Where(m => m.IsStatic);
            foreach (var method in staticMethods)
            {
                method.Invoke(null, null);
            }
        }
    }

    public class ApplyOnLoadAttribute : HookAttribute { }
    public class RemoveOnUnloadAttribute : HookAttribute { }
}
