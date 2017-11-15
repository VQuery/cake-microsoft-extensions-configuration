using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cake.Microsoft.Extensions.Configuration
{
    public static class CommandLineHelper
    {
        public static ICollection<string> KnownCakeCommandLineArguments = new[]
        {
            "verbosity", "v",
            "showdescription", "s",
            "dryrun",
            "noop",
            "whatif",
            "help", "?",
            "version", "ver",
            "debug", "d",
            "mono",
            "nuget_useinprocessclient"
        };

        public static IDictionary<string, string> KnownCakeCommandLineShortNameArguments = new Dictionary<string, string>
        {
            ["v"] = "verbosity",
            ["s"] = "showdescription",
            ["?"] = "help",
            ["ver"] = "version",
            ["d"] = "debug"
        };

        public static (ICollection<string> CakeArgs, ICollection<string> ScriptArgs, ICollection<string> InvalidArgs) GetCommandLineArgs()
        {
            var args = Environment.GetCommandLineArgs().Skip(1).ToList();

            ICollection<string> knownArgs = new List<string>();
            ICollection<string> scriptArgs = new List<string>();
            ICollection<string> invalidArgs = new List<string>();

            for (var i = 0; i < args.Count; i++)
            {
                string argName;
                var arg = args[i];

                if (0 == i && arg.EndsWith(".cake") || arg.EndsWith(".csx"))
                {
                    // build script so ignore
                    continue;
                }

                // break argument up into its parts so we can process it
                var argParts = Regex.Match(arg, @"^(?<prefix>-|--|/)?(?<argName>\w+)(?:=(?<value>\w+))?");

                if (!argParts.Success)
                {
                    // not valid command line argument format for Microsoft.Extensions.Configuration.CommandLine
                    // just ignore, don't break
                    invalidArgs.Add(arg);
                    continue;
                }

                // normalise short names to long names
                (argName, arg) = NormaliseArgument(argParts);

                // we need some info about the current argument so we know what to do next
                var (isSplit, isSwitch) = GetArgumentInfo(argParts, args?[i + 1]);                

                var list = KnownCakeCommandLineArguments.Contains(argName) ? knownArgs : scriptArgs;

                list.Add(arg);

                if (isSplit)
                {
                    i++;
                    list.Add(args[i]);
                }
                else if (isSwitch)
                {
                    list.Add($"{true}");
                }
            }

            return (knownArgs, scriptArgs, invalidArgs);
        }

        private static (string name, string argument) NormaliseArgument(Match argParts)
        {
            var argName = argParts.Groups["argName"].Value;

            if (!KnownCakeCommandLineShortNameArguments.ContainsKey(argName))
            {
                return (argName, argParts.Value);
            }

            var prefix = argParts.Groups["prefix"].Value;
            var newArgName = KnownCakeCommandLineShortNameArguments[argName];
            var arg = argParts.Value.Replace($"{prefix}{argName}", $"{prefix}{newArgName}");

            return (newArgName, arg);
        }

        public static (bool IsSplitArgument, bool IsSwitch) GetArgumentInfo(Match argParts, string nextArgument)
        {
            bool hasValue = argParts.Groups["value"].Success ? argParts.Groups["value"].Value != null : false;

            if (hasValue)
            {
                // has a value so can't be the others
                return (IsSplitArgument: false, IsSwitch: false);
            }
            
            if (nextArgument == null)
            {
                // this is the last argument, so must be a switch
                return (IsSplitArgument: false, IsSwitch: true);
            }
             
            // let's figure out if we have a value or argument next
            var nextArgParts = Regex.Match(nextArgument, @"^(?<prefix>-|--|/)?(?<argName>\w+)(?:=(?<value>\w+))?");

            if (!nextArgParts.Success || nextArgParts.Groups["value"].Success)
            {
                // next is an argument so current must be a switch
                return (IsSplitArgument: false, IsSwitch: true);
            }

            // next is not an argument, so current must be a split argument
            return (IsSplitArgument: true, IsSwitch: false);
        }

        private static IEnumerable<string> GetCustomArguments(IEnumerable<string> args)
        {
            var cakeArgs = new[]
            {
                "verbosity", "v",
                "showdescription", "s",
                "dryrun",
                "noop",
                "whatif",
                "help", "?",
                "version", "ver",
                "debug", "d",
                "mono"
            };

            var customArgs = args
                                // skip until first commandline argument found
                                .SkipWhile(arg => !arg.StartsWith("--") && !arg.StartsWith("-") && !arg.StartsWith("/"))
                                // get only args that are not know cake commandline arguments
                                .Where(arg => !Regex.IsMatch(arg, $"^(--|-|/){string.Join("|", cakeArgs)}"));

            return customArgs;

        }
    }
}
