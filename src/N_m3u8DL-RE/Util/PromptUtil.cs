﻿using N_m3u8DL_RE.Common.Entity;
using N_m3u8DL_RE.Common.Enum;
using N_m3u8DL_RE.Common.Resource;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace N_m3u8DL_RE.Util
{
    public class PromptUtil
    {
        public static List<StreamSpec> SelectStreams(IEnumerable<StreamSpec> lists)
        {
            if (lists.Count() == 1)
                return new List<StreamSpec>(lists);

            //基本流
            var basicStreams = lists.Where(x => x.MediaType == null);
            //可选音频轨道
            var audios = lists.Where(x => x.MediaType == MediaType.AUDIO);
            //可选字幕轨道
            var subs = lists.Where(x => x.MediaType == MediaType.SUBTITLES);

            var prompt = new MultiSelectionPrompt<StreamSpec>()
                        .Title(ResString.promptTitle)
                        .UseConverter(x =>
                        {
                            if (x.Name != null && x.Name.StartsWith("__"))
                                return $"[darkslategray1]{x.Name.Substring(2)}[/]";
                            else
                                return x.ToString().EscapeMarkup().RemoveMarkup();
                        })
                        .Required()
                        .PageSize(10)
                        .MoreChoicesText(ResString.promptChoiceText)
                        .InstructionsText(ResString.promptInfo)
                        ;

            //默认选中第一个
            var first = lists.First();
            prompt.Select(first);

            if (basicStreams.Any())
            {
                prompt.AddChoiceGroup(new StreamSpec() { Name = "__Basic" }, basicStreams);
            }

            if (audios.Any())
            {
                prompt.AddChoiceGroup(new StreamSpec() { Name = "__Audio" }, audios);
                //默认音轨
                if (first.AudioId != null)
                {
                    prompt.Select(audios.First(a => a.GroupId == first.AudioId));
                }
            }
            if (subs.Any())
            {
                prompt.AddChoiceGroup(new StreamSpec() { Name = "__Subtitle" }, subs);
                //默认字幕轨
                if (first.SubtitleId != null)
                {
                    prompt.Select(subs.First(s => s.GroupId == first.SubtitleId));
                }
            }

            //如果此时还是没有选中任何流，自动选择一个
            prompt.Select(basicStreams.Concat(audios).Concat(subs).First());

            //多选
            var selectedStreams = AnsiConsole.Prompt(prompt);

            return selectedStreams;
        }
    }
}
