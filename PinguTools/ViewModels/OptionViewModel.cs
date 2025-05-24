using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using PinguTools.Common;
using PinguTools.Common.Asset;
using PinguTools.Common.Audio;
using PinguTools.Common.Chart;
using PinguTools.Common.Chart.Parser;
using PinguTools.Common.Graphic;
using PinguTools.Common.Resources;
using PinguTools.Common.Xml;
using PinguTools.Models;
using PinguTools.Resources;
using System.IO;
using System.Media;

namespace PinguTools.ViewModels;

public partial class OptionViewModel : WatchViewModel<OptionModel>
{
    [ObservableProperty]
    public partial Book? SelectedBook { get; set; }

    [ObservableProperty]
    public partial BookItem? SelectedBookItem { get; set; }

    protected override string FileGlob => "*.mgxc";

    protected override bool IsFileChanged(string path)
    {
        return Model?.Books.Values.Where(p => p.Meta.FilePath == path) != null;
    }

    protected override void SetModel(OptionModel? oldModel, OptionModel? newModel)
    {
        base.SetModel(oldModel, newModel);
        SelectedBook = null;
        SelectedBookItem = null;
    }

    protected async override Task<OptionModel> ReadModel(string path, IDiagnostic d, IProgress<string> p, CancellationToken ct = default)
    {
        p.Report(CommonStrings.Status_Searching);
        var model = new OptionModel();
        await model.LoadAsync(path, ct);
        var books = model.Books;

        var walker = Directory.EnumerateFiles(path, FileGlob, SearchOption.AllDirectories);
        await BatchAsync(walker, d, async (file, d) =>
        {
            ct.ThrowIfCancellationRequested();
            if (Path.GetExtension(file) != ".mgxc") return;
            var parser = new MgxcParser(d, AssetManager);
            var chart = await parser.ParseAsync(file, ct);
            var meta = chart.Meta;
            var id = meta.Id ?? throw new DiagnosticException(Strings.Diag_file_ignored_due_to_id_missing);
            if (!books.TryGetValue(id, out var book)) books[id] = book = new Book();
            var ctx = new BookItem(chart);
            if (book.Items.ContainsKey(meta.Difficulty)) d.Report(Severity.Warning, Strings.Diag_duplicate_id_and_difficulty, target: file);
            book.Items[meta.Difficulty] = ctx;
        });

        if (books.Count <= 0) throw new DiagnosticException(Strings.Error_no_charts_are_found_directory);
        ct.ThrowIfCancellationRequested();

        foreach (var (id, book) in books.ToList())
        {
            ct.ThrowIfCancellationRequested();
            var items = book.Items.Values.ToArray();
            if (items.Length == 0)
            {
                books.Remove(id);
                continue;
            }

            if (book.Items.ContainsKey(Difficulty.WorldsEnd) && book.Items.Count != 1) d.Report(Severity.Error, Strings.Diag_we_chart_must_be_unique_id, target: items);
            var mainItems = items.Where(x => x.Chart.Meta.IsMain).ToArray();
            if (mainItems.Length > 1) d.Report(Severity.Warning, Strings.Diag_more_than_one_chart_marked_main, target: mainItems);
            else if (mainItems.Length == 0 && items.Length > 1) d.Report(Severity.Warning, Strings.Diag_no_chart_marked_main, target: mainItems);

            var mainItem = mainItems.FirstOrDefault() ?? mainItems.OrderByDescending(x => x.Difficulty).FirstOrDefault();
            if (mainItem == null)
            {
                books.Remove(id);
                continue;
            }

            book.MainDifficulty = mainItem.Difficulty;
        }

        ct.ThrowIfCancellationRequested();
        p.Report(CommonStrings.Status_done);

        return model;
    }

