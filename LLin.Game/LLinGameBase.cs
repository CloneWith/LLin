using System;
using System.Collections.Generic;
using LLin.Game.Online;
using LLin.Game.Screens;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.IO.Stores;
using osuTK;
using LLin.Resources;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.IO;
using osu.Game.Online.API;
using osu.Game.Overlays;
using osu.Game.Resources;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Utils;
using MConfigManager = LLin.Game.Configuration.MConfigManager;

namespace LLin.Game
{
    public class LLinGameBase : osu.Framework.Game
    {
        // Anything in this class is shared between the test browser and the game implementation.
        // It allows for caching global dependencies that should be accessible to tests, or changing
        // the screen scaling for all components including the test browser and framework overlays.

        protected override Container<Drawable> Content { get; }

        protected LLinGameBase()
        {
            // Ensure game and tests scale with window size and screen DPI.
            base.Content.Add(Content = new DrawSizePreservingFillContainer
            {
                // You may want to change TargetDrawSize to your "default" resolution, which will decide how things scale and position when using absolute coordinates.
                TargetDrawSize = new Vector2(1366, 768)
            });
        }

        private DependencyContainer dependencies;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) =>
            dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        private WorkingBeatmap defaultBeatmap;
        private DatabaseContextFactory contextFactory;

        protected APIAccess APIAccess { get; set; }
        protected OsuConfigManager OsuConfig { get; set; }
        protected RulesetStore OsuRulesetStore { get; set; }
        protected MusicController OsuMusicController { get; set; }

        [Cached]
        [Cached(typeof(IBindable<RulesetInfo>))]
        protected readonly Bindable<RulesetInfo> Ruleset = new Bindable<RulesetInfo>();

        [Cached]
        [Cached(typeof(IBindable<IReadOnlyList<Mod>>))]
        protected readonly Bindable<IReadOnlyList<Mod>> SelectedMods = new Bindable<IReadOnlyList<Mod>>(Array.Empty<Mod>());

        [BackgroundDependencyLoader]
        private void load()
        {
            Resources.AddStore(new DllResourceStore(OsuResources.ResourceAssembly));

            dependencies.CacheAs(new MConfigManager(Storage));
            dependencies.CacheAs(Storage);

            dependencies.CacheAs(new LargeTextureStore(Host.CreateTextureLoaderStore(new NamespacedResourceStore<byte[]>(Resources, @"Textures"))));

            //osu.Game兼容
            defaultBeatmap = new DummyWorkingBeatmap(Audio, null);

            dependencies.Cache(OsuConfig = new OsuConfigManager(Storage));

            dependencies.Cache(new SessionStatics());

            dependencies.Cache(new OsuColour());

            Resources.AddStore(new DllResourceStore(typeof(LLinResources).Assembly));

            dependencies.CacheAs(APIAccess ??= new APIAccess(OsuConfig, new VoidApiEndpointConfiguration(), string.Empty));

            dependencies.Cache(contextFactory = new DatabaseContextFactory(Storage));

            dependencies.Cache(OsuRulesetStore = new RulesetStore(contextFactory, Storage)); //OsuScreen
            dependencies.Cache(new FileStore(contextFactory, Storage)); //由Storyboard使用

            dependencies.Cache(BeatmapManager = new BeatmapManager(Storage,
                contextFactory,
                OsuRulesetStore,
                APIAccess,
                Audio,
                Resources,
                Host,
                defaultBeatmap, true)); //osu.Game祖宗级依赖（

            var beatmap = new NonNullableBindable<WorkingBeatmap>(defaultBeatmap);

            dependencies.CacheAs<IBindable<WorkingBeatmap>>(beatmap);
            dependencies.CacheAs<Bindable<WorkingBeatmap>>(beatmap);

            //依赖BeatmapManager和IBindable<WorkingBeatmap>，放在最后
            AddInternal(OsuMusicController = new MusicController());
            dependencies.CacheAs(OsuMusicController);

            PreviewTrackManager osuPreviewTrackManager;
            dependencies.Cache(osuPreviewTrackManager = new PreviewTrackManager());
            Add(osuPreviewTrackManager);

            //自定义字体
            dependencies.Cache(new CustomFontHelper());

            dependencies.Cache(new CustomStore(Storage, this));

            //字体
            AddFont(Resources, @"Fonts/osuFont");

            AddFont(Resources, @"Fonts/Torus/Torus-Regular");
            AddFont(Resources, @"Fonts/Torus/Torus-Light");
            AddFont(Resources, @"Fonts/Torus/Torus-SemiBold");
            AddFont(Resources, @"Fonts/Torus/Torus-Bold");

            AddFont(Resources, @"Fonts/Noto/Noto-Basic");
            AddFont(Resources, @"Fonts/Noto/Noto-Hangul");
            AddFont(Resources, @"Fonts/Noto/Noto-CJK-Basic");
            AddFont(Resources, @"Fonts/Noto/Noto-CJK-Compatibility");
            AddFont(Resources, @"Fonts/Noto/Noto-Thai");
        }

        protected Storage Storage { get; set; }
        protected BeatmapManager BeatmapManager { get; set; }

        public override void SetHost(GameHost host)
        {
            Storage = host.Storage;

            base.SetHost(host);
        }
    }
}
