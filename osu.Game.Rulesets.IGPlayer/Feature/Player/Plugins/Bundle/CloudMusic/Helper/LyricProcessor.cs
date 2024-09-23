using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Threading;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Misc;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Plugins.Bundle.CloudMusic.Misc;
using Component = osu.Framework.Graphics.Component;

namespace osu.Game.Rulesets.IGPlayer.Feature.Player.Plugins.Bundle.CloudMusic.Helper
{
    public partial class LyricProcessor : Component
    {
        #region 获取状态

        public enum SearchState
        {
            [Description("未找到歌曲或信息不匹配")]
            Fail,

            [Description("搜索中")]
            Searching,

            [Description("模糊搜索中")]
            FuzzySearching,

            [Description("已就绪")]
            Success
        }

        public readonly Bindable<SearchState> State = new Bindable<SearchState>();

        private void setState(SearchState newState)
        {
            State.Value = newState;
        }

        #endregion

        #region 歌词获取

        private APISearchRequest? currentSearchRequest;
        private APILyricRequest? currentLyricRequest;

        private CancellationTokenSource cancellationTokenSource = null!;

        private UrlEncoder? encoder;

        /// <summary>
        /// 通过给定的<see cref="SearchOption"/>>搜索歌曲
        /// </summary>
        /// <param name="searchOption"><see cref="SearchOption"/>></param>
        public void Search(SearchOption searchOption)
        {
            if (State.Value != SearchState.FuzzySearching)
                setState(SearchState.Searching);

            var beatmap = searchOption.Beatmap;

            if (beatmap == null)
            {
                setState(SearchState.Fail);
                return;
            }

            var onFinish = searchOption.OnFinish;
            var onFail = searchOption.OnFail;

            if (!searchOption.NoLocalFile && searchOption.NoRetry)
            {
                var localLyrics = GetLocalLyrics(beatmap);

                if (localLyrics != null)
                {
                    setState(SearchState.Success);
                    onFinish?.Invoke(localLyrics);
                    return;
                }
            }

            //TODO: 实现新版网易云API的查询
            if (LyricPlugin.DisableCloudLookup)
            {
                setState(SearchState.Success);
                searchOption.OnFinish?.Invoke(new APILyricResponseRoot());
                return;
            }

            encoder ??= UrlEncoder.Default;

            //处理之前的请求
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();

            currentSearchRequest?.Dispose();
            currentLyricRequest?.Dispose();

            //处理要搜索的歌名: "标题 艺术家"
            string title = beatmap.Metadata.GetTitle();
            string artist = searchOption.NoArtist ? string.Empty : $" {beatmap.Metadata.GetArtist()}";
            string target = encoder.Encode($"{title} {artist}");

            var req = new APISearchRequest(target);

            req.Finished += () =>
            {
                var meta = RequestFinishMeta.From(req.ResponseObject, beatmap, onFinish, onFail, searchOption.TitleSimiliarThreshold);
                meta.NoRetry = searchOption.NoRetry;

                onSongSearchRequestFinish(meta, req);
            };

            req.Failed += e =>
            {
                if (currentSearchRequest == req)
                    setState(SearchState.Fail);

                string message = "[LyricProcessor] 查询歌曲失败";

                if (e is HttpRequestException)
                    message += ", 未能送达http请求, 请检查当前网络以及代理";

                Logging.LogError(e, message);
                onFail?.Invoke(e.ToString());
            };

            req.PerformAsync(cancellationTokenSource.Token).ConfigureAwait(false);

            currentSearchRequest = req;
        }

        public APILyricResponseRoot? GetLocalLyrics(WorkingBeatmap beatmap)
        {
            APILyricResponseRoot? deserializedObject = null;

            try
            {
                string path = storage.GetFullPath(lyricFilePath(beatmap), true);
                string content = File.ReadAllText(path);
                deserializedObject = JsonConvert.DeserializeObject<APILyricResponseRoot>(content);
            }
            catch
            {
                //忽略异常
            }

            return deserializedObject;
        }

