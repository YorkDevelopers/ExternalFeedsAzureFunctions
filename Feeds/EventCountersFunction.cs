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
        private static DateTime ToDatetime(Event evt)
        => (new DateTime(1970, 1, 1)).AddMilliseconds(double.Parse(evt.time));


        [FunctionName("EventCounters")]
        public static void Run([TimerTrigger("0 12 3 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            const string URL = "https://api.meetup.com";
            log.Info("Line 1");
            var meetupToken = ConfigurationManager.AppSettings["MEETUPTOKEN"];
            log.Info("Line 2");
            var client = PrepareHttpClient(new Uri(URL));
            log.Info("Line 3");
            var allEvents = new List<Common>();
            log.Info("Line 4");

            var events = GET<List<Event>>(client, $"/yorkdevelopers/events?sign=true&key={meetupToken}&status=past");
            log.Info("Got York Developers events");

            // Count the number of events last year
            var countEventsLastYear = events.Count(x =>  ToDatetime(x).Year >= DateTime.Now.Year - 1);
            var countEventsThisYear = events.Count(x => ToDatetime(x).Year == DateTime.Now.Year);
            var countEventsThisMonth = events.Count(x => ToDatetime(x).Month == DateTime.Now.Month);
            var countEventsThisWeek = events.Count(x => ToDatetime(x) >= DateTime.Now.AddDays(-7));

            log.Info("countEventsLastYear: " + countEventsLastYear);
            log.Info("countEventsThisYear: " + countEventsThisYear);
            log.Info("countEventsThisMonth: " + countEventsThisMonth);
            log.Info("countEventsThisWeek: " + countEventsThisWeek);

            foreach (var evt in events)
            {
                log.Info(ToDatetime(evt) + " | " + evt.description);
            }

            var eventCounterList = new CounterList();
            eventCounterList.Meetups_2018 = countEventsThisYear;
            eventCounterList.Meetups_2017 = countEventsLastYear;
            eventCounterList.Meetups_This_Month = countEventsThisMonth;
            eventCounterList.Meetups_This_Week = countEventsThisWeek + 1; // Added so that it displays a value

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