    protected async override Task Action()
    {
        var settings = Model;
        if (settings == null) return;
        if (!settings.CanExecute) throw new DiagnosticException(Strings.Error_but_nobody_came);

        var books = settings.Books;
        if (books.Count == 0) throw new DiagnosticException(Strings.Error_no_charts_are_found_directory);

        var initialDirectory = settings.WorkingDirectory;
        if (string.IsNullOrWhiteSpace(initialDirectory) || !Directory.Exists(initialDirectory))
        {
            var dir = Path.GetDirectoryName(ModelPath);
            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir)) initialDirectory = dir;
        }

        var dlg = new OpenFolderDialog
        {
            ClientGuid = new Guid("C81454B6-EA09-41D6-90B2-4BD4FB3D5449"),
            Title = Strings.Title_select_the_output_folder,
            Multiselect = false,
            ValidateNames = true,
            InitialDirectory = initialDirectory
        };
        if (dlg.ShowDialog() != true) return;
        settings.WorkingDirectory = dlg.FolderName;

        var path = Path.Combine(settings.WorkingDirectory, settings.OptionName);
        await ActionService.RunAsync(async (d, p, ct) =>
        {
            var musicFolder = Path.Combine(path, "music");
            var stageFolder = Path.Combine(path, "stage");
            var cueFileFolder = Path.Combine(path, "cueFile");
            var eventFolder = Path.Combine(path, "event");
            var weEntries = new SortedSet<Entry>();
            var ultEntries = new SortedSet<Entry>();

            await settings.SaveAsync(ModelPath, ct);

            await BatchAsync(settings, d, p, async (book, d) =>
            {
                var musicConverter = new MusicConverter();
                var chartConverter = new ChartConverter();
                var stageConverter = new StageConverter(AssetManager);
                var jacketConverter = new JacketConverter();

                var stage = book.Stage;
                if (book.IsCustomStage && settings.ConvertBackground)
                {
                    if (string.IsNullOrWhiteSpace(book.Meta.FullBgFilePath)) throw new DiagnosticException(CommonStrings.Error_background_file_is_not_set);
                    if (book.StageId is null) throw new DiagnosticException(CommonStrings.Error_stage_id_is_not_set);
                    var stageOpts = new StageConverter.Context(book.Meta.FullBgFilePath, [], book.StageId, stageFolder, book.NotesFieldLine);
                    await stageConverter.ConvertAsync(stageOpts, d, null, ct);
                    if (d.HasErrors) throw new DiagnosticException(Strings.Error_Stage_Failed);
                    stage = stageOpts.Result;
                    ct.ThrowIfCancellationRequested();
                }

                var metaMap = book.Items.ToDictionary(x => x.Key, x => x.Value.Meta);
                var xml = new MusicXml(metaMap, book.Difficulty) { StageName = stage };
                var chartFolder = Path.Combine(musicFolder, xml.DataName);

                if (settings.GenerateMusicXml)
                {
                    await xml.SaveAsync(chartFolder);
                    ct.ThrowIfCancellationRequested();
                }

                if (settings.ConvertChart)
                {
                    Directory.CreateDirectory(chartFolder);
                    foreach (var (diff, item) in book.Items)
                    {
                        if (item.Id is not { } songId) throw new DiagnosticException(CommonStrings.Error_song_id_is_not_set);
                        if (diff == Difficulty.WorldsEnd) weEntries.Add(new Entry(songId, book.Title));
                        else if (diff == Difficulty.Ultima) ultEntries.Add(new Entry(songId, book.Title));
                        var chartPath = Path.Combine(chartFolder, xml[item.Difficulty].File);
                        var chartOpts = new ChartConverter.Context(chartPath, item.Chart);
                        await chartConverter.ConvertAsync(chartOpts, d, null, ct);
                        if (d.HasErrors) throw new DiagnosticException(Strings.Error_Chart_Failed);
                        ct.ThrowIfCancellationRequested();
                    }
                }

                if (settings.ConvertJacket)
                {
                    var jacketOpts = new JacketConverter.Context(book.Meta.FullJacketFilePath, Path.Combine(chartFolder, xml.JaketFile));
                    await jacketConverter.ConvertAsync(jacketOpts, d, null, ct);
                    if (d.HasErrors) throw new DiagnosticException(Strings.Error_Jacket_Failed);
                    ct.ThrowIfCancellationRequested();
                }

                // parallel will cause strange problem for this
                if (settings.ConvertAudio)
                {
                    var localDiag = new DiagnosticReporter();
                    var musicOpts = new MusicConverter.Context(book.Meta, cueFileFolder);
                    await musicConverter.ConvertAsync(musicOpts, localDiag, null, ct);
                    if (localDiag.HasErrors) throw new DiagnosticException(Strings.Error_Music_Failed);
                    ct.ThrowIfCancellationRequested();
                }
            }, ct);

            if (settings.GenerateEventXml && ultEntries.Count > 0)
            {
                var eventXml = new EventXml(settings.UltimaEventId, EventXml.MusicType.Ultima, ultEntries);
                var eventPath = Path.Combine(eventFolder, eventXml.DataName);
                await eventXml.SaveAsync(eventPath);
                ct.ThrowIfCancellationRequested();
            }

            if (settings.GenerateEventXml && ultEntries.Count > 0)
            {
                var eventXml = new EventXml(settings.WeEventId, EventXml.MusicType.WldEnd, weEntries);
                var eventPath = Path.Combine(eventFolder, eventXml.DataName);
                await eventXml.SaveAsync(eventPath);
                ct.ThrowIfCancellationRequested();
            }
        });

        SystemSounds.Exclamation.Play();
    }

    private async static Task BatchAsync(OptionModel model, IDiagnostic d, IProgress<string> p, Func<Book, IDiagnostic, Task> action, CancellationToken ct = default)
    {
        var items = model.Books.Values;
        var total = items.Count;
        var completedCount = 0;
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = model.MaxParallelismDegree, CancellationToken = ct };

        p.Report($"{0}/{total}...");
        await Parallel.ForEachAsync(items, parallelOptions, async (book, ct) =>
        {
            var ld = new DiagnosticReporter();
            try
            {
                await action(book, ld);
                ct.ThrowIfCancellationRequested();
            }
            catch (DiagnosticException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ld.Report(ex);
            }
            finally
            {
                var done = Interlocked.Increment(ref completedCount);
                p.Report($"{done}/{total}...");
                foreach (var diagItem in ld.Diagnostics)
                {
                    if (string.IsNullOrWhiteSpace(diagItem.Path))
                    {
                        diagItem.Path = book.Meta.FilePath;
                    }
                    d.Report(diagItem);
                }
            }
        });
    }

    private async static Task BatchAsync(IEnumerable<string> items, IDiagnostic d, Func<string, IDiagnostic, Task> action)
    {
        foreach (var item in items)
        {
            var ld = new DiagnosticReporter();
            try
            {
                await action(item, ld);
            }
            catch (Exception ex)
            {
                ld.Report(ex);
            }
            finally
            {
                foreach (var diagItem in ld.Diagnostics)
                {
                    if (string.IsNullOrWhiteSpace(diagItem.Path)) diagItem.Path = item;
                    d.Report(diagItem);
                }
            }
        }
    }

    protected override Task Reload()
    {
        Model?.SaveAsync(ModelPath);
        return base.Reload();
    }
}