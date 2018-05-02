using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Feeds.EventCounter;
using YamlDotNet.Serialization;
using Feeds.Shared;
using System.Collections.Generic;
using Feeds.Meetup;
using System.Net.Http;
using System;
using System.Configuration;
using System.Linq;

namespace Feeds
{
    public static class EventCountersFunction
    {
        [FunctionName("EventCounters")]
        public static void Run([TimerTrigger("0 12 3 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            const string URL = "https://api.meetup.com";
            var meetupToken = ConfigurationManager.AppSettings["MEETUPTOKEN"];
            var client = PrepareHttpClient(new Uri(URL));
            var allEvents = new List<Common>();

            var events = GET<List<Event>>(client, $"/yorkdevelopers/events?sign=true&key={meetupToken}&fields=group_photo&status=last");
            AddEventsToList(events, allEvents);
            log.Info("Got York Developers events");

            // Count the number of events last year
            var eventsLastYear = allEvents.Where(x => x.Starts.Year >= DateTime.Now.Year - 1).OrderBy(x => x.Starts);
            var counteventsLastYear = eventsLastYear.Count();

            var eventCounterList = new CounterList();
            eventCounterList.Meetups_2018 = 100;
            eventCounterList.Meetups_2017 = counteventsLastYear;
            eventCounterList.Meetups_This_Month = 10;
            eventCounterList.Meetups_This_Week = 3;

            var serializer = new Serializer();
            var yaml = serializer.Serialize(eventCounterList);

            // Push the file to git
            var gitHubClient = new GitHub(log);
            gitHubClient.WriteFileToGitHub("_data/EventCounter.yml", yaml);

        }

        private static HttpClient PrepareHttpClient(Uri endPoint)
        {
            var client = new HttpClient();
            client.BaseAddress = endPoint;
            return client;
        }

        public static T GET<T>(HttpClient client, string apiCall)
        {
            // Proxy the call onto our service.
            var httpResponseMessage = client.GetAsync(apiCall).Result;
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                throw new Exception(string.Format("Failed to GET from {0}.   Status {1}.  Reason {2}. {3}", apiCall, (int)httpResponseMessage.StatusCode, httpResponseMessage.ReasonPhrase, httpResponseMessage.RequestMessage));
            }
            else
            {
                return httpResponseMessage.Content.ReadAsAsync<T>().Result;
            }
        }

        private static void AddEventsToList(List<Event> events, List<Common> allEvents)
        {
            // Location of the Perky Peacock
            const double LAT = 53.960636138916016;
            const double LON = -1.0860970020294189;
            const double LARGEST_DISTANCE = 25;

            var geoData = new GeoData();

            foreach (var evt in events)
            {
                // Is this event near to us?
                var nearUs = false;

                if (evt.venue == null)
                {
                    // We don't have a venue,  so the best we can do is to check to see if the word York is 
                    // in the description,  groupname or URL.
                    nearUs = (evt.name ?? "").ToLower().Contains("york") ||
                             (evt.description ?? "").ToLower().Contains("york") ||
                             (evt.link ?? "").ToLower().Contains("york");
                }
                else
                {
                    // We know the venue for the event,  so we can check that it's within X miles
                    var distance = geoData.distance(LAT, LON, evt.venue?.lat ?? 0.0, evt.venue?.lon ?? 0.0, 'M');
                    nearUs = (distance <= LARGEST_DISTANCE);
                }

                if (nearUs)
                {
                    var common = new Common();
                    common.Name = evt.name;
                    common.Description = evt.description;
                    common.URL = evt.link;

                    // An event is free unless it contains a fee record with an amount which isn't 0
                    common.IsFree = ((evt.fee?.amount ?? 0) == 0);

                    // Events optionally have a group photo
                    common.Logo = evt.group?.photo?.thumb_link;

                    // The start time is held in milliseconds
                    common.Starts = (new DateTime(1970, 1, 1)).AddMilliseconds(double.Parse(evt.time));

                    if (evt.duration == null)
                    {
                        // If no duration is supplied,  then meetup assumes 3 hours
                        common.Ends = common.Starts.AddHours(3);
                    }
                    else
                    {
                        // Duration is also held in milliseconds
                        var duration = TimeSpan.FromMilliseconds(double.Parse(evt.duration));
                        common.Ends = common.Starts + duration;
                    }
                    common.Venue = evt.venue?.name;

                    // Is this one of our meetups?
                    common.Endorsed = (evt.group.name == "York Developers" || evt.group.name == "York Code Dojo");

                    allEvents.Add(common);
                }
            }
        }
    }
}
