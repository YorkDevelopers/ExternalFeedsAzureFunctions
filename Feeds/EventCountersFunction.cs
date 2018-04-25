using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Feeds.EventCounter;
using YamlDotNet.Serialization;
using Feeds.Shared;

namespace Feeds
{
    class EventCountersFunction
    {
        [FunctionName("EventCounters")]
        public static void Run([TimerTrigger("0 12 3 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            var eventCounterList = new CounterList();
            eventCounterList.Meetups_2018 = 100;
            eventCounterList.Meetups_2017 = 33;
            eventCounterList.Meetups_This_Month = 10;
            eventCounterList.Meetups_This_Week = 3;

            var serializer = new Serializer();
            var yaml = serializer.Serialize(eventCounterList);

            // Push the file to git
            var gitHubClient = new GitHub(log);
            gitHubClient.WriteFileToGitHub("_data/EventCounter.yml", yaml);

        }
    }
}
