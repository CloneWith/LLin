using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Misc;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Plugins.Bundle.CloudMusic.Helper;

namespace osu.Game.Rulesets.IGPlayer.Feature.Player.Plugins.Bundle.CloudMusic.Misc
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class APISongInfo
    {
        public long ID { get; set; }

        public string? Name { get; set; }

        public List<APIArtistInfo> Artists { get; set; } = [];

        /// <summary>
        /// 获取网易云歌曲标题和搜索标题的相似度
        /// </summary>
        /// <returns>相似度百分比</returns>
        public float GetSimilarPercentage(WorkingBeatmap? beatmap)
        {
            string neteaseTitle = Name?.ToLowerInvariant() ?? string.Empty;
            string ourTitle = beatmap?.Metadata.GetTitle().ToLowerInvariant() ?? string.Empty;

            string source = neteaseTitle.Length > ourTitle.Length ? neteaseTitle : ourTitle;
            string target = neteaseTitle.Length > ourTitle.Length ? ourTitle : neteaseTitle;

            if (string.IsNullOrEmpty(neteaseTitle) || string.IsNullOrEmpty(ourTitle)) return 0;

            int distance = LevenshteinDistance.Compute(source, target);
            float percentage = 1 - (distance / (float)source.Length);

            return Math.Abs(percentage);
        }

        public string GetArtist() => string.Join(' ', Artists.Select(a => a.Name));
    }
}
