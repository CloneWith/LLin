#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Containers;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Graphics.SideBar.Settings.Sections;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Graphics.SideBar.Tabs;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Plugins;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Plugins.Config;
using osu.Game.Rulesets.IGPlayer.Helper.Configuration;
using osuTK;

namespace osu.Game.Rulesets.IGPlayer.Feature.Player.Graphics.SideBar.Settings
{
    public partial class PlayerSettings : OsuScrollContainer, ISidebarContent
    {
        private readonly FillFlowContainer<Section> fillFlow = new FillFlowContainer<Section>
        {
            AutoSizeAxes = Axes.Y,
            RelativeSizeAxes = Axes.X,
            Anchor = Anchor.TopRight,
            Origin = Anchor.TopRight,
            Spacing = new Vector2(5),
            Direction = FillDirection.Vertical
        };

        public string Title => "播放器设置";
        public IconUsage Icon { get; } = FontAwesome.Solid.Cog;

        [BackgroundDependencyLoader]
        private void load(MConfigManager config, LLinPluginManager pluginManager)
        {
            ScrollbarVisible = false;
            RelativeSizeAxes = Axes.Both;
            Add(fillFlow);

            foreach (var pl in pluginManager.GetAllPlugins(false))
            {
                if (pluginManager.GetSettingsFor(pl)?.Length > 0)
                    AddSection(new NewPluginSettingsSection(pl));
            }
        }

        private void onTabPositionChanged(ValueChangedEvent<TabControlPosition> v)
        {
            switch (v.NewValue)
            {
                case TabControlPosition.Left:
                    fillFlow.Anchor = fillFlow.Origin = Anchor.TopLeft;
                    break;

                case TabControlPosition.Right:
                    fillFlow.Anchor = fillFlow.Origin = Anchor.TopRight;
                    break;

                case TabControlPosition.Top:
                    fillFlow.Anchor = fillFlow.Origin = Anchor.TopCentre;
                    break;
            }
        }

        public void AddSection(Section section) => fillFlow.Add(section);
    }
}
