﻿using N_m3u8DL_RE.Parser.Config;
using N_m3u8DL_RE.Common.Entity;
using N_m3u8DL_RE.Common.Enum;
using N_m3u8DL_RE.Common.Log;
using N_m3u8DL_RE.Common.Resource;
using N_m3u8DL_RE.Parser.Util;
using N_m3u8DL_RE.Parser.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using N_m3u8DL_RE.Common.Util;

namespace N_m3u8DL_RE.Parser.Extractor
{
    internal class HLSExtractor : IExtractor
    {
        public ExtractorType ExtractorType => ExtractorType.HLS;

        private string M3u8Url = string.Empty;
        private string BaseUrl = string.Empty;
        private string M3u8Content = string.Empty;

        public ParserConfig ParserConfig { get; set; }

        private HLSExtractor() { }

        public HLSExtractor(ParserConfig parserConfig)
        {
            this.ParserConfig = parserConfig;
            this.M3u8Url = parserConfig.Url ?? string.Empty;
            this.SetBaseUrl();
        }

        private void SetBaseUrl()
        {
            if (!string.IsNullOrEmpty(ParserConfig.BaseUrl))
            {
                this.BaseUrl = ParserConfig.BaseUrl;
            }
            else
            {
                this.BaseUrl = ParserConfig.BaseUrl = this.M3u8Url;
            }
        }

        /// <summary>
        /// 预处理m3u8内容
        /// </summary>
        public void PreProcessContent()
        {
            M3u8Content = M3u8Content.Trim();
            if (!M3u8Content.StartsWith(HLSTags.ext_m3u))
            {
                throw new Exception(ResString.badM3u8);
            }

            foreach (var p in ParserConfig.ContentProcessors)
            {
                if (p.CanProcess(ExtractorType, M3u8Content, ParserConfig))
                {
                    M3u8Content = p.Process(M3u8Content, ParserConfig);
                }
            }
        }

        /// <summary>
        /// 预处理URL
        /// </summary>
        public string PreProcessUrl(string url)
        {
            foreach (var p in ParserConfig.UrlProcessors)
            {
                if (p.CanProcess(ExtractorType, url, ParserConfig))
                {
                    url = p.Process(url, ParserConfig);
                }
            }

            return url;
        }

        private bool IsMaster()
        {
            return M3u8Content.Contains(HLSTags.ext_x_stream_inf);
        }

