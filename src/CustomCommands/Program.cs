using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.Runtime;
using System.Reflection;

namespace CustomCommands
{
    public class Program
    {
        readonly IApplicationEnvironment _appEnv;

        public Program(IApplicationEnvironment appEnv)
        {
            _appEnv = appEnv;
        }

        public void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("You must specify the command to run as the first command line argument");
                return;
            }

            var methodName = args[0];

            var assemblyName = new AssemblyName(_appEnv.ApplicationName);
            var assembly = Assembly.Load(assemblyName);

            var customCommandsTypeInfo = assembly.DefinedTypes.FirstOrDefault(typeinfo => typeinfo.Name == "CustomCommands");
            if (customCommandsTypeInfo == null)
            {
                Console.WriteLine($"Couldn't locate a type named CustomCommands in {_appEnv.ApplicationName}");
                return;
            }

            var method = customCommandsTypeInfo.AsType().GetMethod(methodName);
            if (method == null)
            {
                Console.WriteLine($"Couldn't locate a method named {methodName} in {customCommandsTypeInfo.Namespace}.{customCommandsTypeInfo.Name}");
                return;
            }

            Console.WriteLine($"Invoking {customCommandsTypeInfo.Namespace}.{customCommandsTypeInfo.Name}.{methodName}");
            method.Invoke(null, null);
        }
    }
}
