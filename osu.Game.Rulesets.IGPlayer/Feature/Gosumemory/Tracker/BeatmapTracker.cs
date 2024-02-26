using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.IGPlayer.Feature.Gosumemory.Tracker;

public partial class BeatmapTracker : AbstractTracker
{
    public BeatmapTracker(TrackerHub hub)
        : base(hub)
    {
    }

    private readonly Bindable<WorkingBeatmap> beatmap = new Bindable<WorkingBeatmap>();
    private readonly IBindable<IReadOnlyList<Mod>> mods = new Bindable<IReadOnlyList<Mod>>();

    [Resolved]
    private IBindable<IReadOnlyList<Mod>> globalMods { get; set; } = null!;

    [Resolved]
    private BeatmapDifficultyCache beatmapDifficultyCache { get; set; } = null!;

    private GosuRealmDirectAccessor? directAccessor;

    [BackgroundDependencyLoader]
    private void load(Bindable<WorkingBeatmap> globalBeatmap)
    {
        this.beatmap.BindTo(globalBeatmap);
        this.mods.BindTo(globalMods);

        directAccessor = new GosuRealmDirectAccessor(realmAccess);
        AddInternal(directAccessor);

        try
        {
            var staticRoot = this.staticRoot();

            if (Path.Exists(staticRoot))
                Directory.Delete(staticRoot, true);
        }
        catch (Exception e)
        {
            Logging.Log("Error occurred while clearing cache directory, but it's not a big deal.");
        }
    }

    private string filesRoot()
    {
        return storage.GetFullPath("files");
    }

    private string staticRoot()
    {
        string? path = storage.GetFullPath("gosu_statics", true);

        try
        {
            if (!Path.Exists(path))
                Directory.CreateDirectory(path);
        }
        catch (Exception e)
        {
            Logging.LogError(e, "Unable to create statics directory");
        }

        return path;
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        this.beatmap.BindValueChanged(e =>
        {
            this.onBeatmapChanged(e.NewValue);
            ((Bindable<IReadOnlyList<Mod>>)mods).TriggerChange();
        }, true);

        this.mods.BindValueChanged(e =>
        {
            this.onModsChanged(e.NewValue);
        }, true);
    }

    [Resolved]
    private Bindable<RulesetInfo> globalRuleset { get; set; } = null!;

    private void onModsChanged(IReadOnlyList<Mod> mods)
    {
        var currentWorking = beatmap.Value;
        var difficulty = new BeatmapDifficulty(currentWorking.BeatmapInfo.Difficulty);
        double timeRate = 1;

        foreach (var mod in mods)
        {
            switch (mod)
            {
                case IApplicableToDifficulty applicableToDifficulty:
                    applicableToDifficulty.ApplyToDifficulty(difficulty);
                    break;

                case IApplicableToRate applicableToRate:
                    timeRate = applicableToRate.ApplyToRate(0, timeRate);
                    break;
            }
        }

        Ruleset ruleset = currentWorking.BeatmapInfo.Ruleset.Available
            ? currentWorking.BeatmapInfo.Ruleset.CreateInstance()
            : globalRuleset.Value.CreateInstance();

        BeatmapDifficulty adjusted = difficulty;

        try
        {
            adjusted = ruleset.GetRateAdjustedDisplayDifficulty(difficulty, timeRate);
        }
        catch (Exception)
        {
            Logging.Log("Can't get adjusted difficulty, using original one...");
        }

        var dataRoot = Hub.GetDataRoot();

        dataRoot.MenuValues.GosuBeatmapInfo.Stats.BPM.Max = (int)Math.Round(beatmap.Value.Beatmap.ControlPointInfo.BPMMaximum * timeRate);
        dataRoot.MenuValues.GosuBeatmapInfo.Stats.BPM.Min = (int)Math.Round(beatmap.Value.Beatmap.ControlPointInfo.BPMMinimum * timeRate);

        dataRoot.MenuValues.GosuBeatmapInfo.Stats.AR = adjusted.ApproachRate;
        dataRoot.MenuValues.GosuBeatmapInfo.Stats.CS = adjusted.CircleSize;
        dataRoot.MenuValues.GosuBeatmapInfo.Stats.HP = adjusted.DrainRate;
        dataRoot.MenuValues.GosuBeatmapInfo.Stats.OD = adjusted.OverallDifficulty;
    }

    private IBindable<StarDifficulty?>? starDifficulty;

    private CancellationTokenSource cancellationTokenSource;

    private void onBeatmapChanged(WorkingBeatmap newBeatmap)
    {
        Hub.GetDataRoot().UpdateMetadata(newBeatmap);

        //Logging.Log($"~BACKGROUND IS {newBeatmap.Metadata.BackgroundFile}");
        updateFileSupporters(newBeatmap.BeatmapSetInfo, newBeatmap);

        this.onModsChanged(this.mods.Value);

        // Beatmap star difficulty
        cancellationTokenSource?.Cancel();
        cancellationTokenSource = new CancellationTokenSource();

        this.starDifficulty?.UnbindAll();
        this.starDifficulty = null;
        this.starDifficulty = beatmapDifficultyCache.GetBindableDifficulty(newBeatmap.BeatmapInfo, cancellationTokenSource.Token);
        this.starDifficulty.BindValueChanged(e =>
        {
            double newVal = e.NewValue?.Stars ?? 0d;

            var dataRoot = Hub.GetDataRoot();
            dataRoot.MenuValues.GosuBeatmapInfo.Stats.SR = (float)newVal;
            dataRoot.MenuValues.GosuBeatmapInfo.Stats.MaxCombo = e.NewValue?.MaxCombo ?? 0;
        }, true);
    }

