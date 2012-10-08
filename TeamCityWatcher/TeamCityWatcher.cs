using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using System.Net;
using System.IO;
using System.IO.Ports;
using System;

namespace TeamCityWatcher
{
    class TeamCityWatcher
    {

        private static TeamCityWatcher _teamCityWatcher;

		// C:\Tools\TeamCityWatcher.exe runMin=720 COM3
        static void Main(string[] args)
        {
            _teamCityWatcher = new TeamCityWatcher();
            _teamCityWatcher.Run(Convert.ToInt32(args[0].Split('=')[1]), args[1]);
        }

        /* *
           * TODO:
           * via parameter:
           * -urls with user:password
           * -error if tc is not running
           * -write to event log 
           * */
        private const int Off = 0;
        private const int OneOn = 128; //switch no 8 =2^7
        private const int TwoOn = 64; //switch no 7 =2^6
        private const int ThreeOne = 32; //switch no 6 =2^5
        private bool _buildrunning, _brokenbuild;
        private string _serialPortName = "NOT SET";


        public void Run(int runMinutes, string serialPortName)
        {
            Console.WriteLine("run {0} minutes ", runMinutes);
            _serialPortName = serialPortName;
            Console.WriteLine("Using serial port: " + _serialPortName);
            var endTime = DateTime.Now.AddMinutes(runMinutes);
            while (DateTime.Now.Ticks < endTime.Ticks)
            {
                _buildrunning = false;
                _brokenbuild = false;

                try
                {
                    var lights = Off;
                    GetData("192.168.100.13");
                    GetData("192.168.100.4:8080");
                    if (_brokenbuild)
                    {
                        lights += ThreeOne;
                    }
                    else
                    {
                        lights += OneOn;
                    }
                    if (_buildrunning) lights += TwoOn;
                    SetSwitch(lights);

                }
                catch (Exception e)
                {
                    Console.WriteLine("{0} Exception caught.", e);
                }
                Thread.Sleep(2000);
            }

            Console.WriteLine("turning off");

            SetSwitch(Off);
            Thread.Sleep(2000);
            SetSwitch(Off);
            Thread.Sleep(2000);
            SetSwitch(Off);
            Thread.Sleep(2000);
        }

        private void GetData(String server)
        {
            if (HasRunningBuild(server))
            {
                _buildrunning = true;
                Console.WriteLine("build running");
            }
         
            if (HasBrokenBuild(server))
            {
                _brokenbuild = true;
                Console.WriteLine("broken build");
            }

        }

        private bool HasBrokenBuild(String server)
        {

            XElement rss = GetResponse("http://" + server + "/httpAuth/app/rest/builds");
            var currentBuildJobs = from e in rss.Elements()
                          where e.Name.LocalName == "build"
                          select new
                          {
                              Id = int.Parse(e.Attribute("id").Value),
                              status = e.Attribute("status").Value,
                              buildTypeId = e.Attribute("buildTypeId").Value
                          };

            var succesfull = new List<String>();
            foreach (var buildJob in currentBuildJobs.Where(buildJob => !succesfull.Contains(buildJob.buildTypeId)))
            {
                if (!buildJob.status.Equals("SUCCESS"))
                {
                    return true;
                }
                succesfull.Add(buildJob.buildTypeId);
            }

            return false;
        }


        private bool HasRunningBuild(String server)
        {
            return int.Parse(GetResponse("http://" + server + "/httpAuth/app/rest/builds/?locator=running:true").Attribute("count").Value) > 0;
        }

        protected XElement GetResponse(string url)
        {
            var request = WebRequest.Create(url) as HttpWebRequest;
            request.Credentials = new NetworkCredential("admin", "adminadmin");
            //  request.UserAgent = ".NET Sample";
            //  request.KeepAlive = false;
            //  request.Timeout = 15 * 1000;

            var response = request.GetResponse() as HttpWebResponse;
            if (request.HaveResponse && response != null)
            {
                var reader = new StreamReader(response.GetResponseStream());
                return XElement.Parse(reader.ReadToEnd());
            }
            throw new Exception("Error fetching data from: " + url);
        }

        private void SetSwitch(int i)
        {
            try
            {
                var serialPort = new SerialPort
                                     {
                                         PortName = _serialPortName,
                                         BaudRate = 19200,
                                         Parity = Parity.None,
                                         DataBits = 8,
                                         StopBits = StopBits.One,
                                         Handshake = Handshake.None
                                     };
                serialPort.Open();

                var data = new byte[4];
                data[0] = 3; //set command 
                data[1] = 0; //broadcast to all cards
                data[2] = (byte)i; //set switches 
                data[3] = (byte)(data[0] ^ data[1] ^ data[2]); //parity 
                serialPort.Write(data, 0, 4);

                serialPort.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("serial port error on port : " + _serialPortName, e);
            }
        }
    }
}