        private async Task<List<StreamSpec>> ParseMasterListAsync()
        {
            List<StreamSpec> streams = new List<StreamSpec>();

            using StringReader sr = new StringReader(M3u8Content);
            string? line;
            bool expectPlaylist = false;
            StreamSpec streamSpec = new();

            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.StartsWith(HLSTags.ext_x_stream_inf))
                {
                    streamSpec = new();
                    var bandwidth = string.IsNullOrEmpty(ParserUtil.GetAttribute(line, "AVERAGE-BANDWIDTH")) ? ParserUtil.GetAttribute(line, "BANDWIDTH") : ParserUtil.GetAttribute(line, "AVERAGE-BANDWIDTH");
                    streamSpec.Bandwidth = Convert.ToInt32(bandwidth);
                    streamSpec.Codecs = ParserUtil.GetAttribute(line, "CODECS");
                    streamSpec.Resolution = ParserUtil.GetAttribute(line, "RESOLUTION");

                    var frameRate = ParserUtil.GetAttribute(line, "FRAME-RATE");
                    if (!string.IsNullOrEmpty(frameRate))
                        streamSpec.FrameRate = Convert.ToDouble(frameRate);

                    var audioId = ParserUtil.GetAttribute(line, "AUDIO");
                    if (!string.IsNullOrEmpty(audioId))
                        streamSpec.AudioId = audioId;

                    var videoId = ParserUtil.GetAttribute(line, "VIDEO");
                    if (!string.IsNullOrEmpty(videoId))
                        streamSpec.VideoId = videoId;

                    var subtitleId = ParserUtil.GetAttribute(line, "SUBTITLES");
                    if (!string.IsNullOrEmpty(subtitleId))
                        streamSpec.SubtitleId = subtitleId;

                    expectPlaylist = true;
                }
                else if (line.StartsWith(HLSTags.ext_x_media))
                {
                    streamSpec = new();
                    var type = ParserUtil.GetAttribute(line, "TYPE").Replace("-", "_");
                    if (Enum.TryParse<MediaType>(type, out var mediaType))
                    {
                        streamSpec.MediaType = mediaType;
                    }

                    //跳过CLOSED_CAPTIONS类型（目前不支持）
                    if (streamSpec.MediaType == MediaType.CLOSED_CAPTIONS)
                    {
                        continue;
                    }

                    var url = ParserUtil.GetAttribute(line, "URI");

                    /**
                     *    The URI attribute of the EXT-X-MEDIA tag is REQUIRED if the media
                          type is SUBTITLES, but OPTIONAL if the media type is VIDEO or AUDIO.
                          If the media type is VIDEO or AUDIO, a missing URI attribute
                          indicates that the media data for this Rendition is included in the
                          Media Playlist of any EXT-X-STREAM-INF tag referencing this EXT-
                          X-MEDIA tag.  If the media TYPE is AUDIO and the URI attribute is
                          missing, clients MUST assume that the audio data for this Rendition
                          is present in every video Rendition specified by the EXT-X-STREAM-INF
                          tag.
                          
                          此处直接忽略URI属性为空的情况
                     */
                    if (string.IsNullOrEmpty(url))
                    {
                        continue;
                    }

                    url = ParserUtil.CombineURL(BaseUrl, url);
                    streamSpec.Url = PreProcessUrl(url);

                    var groupId = ParserUtil.GetAttribute(line, "GROUP-ID");
                    streamSpec.GroupId = groupId;

                    var lang = ParserUtil.GetAttribute(line, "LANGUAGE");
                    if (!string.IsNullOrEmpty(lang))
                        streamSpec.Language = lang;

                    var name = ParserUtil.GetAttribute(line, "NAME");
                    if (!string.IsNullOrEmpty(name))
                        streamSpec.Name = name;

                    var def = ParserUtil.GetAttribute(line, "DEFAULT");
                    if (Enum.TryParse<Choise>(type, out var defaultChoise))
                    {
                        streamSpec.Default = defaultChoise;
                    }

                    var channels = ParserUtil.GetAttribute(line, "CHANNELS");
                    if (!string.IsNullOrEmpty(channels))
                        streamSpec.Channels = channels;

                    streams.Add(streamSpec);
                }
                else if (line.StartsWith("#"))
                {
                    continue;
                }
                else if (expectPlaylist)
                {
                    var url = ParserUtil.CombineURL(BaseUrl, line);
                    streamSpec.Url = PreProcessUrl(url);
                    expectPlaylist = false;
                    streams.Add(streamSpec);
                }
            }

