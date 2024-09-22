// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.IGPlayer.Feature.Player.Plugins.Bundle.CloudMusic.Misc
{
    public static class StringExtensions
    {
        public static int ToMilliseconds(this string src)
        {
            string[] spilt = src.Split(":");
            int.TryParse(spilt[0], out int minutes);
            double.TryParse(spilt[1], out double seconds);

            return minutes * 60000 + (int)(seconds * 1000);
        }
    }
}
