using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Misc;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Plugins.Bundle.Yasp.Config;
using osu.Game.Users.Drawables;
using osuTK;

namespace osu.Game.Rulesets.IGPlayer.Feature.Player.Plugins.Bundle.Yasp.Panels;

public partial class CoverIIPanel : CompositeDrawable, IPanel
{
    private WorkingBeatmap? currentBeatmap;

    public void Refresh(WorkingBeatmap? beatmap)
    {
        currentBeatmap = beatmap;

        var meta = beatmap?.Metadata ?? new BeatmapMetadata();
        titleText.Text = meta.GetTitleRomanisable();
        artistText.Text = meta.GetArtistRomanisable();

        sourceText.Text = string.IsNullOrEmpty(meta.Source)
            ? meta.GetTitleRomanisable()
            : meta.Source;

        cover?.Refresh(useUserAvatar.Value, beatmap);
        flowContainer?.FadeIn(300, Easing.OutQuint);
    }

    public override void Show()
    {
        flowContainer?.FadeIn(300, Easing.OutQuint).ScaleTo(1, 1500, Easing.OutBack);
        base.Show();
    }

    public override void Hide()
    {
        flowContainer?.FadeOut(300, Easing.OutQuint).ScaleTo(0.8f, 300, Easing.OutQuint);
        this.Delay(300).FadeOut();
    }

    private readonly BindableBool useUserAvatar = new BindableBool();

    private readonly TruncatingSpriteText titleText = new TruncatingSpriteText
    {
        Padding = new MarginPadding { Bottom = -5 },
        MaxWidth = 650,
        AllowMultiline = false,
        Font = OsuFont.GetFont(size: 60, weight: FontWeight.Bold, typeface: Typeface.TorusAlternate)
    };

    private readonly TruncatingSpriteText artistText = new TruncatingSpriteText
    {
        MaxWidth = 600,
        AllowMultiline = false,
        Font = OsuFont.GetFont(size: 36, weight: FontWeight.Medium),
        Padding = new MarginPadding { Bottom = 0 },
    };

    private readonly TruncatingSpriteText sourceText = new TruncatingSpriteText
    {
        MaxWidth = 600,
        AllowMultiline = false,
        Font = OsuFont.GetFont(size: 24, weight: FontWeight.Medium)
    };

    private AvatarOrBeatmapCover? cover;
    private FillFlowContainer? flowContainer;

    [BackgroundDependencyLoader]
    private void load(YaspPlugin plugin)
    {
        var config = (YaspConfigManager)Dependencies.Get<LLinPluginManager>().GetConfigManager(plugin);
        config.BindWith(YaspSettings.CoverIIUseUserAvatar, useUserAvatar);

        Anchor = Anchor.Centre;
        Origin = Anchor.Centre;
        AutoSizeAxes = Axes.Both;

        int squareLength = 168;
        flowContainer = new FillFlowContainer
        {
            Alpha = 0,
            Scale = new Vector2(0.8f),
            AutoSizeAxes = Axes.X,
            AutoSizeDuration = 300,
            AutoSizeEasing = Easing.OutQuint,
            Direction = FillDirection.Horizontal,

            X = 2,
            Height = squareLength,

            Spacing = new Vector2(25),

            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    Name = "Cover flow",
                    Direction = FillDirection.Horizontal,
                    RelativeSizeAxes = Axes.Y,
                    AutoSizeAxes = Axes.X,

                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Y,
                            Width = 7.5f
                        },
                        cover = new AvatarOrBeatmapCover
                        {
                            RelativeSizeAxes = Axes.Y,
                            Width = squareLength
                        },
                    }
                },
                new Container
                {
                    Name = "Line",
                    RelativeSizeAxes = Axes.Y,
                    Width = 2,

                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Height = 0.8f,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    }
                },
                new FillFlowContainer
                {
                    Name = "Detail Flow",
                    Direction = FillDirection.Vertical,
                    RelativeSizeAxes = Axes.Y,
                    AutoSizeAxes = Axes.X,

                    Margin = new MarginPadding { Top = 15 },

                    Children = new Drawable[]
                    {
                        titleText,
                        artistText,
                        sourceText
                    }
                }
            }
        };

        AddInternal(flowContainer);
        useUserAvatar.BindValueChanged(v =>
        {
            Refresh(currentBeatmap);
        });
    }

    private partial class AvatarOrBeatmapCover : CompositeDrawable
    {
        private WorkingBeatmap? workingBeatmap;
        private bool useUserAvatar;

        [Resolved(canBeNull: true)]
        private IAPIProvider? api { get; set; }

        [BackgroundDependencyLoader]
        private void load()
        {
            Masking = true;
        }

        public void Refresh(bool useUserAvatar, WorkingBeatmap? workingBeatmap)
        {
            Drawable child;

            if (useUserAvatar && this.useUserAvatar)
                return;

            bool useAvatarChanged = this.useUserAvatar != useUserAvatar;

            if (this.workingBeatmap == workingBeatmap && !useAvatarChanged)
                return;

            this.useUserAvatar = useUserAvatar;
            this.workingBeatmap = workingBeatmap;

            if (useUserAvatar)
            {
                child = new UpdateableAvatar(api?.LocalUser.Value ?? null);
            }
            else
            {
                child = new BeatmapCover(workingBeatmap)
                {
                    TimeBeforeWrapperLoad = 0,
                    UseBufferedBackground = false,
                    BackgroundBox = false
                };
            }

            child.RelativeSizeAxes = Axes.Both;
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Depth = float.MaxValue,
                    RelativeSizeAxes = Axes.Both,
                    Colour = ColourInfo.GradientVertical(Color4Extensions.FromHex("#555"), Color4Extensions.FromHex("#444")),
                },
                child
            };
        }
    }
}
