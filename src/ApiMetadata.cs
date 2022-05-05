using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace LeechCode
{
    public class ApiMetadata
    {
        public Problem[] stat_status_pairs { get; set; }
    }
    
    public class Problem
    {
        public Stat stat { get; set; }
        public string status { get; set; }
        public Difficulty difficulty { get; set; }
        public bool paid_only { get; set; }
        public bool is_favor { get; set; }
        public float frequency { get; set; }
        public float progress { get; set; }
        
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public QuestionDetails? question_details { get; set; }
        
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SolutionDetails? solution_details { get; set; }
    }

    public class Stat
    {
        public int question_id { get; set; }
        public bool? question__article__live { get; set; }
        public string question__article__slug { get; set; }
        public bool? question__article__has_video_solution { get; set; }
        public string question__title { get; set; }
        public string question__title_slug { get; set; }
        public bool question__hide { get; set; }
        public int total_acs { get; set; }
        public int total_submitted { get; set; }
        public int frontend_question_id { get; set; }
        public bool is_new_question { get; set; }
    }

    public class Difficulty
    {
        public int level { get; set; }
    }


    public class QuestionDetails
    {
        public bool? has_solution { get; set; }
        public bool? premium_solution { get; set; }
        public List<string> languages { get; set; }
        public DateTime? last_fetch { get; set; }
    }
    public class SolutionDetails
    {
        public List<string> languages { get; set; }
        public DateTime? last_fetch { get; set; }
        public SolutionIframe[] frames { get; set; }
        public VimeoVideo[] vimeoVideos { get; set; }
    }
    public class SolutionIframe
    {
        public SolutionIframeCodeTab[] tabs { get; set; }
    }
    public class SolutionIframeCodeTab
    {
        public string language { get; set; }

    }
    public class VimeoVideo
    {
        public string Url { get; set; }
        public string VideoUrl { get; set; }
        public string Thumbnail { get; set; }
    }

}

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
