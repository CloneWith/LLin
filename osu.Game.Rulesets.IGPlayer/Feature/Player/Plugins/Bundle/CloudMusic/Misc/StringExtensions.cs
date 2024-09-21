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
            string[] spilt = src.Split(".");

            if (spilt.Length < 3)
            {
                // ???
                Logging.Log($"给定的时间不正确：{src}");
                return int.MaxValue;
            }

            string formatString = "";

            for (int i = 0; i < spilt[0].Length; i++)
                formatString += "m";

            formatString += @"\.";

            for (int i = 0; i < spilt[1].Length; i++)
                formatString += "s";

            formatString += @"\.";

            for (int i = 0; i < spilt[2].Length; i++)
                formatString += "f";

            try
            {
                var timeSpan = TimeSpan.ParseExact($"{src}", formatString, new DateTimeFormatInfo());
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
