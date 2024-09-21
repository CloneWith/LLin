// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;

namespace osu.Game.Rulesets.IGPlayer.Feature.Player.Plugins.Bundle.CloudMusic.Misc
{
    public static class StringExtensions
    {
        public static int ToMilliseconds(this string src)
        {
            int result;

            try
            {
                var timeSpan = TimeSpan.ParseExact($"{src}", @"mm\.ss\.fff", new DateTimeFormatInfo());
                result = (int)timeSpan.TotalMilliseconds;
            }
            catch (Exception e)
            {
                string reason = e.Message;

                if (e is FormatException)
                    reason = "格式有误, 请检查原歌词是否正确";

                Logging.LogError(e, $"无法将\"{src}\"转换为歌词时间: {reason}");
                result = int.MaxValue;
            }

            return result;
        }
    }
}
