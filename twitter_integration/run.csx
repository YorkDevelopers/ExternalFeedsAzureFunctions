#load "../Shared/common.csx"
#load "../Shared/gitHub.csx"

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using YamlDotNet.Serialization;
using TweetSharp;

public static void Run(TimerInfo myTimer, TraceWriter log)
{
    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");  

    // const string TWITTER_CONSUMER_KEY = "NvmGrs1vsVZXGMwmCoV0U4rzI";
    // const string TWITTER_CONSUMER_KEY_SECRET = "wn5IHJyF0DnrQE0QviJksYzQiYajnb6gBmFCC1HmPL3H4EBUlO";
    // const string TWITTER_ACCESS_TOKEN = "828513102983479296-l7IrYEnq6kwAcXsGT9pabL6xf8vEi8w";
    // const string TWITTER_ACCESS_TOKEN_SECRET = "eE4UkJvXVFcSDvBC01HTCyFM8WqsZkdK3lh2p3dmoIC3J";

    var consumerKey = ConfigurationManager.AppSettings["TWITTER_YORKDEVELOPERS_CONSUMER_KEY"];
    var consumerSecret = ConfigurationManager.AppSettings["TWITTER_YORKDEVELOPERS_CONSUMER_SECRET"];
    var consumerAccessToken = ConfigurationManager.AppSettings["TWITTER_YORKDEVELOPERS_CONSUMER_ACCESS_TOKEN"];
    var consumerAccessTokenSecret = ConfigurationManager.AppSettings["TWITTER_YORKDEVELOPERS_CONSUMER_ACCESS_TOKEN_SECRET"];

    const string SOURCEFILENAME = "_data/Events.yml";

    var gitHub = new GitHub(log);
    var deserializer = new Deserializer();
    
    var yaml = gitHub.ReadFileFromGitHub(SOURCEFILENAME);
    var Events = deserializer.Deserialize<List<Common>>(yaml);

    foreach (var event in Events)
    {
        if ((event.StartDate - dateTime.Now()).TotalDays == 7 && event.Endorsed)
        {
            SendTweet(event.name + " " + event.Starts.ToString("dd/M/yyyy") + " " + event.URL + " #codeyork");
        } 
    }
                  
}

public static TwitterStatus SendTweet(string tweetText)
{
    var twitterApp = new TwitterService(TWITTER_YORKDEVELOPERS_CONSUMER_KEY, TWITTER_YORKDEVELOPERS_CONSUMER_SECRET);
    twitterApp.AuthenticateWith(TWITTER_YORKDEVELOPERS_CONSUMER_ACCESS_TOKEN, TWITTER_YORKDEVELOPERS_CONSUMER_ACCESS_TOKEN_SECRET);
    return twitterApp.SendTweet(new SendTweetOptions { Status = tweetText });
}


public static string RemoveSpacesAndTitleCase(string strToConvert)
{
    // Creates a TextInfo based on the "en-US" culture.
    TextInfo myTI = new CultureInfo("en-US", false).TextInfo;

    // Changes a string to titlecase, then replace the spaces with empty
    return myTI.ToTitleCase(strToConvert).Replace(" ", "");
}

