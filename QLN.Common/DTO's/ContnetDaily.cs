using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ContnetDaily
    {

        public class Rootobject
        {
            public Qln_Contents_Daily qln_contents_daily { get; set; }
        }

        public class Qln_Contents_Daily
        {
            public Qln_Contents_Daily_Event qln_contents_daily_event { get; set; }
            public Qln_Contents_Daily_Featured_Events qln_contents_daily_featured_events { get; set; }
            public Qln_Contents_Daily_More_Articles qln_contents_daily_more_articles { get; set; }
            public Qln_Contents_Daily_Topics_1 qln_contents_daily_topics_1 { get; set; }
            public Qln_Contents_Daily_Topics_2 qln_contents_daily_topics_2 { get; set; }
            public Qln_Contents_Daily_Topics_3 qln_contents_daily_topics_3 { get; set; }
            public Qln_Contents_Daily_Topics_4 qln_contents_daily_topics_4 { get; set; }
            public Qln_Contents_Daily_Topics_5 qln_contents_daily_topics_5 { get; set; }
            public Qln_Contents_Daily_Top_Stories qln_contents_daily_top_stories { get; set; }
            public Qln_Contents_Daily_Top_Story qln_contents_daily_top_story { get; set; }
            public Qln_Contents_Daily_Watch_On_Qatar_Living qln_contents_daily_watch_on_qatar_living { get; set; }
        }

        public class Qln_Contents_Daily_Event
        {
            public string queue_label { get; set; }
            public Item[] items { get; set; }
        }

        public class Item
        {
            public string page_name { get; set; }
            public string queue_name { get; set; }
            public string queue_label { get; set; }
            public string node_type { get; set; }
            public string nid { get; set; }
            public string date_created { get; set; }
            public string image_url { get; set; }
            public string user_name { get; set; }
            public string title { get; set; }
            public string slug { get; set; }
            public string category { get; set; }
            public string category_id { get; set; }
            public string event_venue { get; set; }
            public string event_start { get; set; }
            public string event_end { get; set; }
            public string event_lat { get; set; }
            public string event_long { get; set; }
            public string description { get; set; }
        }

        public class Qln_Contents_Daily_Featured_Events
        {
            public string queue_label { get; set; }
            public Item1[] items { get; set; }
        }

        public class Item1
        {
            public string page_name { get; set; }
            public string queue_name { get; set; }
            public string queue_label { get; set; }
            public string node_type { get; set; }
            public string nid { get; set; }
            public string date_created { get; set; }
            public string image_url { get; set; }
            public string user_name { get; set; }
            public string title { get; set; }
            public string slug { get; set; }
            public string category { get; set; }
            public string category_id { get; set; }
            public string event_venue { get; set; }
            public string event_start { get; set; }
            public string event_end { get; set; }
            public string event_lat { get; set; }
            public string event_long { get; set; }
            public string description { get; set; }
        }

        public class Qln_Contents_Daily_More_Articles
        {
            public string queue_label { get; set; }
            public Item2[] items { get; set; }
        }

        public class Item2
        {
            public string page_name { get; set; }
            public string queue_name { get; set; }
            public string queue_label { get; set; }
            public string node_type { get; set; }
            public string nid { get; set; }
            public string date_created { get; set; }
            public string image_url { get; set; }
            public string user_name { get; set; }
            public string title { get; set; }
            public string slug { get; set; }
            public string category { get; set; }
            public string category_id { get; set; }
            public string description { get; set; }
        }

        public class Qln_Contents_Daily_Topics_1
        {
            public string queue_label { get; set; }
            public Item3[] items { get; set; }
        }

        public class Item3
        {
            public string page_name { get; set; }
            public string queue_name { get; set; }
            public string queue_label { get; set; }
            public string node_type { get; set; }
            public string nid { get; set; }
            public string date_created { get; set; }
            public string image_url { get; set; }
            public string user_name { get; set; }
            public string title { get; set; }
            public string slug { get; set; }
            public string category { get; set; }
            public string category_id { get; set; }
            public string description { get; set; }
        }

        public class Qln_Contents_Daily_Topics_2
        {
            public string queue_label { get; set; }
            public Item4[] items { get; set; }
        }

        public class Item4
        {
            public string page_name { get; set; }
            public string queue_name { get; set; }
            public string queue_label { get; set; }
            public string node_type { get; set; }
            public string nid { get; set; }
            public string date_created { get; set; }
            public string image_url { get; set; }
            public string user_name { get; set; }
            public string title { get; set; }
            public string slug { get; set; }
            public string category { get; set; }
            public string category_id { get; set; }
            public string description { get; set; }
        }

        public class Qln_Contents_Daily_Topics_3
        {
            public string queue_label { get; set; }
            public Item5[] items { get; set; }
        }

        public class Item5
        {
            public string page_name { get; set; }
            public string queue_name { get; set; }
            public string queue_label { get; set; }
            public string node_type { get; set; }
            public string nid { get; set; }
            public string date_created { get; set; }
            public string image_url { get; set; }
            public string user_name { get; set; }
            public string title { get; set; }
            public string slug { get; set; }
            public string category { get; set; }
            public string category_id { get; set; }
            public string description { get; set; }
        }

        public class Qln_Contents_Daily_Topics_4
        {
            public string queue_label { get; set; }
            public Item6[] items { get; set; }
        }

        public class Item6
        {
            public string page_name { get; set; }
            public string queue_name { get; set; }
            public string queue_label { get; set; }
            public string node_type { get; set; }
            public string nid { get; set; }
            public string date_created { get; set; }
            public string image_url { get; set; }
            public string user_name { get; set; }
            public string title { get; set; }
            public string slug { get; set; }
            public string category { get; set; }
            public string category_id { get; set; }
            public string description { get; set; }
        }

        public class Qln_Contents_Daily_Topics_5
        {
            public string queue_label { get; set; }
            public Item7[] items { get; set; }
        }

        public class Item7
        {
            public string page_name { get; set; }
            public string queue_name { get; set; }
            public string queue_label { get; set; }
            public string node_type { get; set; }
            public string nid { get; set; }
            public string date_created { get; set; }
            public string image_url { get; set; }
            public string user_name { get; set; }
            public string title { get; set; }
            public string slug { get; set; }
            public string category { get; set; }
            public string category_id { get; set; }
            public string description { get; set; }
        }

        public class Qln_Contents_Daily_Top_Stories
        {
            public string queue_label { get; set; }
            public Item8[] items { get; set; }
        }

        public class Item8
        {
            public string page_name { get; set; }
            public string queue_name { get; set; }
            public string queue_label { get; set; }
            public string node_type { get; set; }
            public string nid { get; set; }
            public string date_created { get; set; }
            public string image_url { get; set; }
            public string user_name { get; set; }
            public string title { get; set; }
            public string slug { get; set; }
            public string category { get; set; }
            public string category_id { get; set; }
            public string description { get; set; }
        }

        public class Qln_Contents_Daily_Top_Story
        {
            public string queue_label { get; set; }
            public Item9[] items { get; set; }
        }

        public class Item9
        {
            public string page_name { get; set; }
            public string queue_name { get; set; }
            public string queue_label { get; set; }
            public string node_type { get; set; }
            public string nid { get; set; }
            public string date_created { get; set; }
            public string image_url { get; set; }
            public string user_name { get; set; }
            public string title { get; set; }
            public string slug { get; set; }
            public string category { get; set; }
            public string category_id { get; set; }
            public string description { get; set; }
        }

        public class Qln_Contents_Daily_Watch_On_Qatar_Living
        {
            public string queue_label { get; set; }
            public Item10[] items { get; set; }
        }

        public class Item10
        {
            public string page_name { get; set; }
            public string queue_name { get; set; }
            public string queue_label { get; set; }
            public string node_type { get; set; }
            public string nid { get; set; }
            public string date_created { get; set; }
            public string image_url { get; set; }
            public string video_url { get; set; }
            public string user_name { get; set; }
            public string title { get; set; }
        }

    }
}
