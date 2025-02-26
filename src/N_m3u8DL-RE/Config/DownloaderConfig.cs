﻿using N_m3u8DL_RE.CommandLine;
using N_m3u8DL_RE.Enum;
using N_m3u8DL_RE.Parser.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace N_m3u8DL_RE.Config
{
    internal class DownloaderConfig
    {
        public DownloaderConfig() { }

        public DownloaderConfig(MyOption option)
        {
            AutoSubtitleFix = option.AutoSubtitleFix;
            SkipMerge = option.SkipMerge;
            BinaryMerge = option.BinaryMerge;
            DelAfterDone = option.DelAfterDone;
            CheckSegmentsCount = option.CheckSegmentsCount;
            SubtitleFormat = option.SubtitleFormat;
            TmpDir = option.TmpDir;
            SaveName = option.SaveName;
            SaveDir = option.SaveDir;
            ThreadCount = option.ThreadCount;
            SavePattern = option.SavePattern;
            Keys = option.Keys;
        }

        /// <summary>
        /// 临时文件存储目录
        /// </summary>
        public string? TmpDir { get; set; }
        /// <summary>
        /// 文件存储目录
        /// </summary>
        public string? SaveDir { get; set; }
        /// <summary>
        /// 文件名
        /// </summary>
        public string? SaveName { get; set; }
        /// <summary>
        /// 文件名模板
        /// </summary>
        public string? SavePattern { get; set; }
        /// <summary>
        /// 线程数
        /// </summary>
        public int ThreadCount { get; set; } = 8;
        /// <summary>
        /// 跳过合并
        /// </summary>
        public bool SkipMerge { get; set; } = false;
        /// <summary>
        /// 二进制合并
        /// </summary>
        public bool BinaryMerge { get; set; } = false;
        /// <summary>
        /// 完成后是否删除临时文件
        /// </summary>
        public bool DelAfterDone { get; set; } = false;
        /// <summary>
        /// 校验有没有下完全部分片
        /// </summary>
        public bool CheckSegmentsCount { get; set; } = true;
        /// <summary>
        /// 校验响应头的文件大小和实际大小
        /// </summary>
        public bool CheckContentLength { get; set; } = true;
        /// <summary>
        /// 自动修复字幕
        /// </summary>
        public bool AutoSubtitleFix { get; set; } = true;
        /// <summary>
        /// 字幕格式
        /// </summary>
        public SubtitleFormat SubtitleFormat { get; set; } = SubtitleFormat.VTT;
        /// <summary>
        /// 请求头
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>()
        {
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36"
        };
        /// <summary>
        /// 解密KEYs
        /// </summary>
        public string[]? Keys { get; set; }
    }
}
