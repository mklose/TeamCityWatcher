using System.Linq;
using System.Threading;
using System;

namespace TeamCityWatcher
{
    public class TeamCityWatcher
    {
        public static void Main(string[] args)
        {
            var parser = new ArgumentParser();
            try
            {
                parser.Parse(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return;
            }
            new TeamCityWatcher(parser).Run();
        }

        private readonly ArgumentParser _parser;

        private TeamCityWatcher(ArgumentParser parser)
        {
            _parser = parser;
        }

        private void Run()
        {
            _serialPortConnector = new SerialPortConnector(_parser.ComPort);
            _serialPortConnector.TurnOff();
            var endTime = DateTime.Now.AddMinutes(_parser.RunTimeMinutes);
            while (DateTime.Now.Ticks < endTime.Ticks)
                DoRun();
            _serialPortConnector.TurnOff();
        }

        private void DoRun()
        {
            try
            {
                UpdateLight();
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
            }
            Thread.Sleep(2000);
        }

        private void UpdateLight()
        {
            var hasBrokenBuild = _parser.Servers.Any(srv => srv.HasBrokenBuild());
            var hasRunningBuild = _parser.Servers.Any(srv => srv.HasRunningBuild());
            _serialPortConnector.SetLights(!hasBrokenBuild, hasRunningBuild, hasBrokenBuild);
        }

        private SerialPortConnector _serialPortConnector;
    }
}
