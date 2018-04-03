using System;
using YamlDotNet.Serialization;

namespace Feeds.Shared
{
    public class Common
    {
        [YamlMember(typeof(String), Alias = "name")]
        public string Name { get; set; }

        [YamlMember(typeof(String), Alias = "description")]
        public string Description { get; set; }

        [YamlMember(typeof(String), Alias = "url")]
        public string URL { get; set; }

        [YamlMember(typeof(bool), Alias = "is-free")]
        public bool IsFree { get; set; }

        [YamlMember(typeof(String), Alias = "logo")]
        public string Logo { get; set; }

        [YamlMember(typeof(DateTime), Alias = "starts")]
        public DateTime Starts { get; set; }

        [YamlMember(typeof(DateTime), Alias = "ends")]
        public DateTime Ends { get; set; }

        [YamlMember(typeof(string), Alias = "venue")]
        public string Venue { get; set; }

        [YamlMember(typeof(bool), Alias = "endorsed")]
        public bool Endorsed { get; set; }
    }
}