            return streams;
        }

        private async Task<Playlist> ParseListAsync()
        {
            //标记是否已清除优酷广告分片
            bool hasAd = false;

            using StringReader sr = new StringReader(M3u8Content);
            string? line;
            bool expectSegment = false;
            bool isEndlist = false;
            long segIndex = 0;
            bool isAd = false;
            long startIndex;

            Playlist playlist = new();
            List<MediaPart> mediaParts = new();

            //当前的加密信息
            EncryptInfo currentEncryptInfo = new();
            if (ParserConfig.CustomeKey != null)
            {
                currentEncryptInfo.Method = ParserConfig.CustomMethod ?? EncryptMethod.AES_128;
                currentEncryptInfo.Key = ParserConfig.CustomeKey;
                if (ParserConfig.CustomeIV != null)
                    currentEncryptInfo.IV = ParserConfig.CustomeIV;
            }
            //上次读取到的加密行，#EXT-X-KEY:……
            string lastKeyLine = "";

            MediaPart mediaPart = new();
            MediaSegment segment = new();
            List<MediaSegment> segments = new();


            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                //只下载部分字节
                if (line.StartsWith(HLSTags.ext_x_byterange))
                {
                    var p = ParserUtil.GetAttribute(line);
                    var (n, o) = ParserUtil.GetRange(p);
                    segment.ExpectLength = n;
                    segment.StartRange = o ?? segments.Last().StartRange + segments.Last().ExpectLength;
                    expectSegment = true;
                }
                //国家地理去广告
                else if (line.StartsWith("#UPLYNK-SEGMENT"))
                {
                    if (line.Contains(",ad"))
                        isAd = true;
                    else if (line.Contains(",segment"))
                        isAd = false;
                }
                //国家地理去广告
                else if (isAd)
                {
                    continue;
                }
                //解析定义的分段长度
                else if (line.StartsWith(HLSTags.ext_x_targetduration))
                {
                    playlist.TargetDuration = Convert.ToDouble(ParserUtil.GetAttribute(line));
                }
                //解析起始编号
                else if (line.StartsWith(HLSTags.ext_x_media_sequence))
                {
                    segIndex = Convert.ToInt64(ParserUtil.GetAttribute(line));
                    startIndex = segIndex;
                }
                //program date time
                else if (line.StartsWith(HLSTags.ext_x_program_date_time))
                {
                    //
                }
                //解析不连续标记，需要单独合并（timestamp不同）
                else if (line.StartsWith(HLSTags.ext_x_discontinuity))
                {
                    //修复优酷去除广告后的遗留问题
                    if (hasAd && mediaParts.Count > 0)
                    {
                        segments = mediaParts[mediaParts.Count - 1].MediaSegments;
                        mediaParts.RemoveAt(mediaParts.Count - 1);
                        hasAd = false;
                        continue;
                    }
                    //常规情况的#EXT-X-DISCONTINUITY标记，新建part
                    if (!hasAd && segments.Count > 1)
                    {
                        mediaParts.Add(new MediaPart()
                        {
                            MediaSegments = segments,
                        });
                        segments = new();
                    }
                }
                //解析KEY
                else if (line.StartsWith(HLSTags.ext_x_key))
                {
                    //自定义KEY情况 不读取当前行的KEY信息.
                    //对于IV，没自定义且当前行有IV的话 就用
                    if (ParserConfig.CustomeKey != null)
                    {
                        if (ParserConfig.CustomeIV == null && line.Contains("IV=0x"))
                            currentEncryptInfo.IV = HexUtil.HexToBytes(ParserUtil.GetAttribute(line, "IV"));
                        continue;
                    }

                    var iv = ParserUtil.GetAttribute(line, "IV");
                    var method = ParserUtil.GetAttribute(line, "METHOD");
                    var uri = ParserUtil.GetAttribute(line, "URI");
                    var uri_last = ParserUtil.GetAttribute(lastKeyLine, "URI");
                    
                    //如果KEY URL相同，不进行重复解析
                    if (uri != uri_last)
                    {
                        //加密方式
                        currentEncryptInfo.Method = EncryptInfo.ParseMethod(method);
                        //IV
                        if (!string.IsNullOrEmpty(iv))
                        {
                            currentEncryptInfo.IV = HexUtil.HexToBytes(iv);
                        }
                        //KEY
                        var parsedInfo = ParseKey(method, uri);
                        currentEncryptInfo.Key = parsedInfo.Key;
                        //加密方式被处理器更改
                        if (currentEncryptInfo.Method != parsedInfo.Method)
                        {
                            currentEncryptInfo.Method = parsedInfo.Method;
                        }
                    }
                    lastKeyLine = line;
                }
                //解析分片时长
                else if (line.StartsWith(HLSTags.extinf))
                {
                    string[] tmp = ParserUtil.GetAttribute(line).Split(',');
                    segment.Duration = Convert.ToDouble(tmp[0]);
                    segment.Index = segIndex;
                    //是否有加密，有的话写入KEY和IV
                    if (currentEncryptInfo.Method != EncryptMethod.NONE)
                    {
                        segment.EncryptInfo.Method = currentEncryptInfo.Method;
                        segment.EncryptInfo.Key = currentEncryptInfo.Key;
                        segment.EncryptInfo.IV = currentEncryptInfo.IV ?? HexUtil.HexToBytes(Convert.ToString(segIndex, 16).PadLeft(32, '0'));
                    }
                    expectSegment = true;
                    segIndex++;
                }
                //m3u8主体结束
                else if (line.StartsWith(HLSTags.ext_x_endlist))
                {
                    if (segments.Count > 0)
                    {
                        mediaParts.Add(new MediaPart()
                        {
                            MediaSegments = segments
                        });
                    }
                    segments = new();
                    isEndlist = true;
                }
                //#EXT-X-MAP
                else if (line.StartsWith(HLSTags.ext_x_map))
                {
                    if (playlist.MediaInit == null) 
                    {
                        playlist.MediaInit = new MediaSegment()
                        {
                            Url = PreProcessUrl(ParserUtil.CombineURL(BaseUrl, ParserUtil.GetAttribute(line, "URI"))),
                        };
                        if (line.Contains("BYTERANGE"))
                        {
                            var p = ParserUtil.GetAttribute(line, "BYTERANGE");
                            var (n, o) = ParserUtil.GetRange(p);
                            playlist.MediaInit.ExpectLength = n;
                            playlist.MediaInit.StartRange = o ?? 0L;
                        }
                    }
                    //遇到了其他的map，说明已经不是一个视频了，全部丢弃即可
                    else
                    {
                        if (segments.Count > 0)
                        {
                            mediaParts.Add(new MediaPart()
                            {
                                MediaSegments = segments
                            });
                        }
                        segments = new();
                        isEndlist = true;
                        break;
                    }
                }
                //评论行不解析
                else if (line.StartsWith("#")) continue;
                //空白行不解析
                else if (line.StartsWith("\r\n")) continue;
                //解析分片的地址
                else if (expectSegment)
                {
                    var segUrl = PreProcessUrl(ParserUtil.CombineURL(BaseUrl, line));
                    segment.Url = segUrl;
                    segments.Add(segment);
                    segment = new();
                    //优酷的广告分段则清除此分片
                    //需要注意，遇到广告说明程序对上文的#EXT-X-DISCONTINUITY做出的动作是不必要的，
                    //其实上下文是同一种编码，需要恢复到原先的part上
                    if (segUrl.Contains("ccode=") && segUrl.Contains("/ad/") && segUrl.Contains("duration="))
                    {
                        segments.RemoveAt(segments.Count - 1);
                        segIndex--;
                        hasAd = true;
                    }
                    //优酷广告(4K分辨率测试)
                    if (segUrl.Contains("ccode=0902") && segUrl.Contains("duration="))
                    {
                        segments.RemoveAt(segments.Count - 1);
                        segIndex--;
                        hasAd = true;
                    }
                    expectSegment = false;
                }
            }

            //直播的情况，无法遇到m3u8结束标记，需要手动将segments加入parts
            if (!isEndlist)
            {
                mediaParts.Add(new MediaPart()
                {
                    MediaSegments = segments
                });
            }

            playlist.MediaParts = mediaParts;
            playlist.IsLive = !isEndlist;

            //直播刷新间隔
            if (playlist.IsLive)
            {
                //由于播放器默认从最后3个分片开始播放 此处设置刷新间隔为TargetDuration的2倍
                playlist.RefreshIntervalMs = (int)((playlist.TargetDuration ?? 5) * 2 * 1000);
            }

            return playlist;
        }

        private EncryptInfo ParseKey(string method, string uriText)
        {
            foreach (var p in ParserConfig.KeyProcessors)
            {
                if (p.CanProcess(ExtractorType, method, uriText, M3u8Content, ParserConfig))
                {
                    //匹配到对应处理器后不再继续
                    return p.Process(method, uriText, M3u8Content, ParserConfig);
                }
            }

            throw new Exception(ResString.keyProcessorNotFound);
        }

        public async Task<List<StreamSpec>> ExtractStreamsAsync(string rawText)
        {
            this.M3u8Content = rawText;
            this.PreProcessContent();
            if (IsMaster())
            {
                Logger.Warn(ResString.masterM3u8Found);
                var lists = await ParseMasterListAsync();
                lists = lists.DistinctBy(p => p.Url).ToList();
                return lists;
            }
            else
            {
                var playlist = await ParseListAsync();
                return new List<StreamSpec>()
                {
                    new StreamSpec()
                    {
                        Url = ParserConfig.Url,
                        Playlist = playlist,
                        Extension = playlist.MediaInit != null ? "mp4" : "ts"
                    }
                };
            }
        }

        private async Task LoadM3u8FromUrlAsync(string url)
        {
            //Logger.Info(ResString.loadingUrl + url);
            if (url.StartsWith("file:"))
            {
                var uri = new Uri(url);
                this.M3u8Content = File.ReadAllText(uri.LocalPath);
            }
            else if (url.StartsWith("http"))
            {
                (this.M3u8Content, url) = await HTTPUtil.GetWebSourceAndNewUrlAsync(url, ParserConfig.Headers);
            }

            this.M3u8Url = url;
            this.SetBaseUrl();
            this.PreProcessContent();
        }

        public async Task FetchPlayListAsync(List<StreamSpec> lists)
        {
            for (int i = 0; i < lists.Count; i++)
            {
                //重新加载m3u8
                await LoadM3u8FromUrlAsync(lists[i].Url!);
                lists[i].Playlist = await ParseListAsync();
                if (lists[i].MediaType == MediaType.SUBTITLES)
                {
                    var a = lists[i].Playlist!.MediaParts.Any(p => p.MediaSegments.Any(m => m.Url.Contains(".ttml")));
                    var b = lists[i].Playlist!.MediaParts.Any(p => p.MediaSegments.Any(m => m.Url.Contains(".vtt") || m.Url.Contains(".webvtt")));
                    if (a) lists[i].Extension = "ttml";
                    if (b) lists[i].Extension = "vtt";
                }
                else
                {
                    lists[i].Extension = lists[i].Playlist!.MediaInit != null ? "m4s" : "ts";
                }
            }
        }
    }
}
