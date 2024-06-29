using System;
using System.Linq;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Framework.Screens;
using osu.Game.Graphics;
using osu.Game.Rulesets.IGPlayer.Feature.Player.Screens.LLin;
using osu.Game.Screens;
using osu.Game.Screens.Menu;
using osu.Game.Screens.Select;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Rulesets.IGPlayer.Helper.Injectors;

public partial class GameScreenInjector : AbstractInjector
{
    private OsuScreenStack? screenStack;

    private readonly object injectLock = new object();

    [Resolved]
    private OsuGame game { get; set; } = null!;

    [Resolved(canBeNull: true)]
    private IBindable<RulesetInfo>? ruleset { get; set; }

    [BackgroundDependencyLoader]
    private void load()
    {
        hookScreenStack();

        if (ruleset is Bindable<RulesetInfo> rs)
        {
            //避免用户切换到此ruleset
            ruleset?.BindValueChanged(v =>
            {
                if (v.NewValue.ShortName == "igplayerruleset" && v.OldValue != null && v.OldValue.ShortName != "igplayerruleset")
                    rs.Value = v.OldValue;
            });
        }
    }

    private bool hookScreenStack()
    {
        lock (injectLock)
        {
            var screenStackField = this.FindFieldInstance(game, typeof(OsuScreenStack));

            if (screenStackField == null) return false;

            object? val = screenStackField.GetValue(game);

            if (val is not OsuScreenStack osuScreenStack) return false;

            screenStack = osuScreenStack;

            screenStack.ScreenExited += onScreenSwitch;
            screenStack.ScreenPushed += onScreenSwitch;

            return true;
        }
    }

    private void onScreenSwitch(IScreen lastscreen, IScreen newscreen)
    {
        Logging.Log($"🦢 Screen Changed! {lastscreen} -> {newscreen}", level: LogLevel.Debug);

        if (lastscreen == currentPlaySongSelect && newscreen is MainMenu)
            currentPlaySongSelect = null;

        switch (newscreen)
        {
            case MainMenu menu when !menuEntryInjected:
                findButtonSystem(menu);
                menuEntryInjected = true;
                break;

            case PlaySongSelect playSongSelect:
                this.Schedule(() => findFooter(playSongSelect));
                break;
        }
    }

    private void pushPlayerScreen(MainMenuButton? button)
    {
        game.PerformFromScreen(s => s.Push(new LLinScreen()), new[]
        {
            typeof(MainMenu),
            typeof(PlaySongSelect)
        });
    }

    #region MainMenu -> ButtonSystem

    private void findButtonSystem(MainMenu menu)
    {
        try
        {
            const BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;

            if (menu.GetType().GetFields(flag)
                    .FirstOrDefault(f => f.FieldType == typeof(ButtonSystem))?.GetValue(menu) is not ButtonSystem buttonSystem)
            {
                Logging.Log("无法向主界面添加入口, 因为没有找到ButtonSystem", level: LogLevel.Important);
                return;
            }

            // Find Multiplayer button
            var target = this.findChildInContainer(buttonSystem, d => d is MainMenuButton mainMenuButton && mainMenuButton.TriggerKeys.Contains(Key.M));

            if (target == null)
            {
                Logging.Log("无法向主界面添加入口, 因为没有找到游玩按钮", level: LogLevel.Important);
                return;
            }

            var targetParent = target.Parent as FlowContainerWithOrigin;
            Logging.Log($"Parent is {target.Parent}");
            targetParent!.Add(new MainMenuButton("Hikariii播放器", "button-generic-select", OsuIcon.Play, new Color4(0, 86, 73, 255), pushPlayerScreen)
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                VisibleState = ButtonSystemState.Play
            });
        }
        catch (Exception e)
        {
            Logging.LogError(e, "向主界面添加入口时出现问题");
        }
    }

    private bool menuEntryInjected;

    private Drawable? findChildInContainer(Container container, Func<Drawable, bool> func)
    {
        foreach (var containerChild in container.Children)
        {
            if (containerChild == null) continue;

            if (func.Invoke(containerChild)) return containerChild;

            if (containerChild is not Container childContainer) continue;

            var childVal = findChildInContainer(childContainer, func);
            if (childVal != null) return childVal;
        }

        return null;
    }

    #endregion

    #region PlaySongSelect -> Footer

    private PlaySongSelect? currentPlaySongSelect;

    private void findFooter(PlaySongSelect playSongSelect)
    {
        if (playSongSelect == currentPlaySongSelect) return;

        try
        {
            if (screenStack!.CurrentScreen != playSongSelect) return;

            const BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.SetProperty
                                      | BindingFlags.SetField;

            if (playSongSelect.GetType().GetProperties(flag)
                              .FirstOrDefault(f => f.PropertyType == typeof(Footer))?.GetValue(playSongSelect) is not Footer footer)
            {
                Logging.Log("没有找到Footer", level: LogLevel.Important);
                return;
            }

            footer.AddButton(new FooterButtonOpenInMvis
            {
                Action = () => this.pushPlayerScreen(null)
            }, null);

            currentPlaySongSelect = playSongSelect;
        }
        catch (Exception e)
        {
            Logging.LogError(e, "向歌曲选择添加入口时出现问题");
        }
    }

    private partial class FooterButtonOpenInMvis : FooterButton
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Alpha = 0;
            SelectedColour = new Color4(0, 86, 73, 255);
            DeselectedColour = SelectedColour.Opacity(0.5f);
            Text = @"在Hikariii中打开";
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            ButtonContentContainer.Margin = new MarginPadding { Horizontal = (100 - TextContainer.Width) / 2 };
        }
    }
}

#endregion
