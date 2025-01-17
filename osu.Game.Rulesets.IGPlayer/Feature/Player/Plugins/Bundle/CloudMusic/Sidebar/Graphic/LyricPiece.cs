using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Interfaces;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Plugins.Bundle.CloudMusic.Config;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Plugins.Bundle.CloudMusic.Misc;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Screens.LLin;
using osu.Game.Rulesets.IGPlayer.Localisation.LLin.Plugins;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.IGPlayer.Feature.Player.Plugins.Bundle.CloudMusic.Sidebar.Graphic
{
    public partial class LyricPiece : DrawableLyric, IHasTooltip, IHasContextMenu
    {
        public LocalisableString TooltipText { get; private set; }

        [Resolved]
        private LyricConfigManager config { get; set; } = null!;

        [Resolved]
        private LyricPlugin plugin { get; set; } = null!;

        public MenuItem[] ContextMenuItems => new MenuItem[]
        {
            new OsuMenuItem(
                CloudMusicStrings.AdjustOffsetToLyric.ToString(),
                MenuItemType.Standard,
                () => plugin.Offset.Value = Value.Time - llinScreen.CurrentTrack.CurrentTime)
        };

        private Box hoverBox = null!;
        private OsuSpriteText contentText = null!;
        private OsuSpriteText translateText = null!;
        private OsuSpriteText timeText = null!;
        private readonly BindableDouble offset = new BindableDouble();

        public LyricPiece(Lyric lrc)
        {
            Value = lrc;
        }

        public LyricPiece()
        {
            Value = new Lyric
            {
                Content = "missingno"
            };
        }

        [Resolved]
        private IImplementLLin llinScreen { get; set; } = null!;

        [Resolved]
        private CustomColourProvider colourProvider { get; set; } = null!;

        private Box bgBox = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            CornerRadius = 5f;
            Masking = true;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChildren = new Drawable[]
            {
                bgBox = new Box
                {
                    Colour = colourProvider.Highlight1,
                    RelativeSizeAxes = Axes.Both,
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Spacing = new Vector2(5),
                    Direction = FillDirection.Horizontal,
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            Name = "时间显示",
                            Masking = true,
                            CornerRadius = 5,
                            AutoSizeAxes = Axes.Y,
                            Width = 80,
                            Margin = new MarginPadding { Vertical = 5, Left = 5, Right = -1 },
                            Children = new Drawable[]
                            {
                                timeText = new OsuSpriteText
                                {
                                    Font = OsuFont.GetFont(size: 17, weight: FontWeight.Bold),
                                    Margin = new MarginPadding { Horizontal = 5, Vertical = 3 },
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre
                                }
                            }
                        },
                        new Circle
                        {
                            Name = "分隔",
                            Height = 3,
                            Colour = Color4.Gray.Opacity(0.6f),
                            Width = 20,
                            Margin = new MarginPadding { Top = 16 }
                        },
                        new Container
                        {
                            Name = "歌词内容",
                            Width = 380,
                            AutoSizeAxes = Axes.Y,
                            Margin = new MarginPadding { Top = 6 },
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Height = 18,
                                    Colour = Color4.White.Opacity(0)
                                },
                                textFillFlow = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Children = new Drawable[]
                                    {
                                        contentText = new TruncatingSpriteText
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Margin = new MarginPadding { Left = 5, Bottom = 5 }
                                        },
                                        translateText = new TruncatingSpriteText
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Margin = new MarginPadding { Left = 5, Bottom = 5 }
                                        },
                                    }
                                },
                            }
                        },
                    }
                },
                hoverBox = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White.Opacity(0.2f),
                    Alpha = 0
                },
                new HoverClickSounds()
            };

            colourProvider.HueColour.BindValueChanged(_ =>
            {
                bgBox.Colour = colourProvider.Highlight1.Opacity(isCurrent ? 1 : 0);
            }, true);
            offset.BindValueChanged(_ => Schedule(() => UpdateValue(Value)), true);
        }

        private bool isCurrentReal;

        private bool isCurrent
        {
            get => isCurrentReal;
            set
            {
                bgBox.FadeColour(colourProvider.Highlight1.Opacity(value ? 1 : 0), 300, Easing.OutQuint);
                textFillFlow.FadeColour(value ? Color4.Black : Color4.White, 300, Easing.OutQuint);
                timeText.FadeColour(value ? Color4.Black : Color4.White, 300, Easing.OutQuint);

                isCurrentReal = value;
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            offset.BindTo(plugin.Offset);
        }

        protected override void Update()
        {
            isCurrent = plugin.CurrentLine != null && plugin.CurrentLine.Equals(Value);

            base.Update();
        }

        private bool haveLyric;
        private FillFlowContainer textFillFlow = null!;

        protected override void UpdateValue(Lyric lyric)
        {
            contentText.Text = lyric.Content;
            translateText.Text = lyric.TranslatedString;

            var timeSpan = TimeSpan.FromMilliseconds(Math.Max(lyric.Time - offset.Value, 0));
            timeText.Text = $"{timeSpan:mm\\:ss\\.fff}";
            TooltipText = $"{timeText.Text}"
                          + (string.IsNullOrEmpty(lyric.Content)
                              ? string.Empty
                              : $"－ {lyric.Content}")
                          + (string.IsNullOrEmpty(lyric.TranslatedString)
                              ? string.Empty
                              : $"－ {lyric.TranslatedString}");

            haveLyric = !string.IsNullOrWhiteSpace(lyric.Content);

            Colour = haveLyric
                ? Color4.White
                : Color4Extensions.FromHex(@"555");
        }

        protected override bool OnClick(ClickEvent e)
        {
            llinScreen.SeekTo(Value.Time + 1 - offset.Value);
            return base.OnClick(e);
        }

        protected override bool OnHover(HoverEvent e)
        {
            hoverBox.FadeIn(300);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            hoverBox.FadeOut(300);
            base.OnHoverLost(e);
        }

        //时间显示的高度(23) + 2 * (文本高度 + 文本Margin(5))
        public override int FinalHeight()
        {
            int val = 23; //时间显示大小

            val += 10; //时间显示Margin

            if (!string.IsNullOrEmpty(Value.Content) && !string.IsNullOrEmpty(Value.TranslatedString))
            {
                val += (int)(string.IsNullOrEmpty(Value.TranslatedString)
                    ? (string.IsNullOrEmpty(Value.Content)
                        ? 0
                        : (translateText?.Height ?? 18 + 5))
                    : (contentText?.Height ?? 18 + 5));
            }

            val += 5; //向下Margin

            return val;
        }
    }
}
