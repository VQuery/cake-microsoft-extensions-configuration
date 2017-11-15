using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Core;
using Cake.Core.Annotations;
using Microsoft.Extensions.Configuration;

namespace Cake.Microsoft.Extensions.Configuration
{
    [CakeNamespaceImport("Microsoft.Extensions.Configuration")]
    public static class ConfigurationAlias
    {
        private static IConfiguration _configuration;

        [CakeMethodAlias]
        public static void LoadConfiguration(this ICakeContext context, Action<IConfigurationBuilder, string[]> initialiseAction)
        {
            var builder = new ConfigurationBuilder();

            var (cakeArgs, scriptArgs, invalidArgs) = CommandLineHelper.GetCommandLineArgs();

            foreach(var arg in invalidArgs)
            {
                Console.WriteLine($"{arg} is not in the correct format for Microsoft.Extensions.Configuration.CommandLine and has been ignored.");
            }

            initialiseAction(builder, scriptArgs.ToArray());
            
            _configuration = builder.Build();
        }

        [CakeMethodAlias]
        public static T GetConfiguration<T>(this ICakeContext context) where T : new()
        {
            return context.GetConfiguration<T>(localConfiguration: null, commandLineSwitchMappings: null);
        }

        [CakeMethodAlias]
        public static T GetConfiguration<T>(this ICakeContext context, IDictionary<string, string> localConfiguration = null, IDictionary<string, string> commandLineSwitchMappings = null) where T : new()
        {
            var configuration = default(T);
            context.GetConfiguration(configuration, localConfiguration, commandLineSwitchMappings);
            return configuration;
        }

        [CakeMethodAlias]
        public static void GetConfiguration<T>(this ICakeContext context, T instance, IDictionary<string, string> localConfiguration = null, IDictionary<string, string> commandLineSwitchMappings = null)
        {
            var mappings = commandLineSwitchMappings?.Concat(CommandLineHelper.KnownCakeCommandLineShortNameArguments.Select(kvp => new KeyValuePair<string, string>($"-{kvp.Key}", $"--{kvp.Value}")));
            context.GetConfiguration(instance, DefaultLoadConfiguration(localConfiguration, (IDictionary<string, string>)mappings));
        }

        [CakeMethodAlias]
        public static T GetConfiguration<T>(this ICakeContext context, Action<IConfigurationBuilder, string[]> initialiseAction) where T : new()
        {
            var configuration = default(T);
            context.GetConfiguration(configuration, initialiseAction);
            return configuration;
        }

        [CakeMethodAlias]
        public static void GetConfiguration<T>(this ICakeContext context, T instance, Action<IConfigurationBuilder, string[]> initialiseAction)
        {
            context.LoadConfiguration(initialiseAction);
            context.BindConfiguration(instance);
        }

        [CakeMethodAlias]
        public static T BindConfiguration<T>(this ICakeContext context) where T : new()
        {
            var instance = new T();
            context.BindConfiguration(instance);
            return instance;
        }

        [CakeMethodAlias]
        public static void BindConfiguration(this ICakeContext context, object instance)
        {
            _configuration.Bind(instance);
        }

        [CakePropertyAlias]
        public static IConfiguration ScriptConfiguration(this ICakeContext context)
        {
            return _configuration;
        }

        private static Action<IConfigurationBuilder, string[]> DefaultLoadConfiguration(IDictionary<string, string> localConfiguration = null, IDictionary<string, string> commandLineSwitchMappings = null)
        {
            return (builder, args) =>
            {
                builder
                    .AddInMemoryCollection(localConfiguration)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args, commandLineSwitchMappings)
                ;
            };
        }
    }
}