        /// <summary>
        /// 通过给定的网易云音乐ID搜索歌曲
        /// </summary>
        /// <param name="id">歌曲ID</param>
        /// <param name="beatmap"></param>
        /// <param name="onFinish"></param>
        /// <param name="onFail"></param>
        public void SearchByNeteaseID(long id, WorkingBeatmap beatmap, Action<APILyricResponseRoot> onFinish, Action<string> onFail)
        {
            //处理之前的请求
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();

            var fakeResponse = new APISearchResponseRoot
            {
                Result = new APISearchResultInfo
                {
                    SongCount = 1,
                    Songs = new List<APISongInfo>
                    {
                        new APISongInfo
                        {
                            ID = id
                        }
                    }
                }
            };

            var meta = RequestFinishMeta.From(fakeResponse, beatmap, onFinish, onFail, 0);
            meta.NoRetry = true;

            onSongSearchRequestFinish(meta, null);
        }

        private static string lyricFilePath(WorkingBeatmap beatmap) => $"custom/lyrics/beatmap-{beatmap.BeatmapSetInfo.ID}.json";

        /// <summary>
        /// 当歌曲搜索请求完成后...
        /// </summary>
        /// <param name="meta"></param>
        /// <param name="searchRequest"></param>
        private void onSongSearchRequestFinish(RequestFinishMeta meta, APISearchRequest? searchRequest)
        {
            if (!meta.Success)
            {
                //如果没成功，尝试使用标题重搜
                if (meta.SourceBeatmap != null && !meta.NoRetry)
                {
                    var searchMeta = SearchOption.FromRequestFinishMeta(meta);
                    searchMeta.NoArtist = true;
                    searchMeta.NoRetry = true;
                    searchMeta.NoLocalFile = true;

                    if (searchRequest != null && searchRequest == currentSearchRequest)
                        setState(SearchState.FuzzySearching);

                    //Logging.Log("精准搜索失败, 将尝试只搜索标题...");
                    Search(searchMeta);
                }
                else
                {
                    if (searchRequest != null && searchRequest == currentSearchRequest)
                        setState(SearchState.Fail);

                    meta.OnFail?.Invoke("未搜索到对应歌曲!");
                }

                return;
            }

            float similiarPrecentage = meta.GetSimiliarPrecentage();

            Logging.Log($"Beatmap: '{meta.SourceBeatmap?.Metadata.GetTitle() ?? "???"}' <-> '{meta.GetNeteaseTitle()}' -> {similiarPrecentage} <-> {meta.TitleSimiliarThreshold}");

            if (similiarPrecentage >= meta.TitleSimiliarThreshold)
            {
                //标题匹配，发送歌词查询请求
                var req = new APILyricRequest(meta.SongID);
                req.Finished += () =>
                {
                    if (currentLyricRequest == req)
                        setState(SearchState.Success);

                    meta.OnFinish?.Invoke(req.ResponseObject);
                };
                req.Failed += e =>
                {
                    if (currentLyricRequest == req)
                        setState(SearchState.Fail);

                    Logging.LogError(e, "获取歌词失败");
                };
                req.PerformAsync(cancellationTokenSource.Token).ConfigureAwait(false);

                currentLyricRequest = req;
            }
            else
            {
                //Logging.Log("标题匹配失败, 将不会继续搜索歌词...");
                this.setState(SearchState.Fail);

                Logging.Log($"对 {meta.SourceBeatmap?.Metadata.GetTitle() ?? "未知谱面"} 的标题匹配失败：");
                Logging.Log($"Beatmap: '{meta.SourceBeatmap?.Metadata.GetTitle() ?? "???"}' <-> '{meta.GetNeteaseTitle()}' -> {similiarPrecentage} < {meta.TitleSimiliarThreshold}");

                meta.OnFail?.Invoke("标题匹配失败, 将不会继续搜索歌词...");
            }
        }

        #endregion

        #region 歌词读取、写入

        [Resolved]
        private Storage storage { get; set; } = null!;

        public void WriteLrcToFile(APILyricResponseRoot? responseRoot, WorkingBeatmap beatmap)
        {
            try
            {
                string serializedObject = JsonConvert.SerializeObject(responseRoot);
                File.WriteAllText(storage.GetFullPath(lyricFilePath(beatmap), true), serializedObject);
            }
            catch (Exception e)
            {
                Logging.LogError(e, "写入歌词时发生了错误");
            }
        }

        #endregion
    }
}
