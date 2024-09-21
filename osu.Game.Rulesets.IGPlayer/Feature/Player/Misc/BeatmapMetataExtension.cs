// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Beatmaps;

namespace osu.Game.Rulesets.IGPlayer.Feature.Player.Misc
{
    public static class BeatmapMetataExtension
    {
        public static RomanisableString GetTitleRomanisable(this BeatmapMetadata metadata)
        {
            return new RomanisableString(metadata.TitleUnicode, metadata.Title);
        }

        public static RomanisableString GetArtistRomanisable(this BeatmapMetadata metadata)
        {
            return new RomanisableString(metadata.ArtistUnicode, metadata.Artist);
        }

        public static string GetTitle(this BeatmapMetadata metadata)
        {
            return string.IsNullOrEmpty(metadata.TitleUnicode)
                ? metadata.Title
                : metadata.TitleUnicode;
        }

        public static string GetArtist(this BeatmapMetadata metadata)
        {
            return string.IsNullOrEmpty(metadata.ArtistUnicode)
                ? metadata.Artist
                : metadata.ArtistUnicode;
        }
    }
}
