#load "../Shared/common.csx"
#load "../Shared/gitHub.csx"

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using YamlDotNet.Serialization;
using TweetSharp;
using System.Configuration;
using System.Globalization;

public static void Run(TimerInfo myTimer, TraceWriter log)
{
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
        if (((evt.Starts - DateTime.Now).TotalDays == 7))// && evt.Endorsed)
        {
            log.Info($"Tweeting about {evt.Name}");
            SendTweet(twitterApp, evt.Name + " " + evt.Starts.ToString("dd/M/yyyy") + " " + evt.URL + " #codeyork #techforyork");
        } 
    }
                  
}

public static TwitterStatus SendTweet(TwitterService twitterApp, string tweetText)
{
    var options = new TweetSharp.SendTweetOptions(){ Status = tweetText };
    return twitterApp.SendTweet(options);
}


public static string RemoveSpacesAndTitleCase(string strToConvert)
{
    // Creates a TextInfo based on the "en-US" culture.
    TextInfo myTI = new CultureInfo("en-US", false).TextInfo;

    // Changes a string to titlecase, then replace the spaces with empty
    return myTI.ToTitleCase(strToConvert).Replace(" ", "");
}

