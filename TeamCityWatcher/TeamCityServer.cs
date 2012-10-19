using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;

namespace TeamCityWatcher
{
    class TeamCityServer
    {
        private readonly string _url;
        private readonly string _httpUser;
        private readonly string _httpPassword;

        public TeamCityServer(string url, string httpUser, string httpPassword)
        {
            _url = url;
            _httpUser = httpUser;
            _httpPassword = httpPassword;
        }

        public bool HasBrokenBuild()
        {
            var rss = GetResponse("http://" + _url + "/httpAuth/app/rest/builds");
            if (rss == null) return true;
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
                    return true;
                succesfull.Add(buildJob.buildTypeId);
            }
            return false;
        }


        public bool HasRunningBuild()
        {
            var xAttribute = GetResponse("http://" + _url + "/httpAuth/app/rest/builds/?locator=running:true").Attribute("count");
            return xAttribute != null && int.Parse(xAttribute.Value) > 0;
        }

        protected XElement GetResponse(string url)
        {
            try
            {
                var request = WebRequest.Create(url) as HttpWebRequest;
                if (request != null)
                {
                    request.Credentials = new NetworkCredential(_httpUser, _httpPassword);

                    var response = request.GetResponse() as HttpWebResponse;
                    if (request.HaveResponse && response != null)
                    {
                        var reader = new StreamReader(response.GetResponseStream());
                        return XElement.Parse(reader.ReadToEnd());
                    }
                }   
            } catch(Exception)
            {
                Console.Error.WriteLine("Error fetching data from: " + url);   
            }
            return null;
        }
    }
}
