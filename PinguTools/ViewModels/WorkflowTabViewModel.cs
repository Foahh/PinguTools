using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PinguTools.Audio;
using PinguTools.Chart;
using PinguTools.Common;
using PinguTools.Common.Localization;
using PinguTools.Image;
using PinguTools.Localization;
using PinguTools.Services;
using System.IO;
using System.Media;

namespace PinguTools.ViewModels;

public class WorkflowTabViewModel : ChartTabViewModel
{
    private readonly AssetService assetService;
    private readonly ChartConverter chartConverter;
    private readonly JacketConverter jacketConverter;
    private readonly MusicConverter musicConverter;
    private readonly StageConverter stageConverter;

    public WorkflowTabViewModel(ChartConverter chartConverter, MusicConverter musicConverter, JacketConverter jacketConverter, StageConverter stageConverter, AssetService ass, ActionService acs) : base(chartConverter, acs)
    {
        this.chartConverter = chartConverter;
        this.musicConverter = musicConverter;
        this.jacketConverter = jacketConverter;
        this.stageConverter = stageConverter;
        assetService = ass;
        ActionCommand = new AsyncRelayCommand(Convert, acs.CanRun);
    }

    private async Task Convert()
    {
        if (string.IsNullOrEmpty(ChartPath)) throw new OperationCanceledException(CommonStrings.Error_input_path_is_not_set);
        var meta = Model?.ToMeta();
        if (meta is null) throw new OperationCanceledException(Strings.Error_meta_is_not_set);
        var songId = meta.Id ?? throw new OperationCanceledException(CommonStrings.Error_song_id_is_not_set);
        if (string.IsNullOrEmpty(meta.BgmFileName)) throw new OperationCanceledException(CommonStrings.Error_audio_file_is_not_set);
        if (string.IsNullOrEmpty(meta.JacketFileName)) throw new OperationCanceledException(CommonStrings.Error_jacket_file_is_not_set);
        if (meta.IsCustomBg)
        {
            if (string.IsNullOrEmpty(meta.BgFileName)) throw new OperationCanceledException(CommonStrings.Error_background_file_is_not_set);
            if (meta.StageId is null) throw new OperationCanceledException(CommonStrings.Error_stage_id_is_not_set);
        }

        var dlg = new OpenFolderDialog
        {
            InitialDirectory = Path.GetDirectoryName((string?)ChartPath),
            Title = Strings.Title_select_the_output_folder,
            Multiselect = false
        };
        if (dlg.ShowDialog() != true) return;
        var path = dlg.FolderName;

        await ActionService.RunAsync(async (diag, p, ct) =>
        {
            if (meta.IsCustomBg)
            {
                var stageOpts = new StageConverter.Context(meta.BgFileName, [], meta.StageId, path, meta.NotesFieldLine, assetService.StageNames);
                await stageConverter.ConvertAsync(stageOpts, diag, p, ct);
                if (diag.HasErrors) return;
                ct.ThrowIfCancellationRequested();
                meta.Stage = stageOpts.Result;
            }

            var xml = ChartXmlBuilder.BuildMusicXml(meta);
            var musicData = xml.Descendants("MusicData").First();
            var workdirName = musicData.Descendants("dataName").First().Value;
            var jacketName = musicData.Descendants("jaketFile").First().Value;
            var fumens = musicData.Descendants("fumens").First().Elements("MusicFumenData");
            var fumen = fumens.FirstOrDefault(f =>
            {
                var id = f.Descendants("type").First().Element("id")?.Value;
                if (string.IsNullOrEmpty(id)) return false;
                if (int.TryParse(id, out var result)) return result == (int)meta.Difficulty;
                return false;
            });
            var chartName = fumen?.Element("file")?.Value ?? throw new OperationCanceledException(CommonStrings.Error_chart_file_is_not_set);

            if (meta.Difficulty == Difficulty.WorldsEnd)
            {
                var eventXml = ChartXmlBuilder.BuildWeEventXml(meta);
                var eventName = eventXml.Descendants("dataName").First().Value;
                var eventPath = Path.Combine(path, eventName);
                Directory.CreateDirectory(eventPath);
                eventXml.Save(Path.Combine(eventPath, "Event.xml"));
            }

            var musicFolder = Path.Combine(path, workdirName);
            Directory.CreateDirectory(musicFolder);
            xml.Save(Path.Combine(musicFolder, "Music.xml"));

            var chartOpts = new ChartConverter.Context(ChartPath, Path.Combine(musicFolder, chartName), meta);
            var musicOpts = new MusicConverter.Context(meta.BgmFileName, path, songId, (double)meta.BgmOffset, (double)meta.BgmPreviewStart, (double)meta.BgmPreviewStop);
            var jacketOpts = new JacketConverter.Context(meta.JacketFileName, Path.Combine(musicFolder, jacketName));

            await chartConverter.ConvertAsync(chartOpts, diag, p, ct);
            if (diag.HasErrors) return;
            ct.ThrowIfCancellationRequested();

            await jacketConverter.ConvertAsync(jacketOpts, diag, p, ct);
            if (diag.HasErrors) return;
            ct.ThrowIfCancellationRequested();

            await musicConverter.ConvertAsync(musicOpts, diag, p, ct);
            if (diag.HasErrors) return;
            ct.ThrowIfCancellationRequested();
        });

        SystemSounds.Exclamation.Play();
    }
}