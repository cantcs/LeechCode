namespace LeechCode.Vimeo
{
    public class VimeoRoot
    {
        public string cdn_url { get; set; }
        public string vimeo_api_url { get; set; }
        public Request request { get; set; }
        public string player_url { get; set; }
        public Video video { get; set; }
        public User user { get; set; }
        public Seo seo { get; set; }
        public Embed embed { get; set; }
        public int view { get; set; }
        public string vimeo_url { get; set; }
    }

    public class Request
    {
        public Files files { get; set; }
        public string lang { get; set; }
        public Sentry sentry { get; set; }
        public Thumb_Preview thumb_preview { get; set; }
        public string referrer { get; set; }
        public string cookie_domain { get; set; }
        public int timestamp { get; set; }
        public Gc_Debug gc_debug { get; set; }
        public int expires { get; set; }
        public Client client { get; set; }
        public string currency { get; set; }
        public string session { get; set; }
        public Cookie cookie { get; set; }
        public Build build { get; set; }
        public Urls urls { get; set; }
        public string signature { get; set; }
        public Flags flags { get; set; }
        public string country { get; set; }
        public File_Codecs file_codecs { get; set; }
        public Ab_Tests ab_tests { get; set; }
    }

    public class Files
    {
        public Dash dash { get; set; }
        public Hls hls { get; set; }
        public Progressive[] progressive { get; set; }
    }

    public class Dash
    {
        public bool separate_av { get; set; }
        public Stream[] streams { get; set; }
        public Cdns cdns { get; set; }
        public Streams_Avc[] streams_avc { get; set; }
        public string default_cdn { get; set; }
    }

    public class Cdns
    {
        public Akfire_Interconnect_Quic akfire_interconnect_quic { get; set; }
        public Fastly_Skyfire fastly_skyfire { get; set; }
    }

    public class Akfire_Interconnect_Quic
    {
        public string url { get; set; }
        public string origin { get; set; }
        public string avc_url { get; set; }
    }

    public class Fastly_Skyfire
    {
        public string url { get; set; }
        public string origin { get; set; }
        public string avc_url { get; set; }
    }

    public class Stream
    {
        public object profile { get; set; }
        public string quality { get; set; }
        public string id { get; set; }
        public int fps { get; set; }
    }

    public class Streams_Avc
    {
        public object profile { get; set; }
        public string quality { get; set; }
        public string id { get; set; }
        public int fps { get; set; }
    }

    public class Hls
    {
        public bool separate_av { get; set; }
        public string default_cdn { get; set; }
        public Cdns1 cdns { get; set; }
    }

    public class Cdns1
    {
        public Akfire_Interconnect_Quic1 akfire_interconnect_quic { get; set; }
        public Fastly_Skyfire1 fastly_skyfire { get; set; }
    }

    public class Akfire_Interconnect_Quic1
    {
        public string url { get; set; }
        public string origin { get; set; }
        public string avc_url { get; set; }
    }

    public class Fastly_Skyfire1
    {
        public string url { get; set; }
        public string origin { get; set; }
        public string avc_url { get; set; }
    }

    public class Progressive
    {
        public string profile { get; set; }
        public int width { get; set; }
        public string mime { get; set; }
        public int fps { get; set; }
        public string url { get; set; }
        public string cdn { get; set; }
        public string quality { get; set; }
        public string id { get; set; }
        public string origin { get; set; }
        public int height { get; set; }
    }

    public class Sentry
    {
        public string url { get; set; }
        public bool enabled { get; set; }
        public bool debug_enabled { get; set; }
        public int debug_intent { get; set; }
    }

    public class Thumb_Preview
    {
        public string url { get; set; }
        public int frame_width { get; set; }
        public float height { get; set; }
        public int width { get; set; }
        public int frame_height { get; set; }
        public int frames { get; set; }
        public int columns { get; set; }
    }

    public class Gc_Debug
    {
        public string bucket { get; set; }
    }

    public class Client
    {
        public string ip { get; set; }
    }

    public class Cookie
    {
        public float volume { get; set; }
        public Captions_Styles captions_styles { get; set; }
        public int scaling { get; set; }
        public object captions { get; set; }
        public object quality { get; set; }
        public int hd { get; set; }
    }

    public class Captions_Styles
    {
        public object fontFamily { get; set; }
        public object fontSize { get; set; }
        public object fontOpacity { get; set; }
        public object color { get; set; }
        public object windowColor { get; set; }
        public object edgeStyle { get; set; }
        public object bgColor { get; set; }
        public object windowOpacity { get; set; }
        public object bgOpacity { get; set; }
    }

    public class Build
    {
        public string backend { get; set; }
        public string js { get; set; }
    }

    public class Urls
    {
        public string barebone_js { get; set; }
        public string test_imp { get; set; }
        public string js_base { get; set; }
        public string fresnel { get; set; }
        public string js { get; set; }
        public string proxy { get; set; }
        public string mux_url { get; set; }
        public string fresnel_mimir_inputs_url { get; set; }
        public string fresnel_chunk_url { get; set; }
        public string three_js { get; set; }
        public string vuid_js { get; set; }
        public string fresnel_manifest_url { get; set; }
        public string chromeless_css { get; set; }
        public string player_telemetry_url { get; set; }
        public string chromeless_js { get; set; }
        public string css { get; set; }
    }

    public class Flags
    {
        public int dnt { get; set; }
        public string preload_video { get; set; }
        public int plays { get; set; }
        public int partials { get; set; }
        public int autohide_controls { get; set; }
    }

    public class File_Codecs
    {
        public Hevc hevc { get; set; }
        public object[] av1 { get; set; }
        public string[] avc { get; set; }
    }

    public class Hevc
    {
        public object[] hdr { get; set; }
        public object[] sdr { get; set; }
        public object[] dvh1 { get; set; }
    }

    public class Ab_Tests
    {
        public Webvr webvr { get; set; }
        public Chromecast chromecast { get; set; }
        public Cmcd cmcd { get; set; }
        public Llhls_Timeout llhls_timeout { get; set; }
        public Stats_Fresnel stats_fresnel { get; set; }
    }

    public class Webvr
    {
        public bool track { get; set; }
        public Data data { get; set; }
        public bool group { get; set; }
    }

    public class Data
    {
    }

    public class Chromecast
    {
        public bool track { get; set; }
        public Data1 data { get; set; }
        public bool group { get; set; }
    }

    public class Data1
    {
    }

    public class Cmcd
    {
        public bool track { get; set; }
        public Data2 data { get; set; }
        public bool group { get; set; }
    }

    public class Data2
    {
    }

    public class Llhls_Timeout
    {
        public bool track { get; set; }
        public Data3 data { get; set; }
        public bool group { get; set; }
    }

    public class Data3
    {
    }

    public class Stats_Fresnel
    {
        public bool track { get; set; }
        public Data4 data { get; set; }
        public bool group { get; set; }
    }

    public class Data4
    {
    }

    public class Video
    {
        public Version version { get; set; }
        public int height { get; set; }
        public int duration { get; set; }
        public Thumbs thumbs { get; set; }
        public Owner owner { get; set; }
        public int id { get; set; }
        public string embed_code { get; set; }
        public string title { get; set; }
        public string share_url { get; set; }
        public int width { get; set; }
        public string embed_permission { get; set; }
        public float fps { get; set; }
        public int spatial { get; set; }
        public object live_event { get; set; }
        public int allow_hd { get; set; }
        public int hd { get; set; }
        public object lang { get; set; }
        public int default_to_hd { get; set; }
        public object url { get; set; }
        public string privacy { get; set; }
        public string bypass_token { get; set; }
        public object unlisted_hash { get; set; }
    }

    public class Version
    {
        public object current { get; set; }
        public Available[] available { get; set; }
    }

    public class Available
    {
        public int is_current { get; set; }
        public int id { get; set; }
        public long file_id { get; set; }
    }

    public class Thumbs
    {
        public string _1280 { get; set; }
        public string _960 { get; set; }
        public string _640 { get; set; }
        public string _base { get; set; }
    }

    public class Owner
    {
        public string account_type { get; set; }
        public string name { get; set; }
        public string img { get; set; }
        public string url { get; set; }
        public string img_2x { get; set; }
        public int id { get; set; }
    }

    public class User
    {
        public int team_origin_user_id { get; set; }
        public int liked { get; set; }
        public string account_type { get; set; }
        public object vimeo_api_client_token { get; set; }
        public object vimeo_api_interaction_tokens { get; set; }
        public int team_id { get; set; }
        public int watch_later { get; set; }
        public int owner { get; set; }
        public int id { get; set; }
        public int mod { get; set; }
        public int private_mode_enabled { get; set; }
        public int logged_in { get; set; }
    }

    public class Seo
    {
        public string upload_date { get; set; }
        public string embed_url { get; set; }
        public string description { get; set; }
        public string thumbnail { get; set; }
    }

    public class Embed
    {
        public int autopause { get; set; }
        public string color { get; set; }
        public string texttrack { get; set; }
        public int on_site { get; set; }
        public string app_id { get; set; }
        public Chapter[] chapters { get; set; }
        public string outro { get; set; }
        public object api { get; set; }
        public int keyboard { get; set; }
        public string player_id { get; set; }
        public object quality { get; set; }
        public int muted { get; set; }
        public int dnt { get; set; }
        public bool editor { get; set; }
        public int log_plays { get; set; }
        public int playsinline { get; set; }
        public int transparent { get; set; }
        public Settings settings { get; set; }
        public string context { get; set; }
        public int time { get; set; }
        public int loop { get; set; }
        public int autoplay { get; set; }
    }

    public class Settings
    {
        public int fullscreen { get; set; }
        public int byline { get; set; }
        public int like { get; set; }
        public int playbar { get; set; }
        public int title { get; set; }
        public int color { get; set; }
        public int speed { get; set; }
        public int watch_later { get; set; }
        public int share { get; set; }
        public int scaling { get; set; }
        public int spatial_compass { get; set; }
        public int collections { get; set; }
        public int portrait { get; set; }
        public int logo { get; set; }
        public int embed { get; set; }
        public int badge { get; set; }
        public int spatial_label { get; set; }
        public int volume { get; set; }
    }

    public class Chapter
    {
        public int timecode { get; set; }
        public string title { get; set; }
        public int id { get; set; }
        public int clip_id { get; set; }
    }

}
