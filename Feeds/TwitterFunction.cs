using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Feeds.Shared;
using Feeds.Twitter;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using YamlDotNet.Serialization;

namespace Feeds
{
    public static class TwitterFunction
    {
        [FunctionName("Twitter")]
        public static void Run([TimerTrigger("0 12 3 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            // The key and secret assigned to our application by twitter
            var consumerKey = ConfigurationManager.AppSettings["TWITTER_CONSUMER_KEY"];
            var consumerSecret = ConfigurationManager.AppSettings["TWITTER_CONSUMER_SECRET"];

            // Encode our credentials into a single string
            var credentials = GetCredentials(consumerKey, consumerSecret);

            // Now we can exchange this for our bearer token
            var bearerToken = GetBearerToken(credentials);

            // Now get all the tweats YorkDevelopers have made
            var tweets = GetTweets(bearerToken);

            // We only care about the ones with the hashtag YorkDeveloperEvent
            var filteredTweets = tweets.Where(IsAnEvent);

            // Now convert them into our common structure
            var allEvents = new List<Common>();
            foreach (var tweet in filteredTweets)
            {
                var evt = new Common();

                // If this is a re-tweet then take the text from the original tweet,
                // otherwise take it from this tweet
                if (!string.IsNullOrWhiteSpace(tweet.quoted_status?.text))
                    evt.Description = TidyText(tweet.quoted_status.text, tweet.quoted_status.entities);
                else
                    evt.Description = TidyText(tweet.text, tweet.entities);

                // We don't really have the name of the event,  so just use the name of the
                // person who tweeted it.
                if (!string.IsNullOrWhiteSpace(tweet.quoted_status?.user?.name))
                    evt.Name = tweet.quoted_status.user.name;
                else
                    evt.Name = "YorkDevelopers";

                // Give the event a logo if we have one
                if (!string.IsNullOrWhiteSpace(tweet.quoted_status?.user?.profile_image_url))
                    evt.Logo = tweet.quoted_status.user.profile_image_url;

                // Take the URL from the retweeted event if present,  otherwise take the one from our text
                var url = tweet.quoted_status?.entities?.urls?.FirstOrDefault()?.expanded_url;
                if (!string.IsNullOrWhiteSpace(url))
                    evt.URL = url;
                else
                    evt.URL = tweet.entities?.urls?.FirstOrDefault()?.expanded_url ?? "";

                // Now the complicated bit.  Attempt to find the date from the text.
                if (!string.IsNullOrWhiteSpace(tweet.quoted_status?.text))
                {
                    // Check the original tweet
                    evt.Starts = FindDateInText(tweet.quoted_status?.text, ParseDate(tweet.quoted_status.created_at));
                }

                if (evt.Starts == DateTime.MinValue)
                {
                    // We haven't found a date,  check the parent tweet
                    evt.Starts = FindDateInText(tweet.text, ParseDate(tweet.created_at));
                }


                // Assume all day
                evt.Ends = evt.Starts;

                // We don't have the following fields available:
                //evt.Venue, evt.IsFree
                allEvents.Add(evt);

            }

            var serializer = new Serializer();
            var yaml = serializer.Serialize(allEvents);

            // Push the file to git
            var gitHubClient = new GitHub(log);
            gitHubClient.WriteFileToGitHub("_data/Twitter2.yml", yaml);
        }

        /// <summary>
        /// Removes any URLs or Hash tags from the text
        /// </summary>
        /// <param name="text"></param>
        /// <param name="entities"></param>
        /// <returns></returns>
        private static string TidyText(string text, entities entities)
        {
            if (entities?.hashtags != null)
            {
                foreach (var ht in entities.hashtags)
                {
                    for (int i = ht.indices[0]; i < ht.indices[1]; i++)
                    {
                        text = ReplaceAtIndex(i, '~', text);
                    }
                }
            }

            if (entities?.urls != null)
            {
                foreach (var ht in entities.urls)
                {
                    for (int i = ht.indices[0]; i < ht.indices[1]; i++)
                    {
                        text = ReplaceAtIndex(i, '~', text);
                    }
                }
            }

            return text.Replace("~", "").Trim();
        }

        static string ReplaceAtIndex(int i, char value, string word)
        {
            char[] letters = word.ToCharArray();
            letters[i] = value;
            return string.Join("", letters);
        }


        private static DateTime ParseDate(string created_at)
        {
            //Sat Mar 04 20:45:30 + 0000 2017
            DateTime dt;
            if (!DateTime.TryParseExact(created_at, "ddd MMM dd HH:mm:ss +ffff yyyy", null, System.Globalization.DateTimeStyles.None, out dt))
            {
                Debugger.Break();
                return DateTime.Now;
            }
            return dt;
        }

        private static DateTime FindDateInText(string text, DateTime created_at)
        {
            // First try and find a month in the text.
            var months = new List<string>() { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            var days = new List<string>() { "1st", "2nd", "3rd", "4th", "5th", "6th", "7th", "8th", "9th", "10th", "11th", "12th", "13th", "14th", "15th",
                                    "16th","17th","18th","19th","20th","21st","22nd","23rd","24th","25th","26th","27th","28th","29th","30th","31st"};
            // First try and find the full month
            var startOfMonth = -1;
            var detectedMonth = "";
            foreach (var month in months)
            {
                if (text.ToLower().Contains(month.ToLower()))
                {
                    startOfMonth = text.ToLower().IndexOf(month.ToLower());
                    detectedMonth = month;
                    break;
                }
            }

            // Then check for just the first three letters
            if (startOfMonth == -1)
            {
                foreach (var month in months)
                {
                    if (text.ToLower().Contains(month.ToLower().Substring(0, 3)))
                    {
                        startOfMonth = text.ToLower().IndexOf(month.ToLower().Substring(0, 3));
                        detectedMonth = month;
                        break;
                    }
                }
            }

            if (startOfMonth == -1) return DateTime.MinValue; //No luck :-(


            // Find the day in the string which occurs closest to the month
            var bestDay = 0;
            var bestDayDifference = int.MaxValue;
            var daysReversed = days.Reverse<string>();
            foreach (var day in daysReversed)
            {
                if (text.ToLower().Contains(day.ToLower()))
                {
                    var startOfDay = text.ToLower().IndexOf(day.ToLower());
                    if (startOfDay < startOfMonth)
                    {
                        var difference = startOfMonth - (startOfDay + day.Length);
                        if (difference < bestDayDifference)
                        {
                            bestDayDifference = difference;
                            bestDay = days.IndexOf(day) + 1;
                        }
                    }

                    if (startOfDay > startOfMonth)
                    {
                        var difference = startOfDay - (startOfMonth + detectedMonth.Length);
                        if (difference < bestDayDifference)
                        {
                            bestDayDifference = difference;
                            bestDay = days.IndexOf(day) + 1;
                        }
                    }
                }
            }
            if (bestDay == 0) return DateTime.MinValue;


            // Assume it's this year,  unless the month occurs before the tweet was made.  E.g. the tweet was made in June and the month is March then
            // the event must take place next year.
            var year = DateTime.Now.Year;
            if (months.IndexOf(detectedMonth) < created_at.Month)
                year++;

            return new DateTime(year, months.IndexOf(detectedMonth) + 1, bestDay);
        }

        private static bool IsAnEvent(tweet tweet)
        {
            // No hashtags
            if (tweet?.entities?.hashtags == null) return false;

            // Is there a hashtag with our tag?
            return (tweet.entities.hashtags.Any(ht => ht.text.ToLower() == "yorkdeveloperevent"));
        }

        private static List<tweet> GetTweets(string bearerToken)
        {
            var client = new HttpClient();
            client.BaseAddress = new System.Uri("https://api.twitter.com");

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + bearerToken);
            var httpResponseMessage = client.GetAsync("/1.1/statuses/user_timeline.json?include_rts=1&count=100&screen_name=yorkdevelopers").Result;
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                throw new Exception(string.Format("Failed to GET to {0}.   Status {1}.  Reason {2}.", "user_timeline", (int)httpResponseMessage.StatusCode, httpResponseMessage.ReasonPhrase));
            }
            else
            {
                return httpResponseMessage.Content.ReadAsAsync<List<tweet>>().Result;
            }
        }

        private static string GetCredentials(string consumerKey, string consumerSecret)
        {
            // Step 1 - URL Encode (RFC 1738).  Normally this will have no effect
            consumerKey = WebUtility.UrlEncode(consumerKey);
            consumerSecret = WebUtility.UrlEncode(consumerSecret);

            // Step 2 - Join them together
            var joined = $"{consumerKey}:{consumerSecret}";

            // Step 3 - base 64encode it
            var encoded = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(joined));
            return encoded;
        }

        /// <summary>
        /// Helper method to define our connection to the APF webservice.
        /// </summary>
        private static string GetBearerToken(string password)
        {
            var client = new HttpClient();
            client.BaseAddress = new System.Uri("https://api.twitter.com");

            client.DefaultRequestHeaders.Add("Authorization", "Basic " + password);
            var content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
            var httpResponseMessage = client.PostAsync("/oauth2/token", content).Result;
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                throw new Exception(string.Format("Failed to POST to {0}.   Status {1}.  Reason {2}.", "/oauth2/token", (int)httpResponseMessage.StatusCode, httpResponseMessage.ReasonPhrase));
            }
            else
            {
                var result = httpResponseMessage.Content.ReadAsAsync<TokenResult>().Result;
                if (result.token_type != "bearer") throw new Exception("Incorrect result from call to /oauth2/token");
                return result.access_token;
            }
        }

    }
}
