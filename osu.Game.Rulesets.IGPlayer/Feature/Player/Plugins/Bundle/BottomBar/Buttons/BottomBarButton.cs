#nullable disable

using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Interfaces.Plugins;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Screens.LLin;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.IGPlayer.Feature.Player.Plugins.Bundle.BottomBar.Buttons
{
    public partial class BottomBarButton : CompositeDrawable, IHasTooltip
    {
        [Resolved]
        private CustomColourProvider colourProvider { get; set; }

        protected CustomColourProvider ColourProvider => colourProvider;
        protected FillFlowContainer ContentFillFlow;

        public IconUsage Icon
        {
            get => SpriteIcon.Icon;
            set => SpriteIcon.Icon = value;
        }

        public LocalisableString Title
        {
            get => SpriteText.Text;
            set => SpriteText.Text = value;
        }

        protected Box BgBox;
        private Box flashBox;

        private IconUsage emptyIcon => new();

        [CanBeNull]
        private Container content;

        [CanBeNull]
        protected Container outerContent;

        public LocalisableString TooltipText { get; set; }

        protected readonly OsuSpriteText SpriteText = new OsuSpriteText
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre
        };

        protected SpriteIcon SpriteIcon = new SpriteIcon
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Size = new Vector2(13),
        };

        public new Axes AutoSizeAxes
        {
            get => base.AutoSizeAxes;
            set => base.AutoSizeAxes = value;
        }

        public readonly IFunctionProvider Provider;

        private float shearStrength;

        public float ShearStrength
        {
            get => shearStrength;
            set
            {
                shearStrength = value;
                this.Schedule(() =>
                {
                    if (outerContent != null)
                        outerContent.Shear = new Vector2(ShearStrength, 0f);

                    if (content != null)
                        content.Shear = new Vector2(-ShearStrength, 0f);
                });
            }
        }

        public BottomBarButton(IFunctionProvider provider = null)
        {
            Size = new Vector2(30);

            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            if (provider == null) return;

            Icon = provider.Icon;
            SpriteText.Text = provider.Title;
            SpriteIcon.Icon = provider.Icon;
            TooltipText = provider.Description;
            Size = provider.Size;
            Provider = provider;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = outerContent = new Container
            {
                Masking = true,
                Shear = new Vector2(ShearStrength, 0f),
                CornerRadius = 7,
                BorderThickness = 2,
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,

                Children = new Drawable[]
                {
                    BgBox = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ColourProvider.Background3
                    },
                    content = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Shear = new Vector2(-ShearStrength, 0f),
                        Masking = true,
                        Child = ContentFillFlow = new FillFlowContainer
                        {
                            Margin = new MarginPadding { Left = 15, Right = 15 },
                            AutoSizeAxes = Axes.X,
                            RelativeSizeAxes = Axes.Y,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Spacing = new Vector2(5),
                            Direction = FillDirection.Horizontal
                        }
                    },
                    flashBox = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                        Alpha = 0,
                    },
                    new HoverClickSounds()
                }
            };

            if (!Icon.Equals(emptyIcon))
                ContentFillFlow.Add(SpriteIcon);

            if (string.IsNullOrEmpty(Title.ToString()))
                ContentFillFlow.Add(SpriteText);

            // From OsuAnimatedButton
            if (AutoSizeAxes != Axes.None)
            {
                content.RelativeSizeAxes = (Axes.Both & ~AutoSizeAxes);
                content.AutoSizeAxes = AutoSizeAxes;
            }

            colourProvider.HueColour.BindValueChanged(_ =>
            {
                BgBox.Colour = ColourProvider.Background3;
                outerContent.BorderColour = ColourInfo.GradientVertical(colourProvider.Background3, colourProvider.Background1);
            }, true);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            var mouseSpace = ToLocalSpace(e.ScreenSpaceMousePosition);

            outerContent.ScaleTo(0.9f, 2000, Easing.OutQuint);
            return base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            outerContent.ScaleTo(1f, 1000, Easing.OutElastic);
            flashBox.FadeOut(1000, Easing.OutQuint);

            base.OnMouseUp(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            OnClickAnimation();
            Provider.Active();

            return true;
        }

        protected override bool OnHover(HoverEvent e)
        {
            flashBox.FadeTo(0.2f, 300);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);
            flashBox.FadeTo(0f, 300);
        }

        protected virtual void OnClickAnimation()
        {
            flashBox?.FadeTo(0.35f).Then().FadeTo(IsHovered ? 0.1f : 0f, 300);
        }

        public void DoFlash() => OnClickAnimation();
    }
}
