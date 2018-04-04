using System;
using System.Collections.Generic;
using System.Linq;
using Feeds.Shared;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using YamlDotNet.Serialization;

namespace Feeds
{
    public static class MergeFeedsFunction
    {
        [FunctionName("MergeFeeds")]
        public static void Run([TimerTrigger("0 15 3 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            var files = new List<string>() { "_data/EventBrite.yml", "_data/Meetup.yml", "_data/YPS.yml", "_data/Twitter.yml", "_data/TicketLeap.yml" };

            const string TARGETFILENAME = "_data/Events.yml";

            var deserializer = new Deserializer();
            var gitHub = new GitHub(log);

            var allEvents = new List<Common>();
            foreach (var file in files)
            {
                var yaml = gitHub.ReadFileFromGitHub(file);
                var extraEvents = deserializer.Deserialize<List<Common>>(yaml);
                allEvents.AddRange(extraEvents);
            }

            var serializer = new Serializer();
            var yamlAll = serializer.Serialize(allEvents.Where(evt => evt.Starts > System.DateTime.Now));

            gitHub.WriteFileToGitHub(TARGETFILENAME, yamlAll);
        }
    }
}
