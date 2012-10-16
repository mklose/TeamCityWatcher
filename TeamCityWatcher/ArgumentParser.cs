﻿using System;
using System.Collections.Generic;
using net.sf.dotnetcli;

namespace TeamCityWatcher
{
    class ArgumentParser
    {
        private readonly Options _options;
        private readonly Option _serverOption;
        private readonly Option _helpOption;
        private const string RunTime = "runTime";
        private const string Port = "port";
        private const string Help = "help";
        private const string Server = "server";

        public ArgumentParser()
        {
            _options = new Options();
            _options.AddOption(OptionBuilder.Factory
                                  .IsRequired()
                                  .HasArg()
                                  .WithArgName(RunTime)
                                  .WithDescription("Setzt die Laufzeit in Minuten")
                                  .Create(RunTime));
            _options.AddOption(OptionBuilder.Factory
                                  .IsRequired()
                                  .HasArg()
                                  .WithArgName(Port)
                                  .WithDescription("Setzt den COM-Port")
                                  .Create(Port));
            _helpOption = OptionBuilder.Factory.WithDescription("Zeigt die Hilfe zu den Befehlen an.").Create(Help);
            _options.AddOption(_helpOption);
            _serverOption = OptionBuilder.Factory.WithArgName("url,user,password")
                .HasArgs(3).IsRequired().WithValueSeparator(Convert.ToChar(","))
                .WithDescription("Referenz zum TeamCity server").Create(Server);
            _options.AddOption(_serverOption);
        }

        public void Parse(string[] args)
        {
            ParseHelp(args);
            Servers = new List<TeamCityServer>();
            var commandLine = new GnuParser().Parse(_options, args);
            ComPort = commandLine.GetOptionValue(Port);
            Console.WriteLine("Using port: " + ComPort);
            RunTimeMinutes = int.Parse(commandLine.GetOptionValue(RunTime));
            Console.WriteLine(String.Format("Running for {0} minutes", RunTimeMinutes));
            ParseServers(args);
        }

        private void ParseHelp(string[] args)
        {
            var options = new Options();
            options.AddOption(_helpOption);
            var commandLine = new GnuParser().Parse(options, args);
            if (commandLine.HasOption(Help) || args.Length == 0)
            {
                ShowHelp();
            }
        }

        private void ParseServers(string[] args)
        {
            var parser = new GnuParser();
            var serverOptions = new Options();
            serverOptions.AddOption(_serverOption);
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] != "-" + Server || i + 1 >= args.Length) continue;
                var currentLine = parser.Parse(serverOptions, new[] {args[i], args[i + 1]});
                AddServer(currentLine.GetOptionValues(Server));
            }
        }

        private void AddServer(string[] values)
        {
            Console.WriteLine(String.Format("Adding server '{0}' user: '{1}'", values[0], values[1]));
            Servers.Add(new TeamCityServer(values[0], values[1], values[2]));
        }

        public int RunTimeMinutes { get; private set; }
        public string ComPort { get; private set; }
        public List<TeamCityServer> Servers { get; private set; }

        private void ShowHelp()
        {
            var helpFormatter = new HelpFormatter();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Several servers can be passed.");
            sb.AppendLine("Example:");
            sb.AppendLine("Running for one hour (60 minutes) and using COM-Port 'COM3'");
            sb.AppendLine("The used server is 192.168.1.12:8080, user: 'admin' and password: 'adminadmin':");
            sb.AppendLine(String.Format("TeamCityWatcher -{0} 60 -{1} COM3", RunTime, Port));
            sb.AppendLine(String.Format("-{0} 192.168.1.12:8080,admin,adminadmin", Server)); 
            helpFormatter.PrintHelp("TeamCityWatcher", "Parameters for the TeamCityWatcher", _options, sb.ToString());
            Environment.Exit(1);
        }
    }
}
