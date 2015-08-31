using System;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;

namespace CustomCommands
{
    public class Program
    {
        readonly IApplicationEnvironment _appEnv;
        readonly IServiceProvider _serviceProvider;

        public Program(IApplicationEnvironment appEnv, IServiceProvider serviceProvider)
        {
            _appEnv = appEnv;
            _serviceProvider = serviceProvider;
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

            var services = new ServiceCollection();
            services.AddInstance<IApplicationEnvironment>(_appEnv);

            // TODO: use reflection to determine if we should provide pass the services parameter
            var instance = ActivatorUtilities.CreateInstance(_serviceProvider, customCommandsTypeInfo.AsType(), services);

            var method = customCommandsTypeInfo.AsType().GetMethod(methodName);
            if (method == null)
            {
                Console.WriteLine($"Couldn't locate a method named {methodName} in {customCommandsTypeInfo.Namespace}.{customCommandsTypeInfo.Name}");
                return;
            }

            var serviceProvider = services.BuildServiceProvider();

            // TODO: add support for async methods - get the Task back from the method and call .Wait()
            // TODO: add support for paassing unused command line args as parameters to method (require that method parameters must be strings)
            // TODO: use reflection to determine what the parameters to pass
            Console.WriteLine($"Invoking {customCommandsTypeInfo.Namespace}.{customCommandsTypeInfo.Name}.{methodName}");
            method.Invoke(instance, new object[] { serviceProvider });
        }
    }
}
