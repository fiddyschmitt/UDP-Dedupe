﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using udp_dedupe.Config;
using udp_dedupe.Network;
using udp_dedupe.Utilities;

namespace udp_dedupe
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var settingsFilename = "";

            if (args.Length == 0)
            {
                var defaultSettingsFilename = "settings.json";
                Console.WriteLine($"Settings filename not provided as arg. Assuming {defaultSettingsFilename}");

                if (!File.Exists(defaultSettingsFilename) || Debugger.IsAttached)
                {
                    Console.WriteLine($"Creating {defaultSettingsFilename} with default values.");

                    var example = new Settings()
                    {
                        Checks = new List<Check>()
                    {
                        new()
                        {
                            TimeWindowInMilliseconds = 5000,
                            Filter = "inbound && !ipv6 && udp && udp.DstPort == 15000",
                            //Filter = "inbound && !ipv6 && udp"
                        }
                    }
                    };

                    File.WriteAllText(defaultSettingsFilename, JsonConvert.SerializeObject(example, Formatting.Indented));
                    settingsFilename = defaultSettingsFilename;
                }
            }
            else
            {
                settingsFilename = args[0];
            }

            if (!File.Exists(settingsFilename))
            {
                Console.WriteLine($"File does not exist: {settingsFilename}");
                return;
            }

            var settingsJson = File.ReadAllText(settingsFilename);
            var settings = JsonConvert.DeserializeObject<Settings>(settingsJson);

            if (settings == null)
            {
                Console.WriteLine($"Settings could not be loaded.");
                return;
            }
            else
            {
                Console.WriteLine($"Read settings from {settingsFilename}");
            }

            var invalidCount = settings
                                .Checks
                                .Select(check =>
                                {
                                    var filterIsValid = WinDivertUtilities.IsFilterValid(check.Filter);
                                    if (!filterIsValid)
                                    {
                                        Console.WriteLine($"Invalid filter: {check.Filter}");
                                    }

                                    return filterIsValid;
                                })
                                .Where(valid => !valid)
                                .Count();

            if (invalidCount > 0)
            {
                return;
            }

            var checkers = settings
                            .Checks
                            .Select(check => new Checker(check))
                            .ToList();

            checkers
                .ForEach(checker =>
                {
                    Task.Factory.StartNew(checker.Start);
                    Console.WriteLine($"Started checker for filter: {checker.Check.Filter}");
                });

            Console.WriteLine($"Running.");

            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