    [Resolved]
    private RealmAccess realmAccess { get; set; } = null!;

    [Resolved]
    private Storage storage { get; set; } = null!;

    private CancellationTokenSource fileExportCancellationTokenSource;

    private void updateFileSupporters(BeatmapSetInfo setInfo, WorkingBeatmap beatmap)
    {
        if (directAccessor == null)
            return;

        string root = staticRoot();

        // Cancel previous update process
        fileExportCancellationTokenSource?.Cancel();
        fileExportCancellationTokenSource = new CancellationTokenSource();

        Task.Run(async () =>
        {
            await Task.Run(() => ensureCacheNotTooMany(root)).ConfigureAwait(false);

            // Background
            string backgroundExt = "";
            string[] rawNameSplit = beatmap.Metadata.BackgroundFile?.Split('.') ?? new string[]{};
            backgroundExt = rawNameSplit.Length >= 2 ? rawNameSplit[^1] : "";

            string backgroundDesti = "_default.png";
            if (beatmap.Metadata.BackgroundFile != null) //Yes, this can be null
                backgroundDesti = $"{root}/{beatmap.BeatmapSetInfo.OnlineID}_{beatmap.Metadata.BackgroundFile.GetHashCode()}.{backgroundExt}";

            string? backgroundFinal = await directAccessor.ExportSingleTask(
                setInfo,
                beatmap.Metadata.BackgroundFile ?? "",
                backgroundDesti).ConfigureAwait(false);

            // .osu File
            string osuFileDesti = "_default.osz";
            if (beatmap.BeatmapInfo.File?.Filename != null)
                osuFileDesti = $"{root}/{beatmap.BeatmapSetInfo.OnlineID}_{beatmap.BeatmapInfo.File.GetHashCode()}.osu";

            string? osuFileFinal = await directAccessor.ExportSingleTask(
                setInfo,
                beatmap.BeatmapInfo.File?.Filename ?? "",
                osuFileDesti).ConfigureAwait(false);

            // Audio file
            string audioFileDesti = "_default.mp3";

            if (beatmap.BeatmapInfo.BeatmapSet?.Metadata.AudioFile != null)
            {
                var audioNameSpilt = beatmap.BeatmapInfo.BeatmapSet.Metadata.AudioFile.Split(".");
                string audioExtName = audioNameSpilt.Length >= 2 ? audioNameSpilt[^1] : "audio";
                audioFileDesti = $"{root}/{beatmap.BeatmapSetInfo.OnlineID}_{beatmap.BeatmapInfo.BeatmapSet.Metadata.AudioFile.GetHashCode()}.{audioExtName}";
            }

            string? audioFinal = await directAccessor.ExportSingleTask(
                setInfo,
                beatmap.BeatmapInfo.BeatmapSet?.Metadata.AudioFile ?? "",
                audioFileDesti).ConfigureAwait(false);

            // Await for statics refresh
            await Task.Run(updateStatics).ConfigureAwait(false);

            // Update!
            this.Schedule(() =>
            {
                //Logging.Log("~~~PUSH TO GOSU!");
                var dataRoot = Hub.GetDataRoot();

                if (backgroundFinal != null)
                {
                    string boardcast = backgroundFinal.Replace(root, "").Replace("/", "");
                    //Logging.Log("~~~BOARDCAST IS " + boardcast);
                    dataRoot.MenuValues.GosuBeatmapInfo.Path.BackgroundPath = boardcast;
                    dataRoot.MenuValues.GosuBeatmapInfo.Path.BgPath = boardcast;
                }

                if (osuFileFinal != null)
                    dataRoot.MenuValues.GosuBeatmapInfo.Path.BeatmapFile = osuFileFinal.Replace(root, "").Replace("/", "");

                if (audioFinal != null)
                    dataRoot.MenuValues.GosuBeatmapInfo.Path.AudioPath = audioFinal.Replace(root, "").Replace("/", "");
            });
        }, fileExportCancellationTokenSource.Token);
    }

    private void ensureCacheNotTooMany(string cachePath)
    {
        if (!Path.Exists(cachePath)) return;

        int fileCount = Directory.GetFiles(cachePath, "*", SearchOption.TopDirectoryOnly).Length;

        // 30 beatmaps
        if (fileCount <= 90) return;

        try
        {
            Directory.Delete(cachePath, true);
            Directory.CreateDirectory(cachePath);
        }
        catch (Exception e)
        {
            Logging.Log("Error occurred while clearing gosu cache... Not a big deal, maybe?");
        }
    }

    private void updateStatics()
    {
        try
        {
            var server = Hub.GetWsLoader()?.Server;
            server?.RemoveStaticContent(staticRoot());
            server?.AddStaticContent(staticRoot(), "/Songs");
        }
        catch (Exception e)
        {
            Logging.LogError(e, "Unable to add cache");
        }
    }
}
