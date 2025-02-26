﻿using N_m3u8DL_RE.Common.Log;
using N_m3u8DL_RE.Common.Resource;
using N_m3u8DL_RE.Enum;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Globalization;

namespace N_m3u8DL_RE.CommandLine
{

    internal class CommandInvoker
    {
        private readonly static Argument<string> Input = new(name: "input", description: ResString.cmd_Input);
        private readonly static Option<string?> TmpDir = new(new string[] { "--tmp-dir" }, description: ResString.cmd_tmpDir);
        private readonly static Option<string?> SaveDir = new(new string[] { "--save-dir", "-o" }, description: ResString.cmd_saveDir);
        private readonly static Option<string?> SaveName = new(new string[] { "--save-name", "-O" }, description: ResString.cmd_saveName);
        private readonly static Option<string?> SavePattern = new(new string[] { "--save-pattern" }, description: ResString.cmd_savePattern, getDefaultValue: () => "<SaveName>_<Id>_<Codecs>_<Language>_<Ext>");
        private readonly static Option<string?> UILanguage = new(new string[] { "--ui-language" }, description: ResString.cmd_uiLanguage);
        private readonly static Option<string[]?> Keys = new(new string[] { "--key" }, description: ResString.cmd_keys) { Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = false };
        private readonly static Option<string[]?> Headers = new(new string[] { "--header", "-H" }, description: ResString.cmd_header) { Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = false };
        private readonly static Option<LogLevel> LogLevel = new(name: "--log-level", description: ResString.cmd_logLevel, getDefaultValue: () => Common.Log.LogLevel.INFO);
        private readonly static Option<SubtitleFormat> SubtitleFormat = new(name: "--sub-format", description: ResString.cmd_subFormat, getDefaultValue: () => Enum.SubtitleFormat.VTT);
        private readonly static Option<bool> AutoSelect = new(new string[] { "--auto-select" }, description: ResString.cmd_autoSelect, getDefaultValue: () => false);
        private readonly static Option<bool> SubOnly = new(new string[] { "--sub-only" }, description: ResString.cmd_subOnly, getDefaultValue: () => false);
        private readonly static Option<int> ThreadCount = new(new string[] { "--thread-count" }, description: ResString.cmd_threadCount, getDefaultValue: () => 8);
        private readonly static Option<bool> SkipMerge = new(new string[] { "--skip-merge" }, description: ResString.cmd_skipMerge, getDefaultValue: () => false);
        private readonly static Option<bool> SkipDownload = new(new string[] { "--skip-download" }, description: ResString.cmd_skipDownload, getDefaultValue: () => false);
        private readonly static Option<bool> BinaryMerge = new(new string[] { "--binary-merge" }, description: ResString.cmd_binaryMerge, getDefaultValue: () => true);
        private readonly static Option<bool> DelAfterDone = new(new string[] { "--del-after-done" }, description: ResString.cmd_delAfterDone, getDefaultValue: () => true);
        private readonly static Option<bool> AutoSubtitleFix = new(new string[] { "--auto-subtitle-fix" }, description: ResString.cmd_subtitleFix, getDefaultValue: () => true);
        private readonly static Option<bool> CheckSegmentsCount = new(new string[] { "--check-segments-count" }, description: ResString.cmd_checkSegmentsCount, getDefaultValue: () => true);
        private readonly static Option<bool> WriteMetaJson = new(new string[] { "--write-meta-json" }, description: ResString.cmd_writeMetaJson, getDefaultValue: () => true);
        private readonly static Option<bool> AppendUrlParams = new(new string[] { "--append-url-params" }, description: ResString.cmd_appendUrlParams, getDefaultValue: () => false);

        class MyOptionBinder : BinderBase<MyOption>
        {
            protected override MyOption GetBoundValue(BindingContext bindingContext)
            {
                var option = new MyOption
                {
                    Input = bindingContext.ParseResult.GetValueForArgument(Input),
                    Headers = bindingContext.ParseResult.GetValueForOption(Headers),
                    LogLevel = bindingContext.ParseResult.GetValueForOption(LogLevel),
                    AutoSelect = bindingContext.ParseResult.GetValueForOption(AutoSelect),
                    SkipMerge = bindingContext.ParseResult.GetValueForOption(SkipMerge),
                    BinaryMerge = bindingContext.ParseResult.GetValueForOption(BinaryMerge),
                    DelAfterDone = bindingContext.ParseResult.GetValueForOption(DelAfterDone),
                    AutoSubtitleFix = bindingContext.ParseResult.GetValueForOption(AutoSubtitleFix),
                    CheckSegmentsCount = bindingContext.ParseResult.GetValueForOption(CheckSegmentsCount),
                    SubtitleFormat = bindingContext.ParseResult.GetValueForOption(SubtitleFormat),
                    SubOnly = bindingContext.ParseResult.GetValueForOption(SubOnly),
                    TmpDir = bindingContext.ParseResult.GetValueForOption(TmpDir),
                    SaveDir = bindingContext.ParseResult.GetValueForOption(SaveDir),
                    SaveName = bindingContext.ParseResult.GetValueForOption(SaveName),
                    ThreadCount = bindingContext.ParseResult.GetValueForOption(ThreadCount),
                    UILanguage = bindingContext.ParseResult.GetValueForOption(UILanguage),
                    SkipDownload = bindingContext.ParseResult.GetValueForOption(SkipDownload),
                    WriteMetaJson = bindingContext.ParseResult.GetValueForOption(WriteMetaJson),
                    AppendUrlParams = bindingContext.ParseResult.GetValueForOption(AppendUrlParams),
                    SavePattern = bindingContext.ParseResult.GetValueForOption(SavePattern),
                    Keys = bindingContext.ParseResult.GetValueForOption(Keys),
                };

                //在这里设置语言

                string loc = "en-US";
                string currLoc = Thread.CurrentThread.CurrentUICulture.Name;
                if (currLoc == "zh-CN" || currLoc == "zh-SG") loc = "zh-Hans";
                else if (currLoc.StartsWith("zh-")) loc = "zh-Hant";

                //以用户选择优先
                loc = option.UILanguage ?? loc;

                CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(loc);
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(loc);
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(loc);
                ResString.Culture = CultureInfo.GetCultureInfo(loc);

                return option;
            }
        }


        public static async Task<int> InvokeArgs(string[] args, Func<MyOption, Task> action)
        {
            var rootCommand = new RootCommand("N_m3u8DL-RE (Beta version) 20220721")
            {
                Input, TmpDir, SaveDir, SaveName, ThreadCount, AutoSelect, SkipMerge, SkipDownload, CheckSegmentsCount,
                BinaryMerge, DelAfterDone, WriteMetaJson, AppendUrlParams, Keys, Headers, /**SavePattern,**/ SubOnly, SubtitleFormat, AutoSubtitleFix,
                LogLevel, UILanguage
            };
            rootCommand.TreatUnmatchedTokensAsErrors = true;
            rootCommand.SetHandler(async (myOption) => await action(myOption), new MyOptionBinder());

            return await rootCommand.InvokeAsync(args);
        }
    }
}
