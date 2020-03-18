using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using Feeds.Shared;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using TweetSharp;
using YamlDotNet.Serialization;

namespace Feeds
{
    public static class TwitterIntegrationFunction
    {
        [FunctionName("TwitterIntegration")]
        public static void Run([TimerTrigger("0 0 4 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            return;

            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            var consumerKey = ConfigurationManager.AppSettings["TWITTER_YORKDEVELOPERS_CONSUMER_KEY"];
            var consumerSecret = ConfigurationManager.AppSettings["TWITTER_YORKDEVELOPERS_CONSUMER_SECRET"];
            var consumerAccessToken = ConfigurationManager.AppSettings["TWITTER_YORKDEVELOPERS_CONSUMER_ACCESS_TOKEN"];
            var consumerAccessTokenSecret = ConfigurationManager.AppSettings["TWITTER_YORKDEVELOPERS_CONSUMER_ACCESS_TOKEN_SECRET"];

            const string SOURCEFILENAME = "_data/Events.yml";

            var gitHub = new GitHub(log);
            var deserializer = new Deserializer();

            var yaml = gitHub.ReadFileFromGitHub(SOURCEFILENAME);
            var Events = deserializer.Deserialize<List<Common>>(yaml);

            var twitterApp = new TwitterService(consumerKey, consumerSecret);
            twitterApp.AuthenticateWith(consumerAccessToken, consumerAccessTokenSecret);

            foreach (var evt in Events)
            {
                var daysUntilEvent = (evt.Starts - DateTime.Now).Days;
                if (evt.Endorsed)
                {
                    log.Info($"{daysUntilEvent} days until {evt.Name}");
                }
                if ((daysUntilEvent == 6) && evt.Endorsed)
                {
                    log.Info($"Tweeting about {evt.Name}");
                    var tweetStr = evt.Name + " - " + 
                                    evt.Starts.Day + GetDateSuffix(evt.Starts.Day) + " " + 
                                    evt.Starts.ToString("MMM yyyy") + " " 
                                    + evt.URL + 
                                    " #YorkDevelopers #CodeYork #TechForYork";
                    SendTweet(twitterApp, tweetStr);
                    log.Info(tweetStr);
                }
            }
        }

        public static string GetDateSuffix(int dayPartofDate)
        {
            var dateSuffix = "";

            switch (dayPartofDate)
            {
                case 1:
                case 21:
                case 31:
                    dateSuffix = "st";
                    break;
                case 2:
                case 22:
                    dateSuffix = "nd";
                    break;
                case 3:
                case 23:
                    dateSuffix = "rd";
                    break;
                default:
                    dateSuffix = "th";
                    break;
            }

            return dateSuffix;
        }

        public static TwitterStatus SendTweet(TwitterService twitterApp, string tweetText)
        {
            var options = new TweetSharp.SendTweetOptions() { Status = tweetText };
            return twitterApp.SendTweet(options);
        }


        public static string RemoveSpacesAndTitleCase(string strToConvert)
        {
            // Creates a TextInfo based on the "en-US" culture.
            var myTI = new CultureInfo("en-US", false).TextInfo;

            // Changes a string to titlecase, then replace the spaces with empty
            return myTI.ToTitleCase(strToConvert).Replace(" ", "");
        }


    }
}
