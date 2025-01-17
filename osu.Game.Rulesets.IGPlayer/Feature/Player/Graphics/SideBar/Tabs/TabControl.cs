#nullable disable

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Game.Graphics.Containers;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Screens.LLin;
using osu.Game.Rulesets.IGPlayer.Helper.Configuration;
using osuTK;

namespace osu.Game.Rulesets.IGPlayer.Feature.Player.Graphics.SideBar.Tabs
{
    internal partial class TabControl : CompositeDrawable
    {
        public FillFlowContainer<TabControlItem> Tabs;

        public float GetRightUnavaliableSpace() => anchorTarget?.Value == TabControlPosition.Right
            ? (Width + 5)
            : 0;

        public float GetLeftUnavaliableSpace() => anchorTarget?.Value == TabControlPosition.Left
            ? (Width + 5)
            : 0;

        public float GetTopUnavaliableSpace() => anchorTarget?.Value == TabControlPosition.Top
            ? (Height + 5)
            : 0;

        [Resolved]
        private CustomColourProvider colourProvider { get; set; }

        private Bindable<TabControlPosition> anchorTarget;

        public TabControl()
        {
            Name = "Header";
            Width = 50;
            RelativeSizeAxes = Axes.Y;

            Anchor = Anchor.CentreRight;
            Origin = Anchor.CentreRight;

            Tabs = new FillFlowContainer<TabControlItem>
            {
                Anchor = Anchor.CentreRight,
                Origin = Anchor.CentreRight,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(10)
            };

            InternalChild = scrollContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Alpha = 0,
                Child = verticalScroll = new OsuScrollContainer(Direction.Vertical)
                {
                    RelativeSizeAxes = Axes.Both,
                    ScrollContent =
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.None,
                        AutoSizeAxes = Axes.Both
                    },
                    ScrollbarVisible = false
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(MConfigManager config)
        {
            anchorTarget = new Bindable<TabControlPosition>();

            anchorTarget.BindValueChanged(onTabControlPosChanged, true);
        }

        private void onTabControlPosChanged(ValueChangedEvent<TabControlPosition> v)
        {
            toggleMode();

            Anchor = Anchor.CentreRight;
            Origin = Anchor.CentreRight;
            Tabs.Padding = new MarginPadding { Left = 5 };
        }

        private Vector2 targetHidePos = new(0);

        private void toggleMode()
        {
            RelativeSizeAxes = Axes.Y;
            Height = 1;
            Width = 75;

            targetHidePos = new Vector2(5, 0);

            Tabs.Margin = new MarginPadding { Vertical = 25 };
            Tabs.Direction = FillDirection.Vertical;

            Tabs.Anchor = Tabs.Origin = Anchor.CentreRight;

            if (!verticalScroll.Children.Contains(Tabs)) verticalScroll.Add(Tabs);

            verticalScroll.FadeIn();

            if (!IsVisible.Value) this.MoveTo(targetHidePos);
        }

        protected override void LoadComplete()
        {
            Hide();
            base.LoadComplete();
        }

        public bool SidebarActive
        {
            get => sidebarActive;
            set
            {
                //如果侧边栏关闭，并且光标不在tabHeader上，隐藏
                if (!IsHovered && value == false) Hide();

                sidebarActive = value;
            }
        }

        private bool sidebarActive;

        public Bindable<bool> IsVisible = new();
        private readonly OsuScrollContainer verticalScroll;
        private readonly Container scrollContainer;

        public override void Show()
        {
            IsVisible.Value = true;
            this.MoveTo(new Vector2(0), 300, Easing.OutQuint);
            scrollContainer.FadeIn(250, Easing.OutQuint);
        }

        public override void Hide()
        {
            if (IsHovered || SidebarActive) return;

            IsVisible.Value = false;

            this.MoveTo(targetHidePos, 300, Easing.OutQuint);
            scrollContainer.FadeOut(250, Easing.OutQuint);
        }

        protected override bool OnHover(HoverEvent e)
        {
            Show();

            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            InternalChild.FadeTo(0.99f, 500).OnComplete(_ => Hide());
            base.OnHoverLost(e);
        }
    }
}
