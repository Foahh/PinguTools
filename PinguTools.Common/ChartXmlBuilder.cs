using PinguTools.Common.Localization;
using System.Xml.Linq;

namespace PinguTools.Common;

public static class ChartXmlBuilder
{
    private static readonly XDeclaration Decl = new("1.0", "utf‑8", null);
    private static readonly XAttribute XsiAttr = new(XNamespace.Xmlns + "xsi", XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance"));
    private static readonly XAttribute XsdAttr = new(XNamespace.Xmlns + "xsd", XNamespace.Get("http://www.w3.org/2001/XMLSchema"));
    private static readonly Entry DefaultNetOpenName = new(2600, "v2_30 00_0", null);

    private static readonly Dictionary<Difficulty, (int id, string str, string data)> DiffMap = new()
    {
        [Difficulty.Basic] = (0, "Basic", "BASIC"),
        [Difficulty.Advanced] = (1, "Advanced", "ADVANCED"),
        [Difficulty.Expert] = (2, "Expert", "EXPERT"),
        [Difficulty.Master] = (3, "Master", "MASTER"),
        [Difficulty.Ultima] = (4, "Ultima", "ULTIMA"),
        [Difficulty.WorldsEnd] = (5, "WorldsEnd", "WORLD'S END")
    };

    private static XDocument Doc(string root, params object?[] content)
    {
        return new XDocument(Decl, Elem(root, XsdAttr, XsiAttr, content));
    }

    private static XElement Elem(string name, params object?[] content)
    {
        return new XElement(name, content);
    }

    private static XElement Val(string n, object? v)
    {
        return new XElement(n, v);
    }

    private static XElement Bool(string n, bool v = false)
    {
        return new XElement(n, v);
    }

    private static XElement Path(string parent, string? path)
    {
        return Elem(parent, Val("path", path));
    }

    private static XElement Id(string element, object id, string str, string? data = null)
    {
        return Elem(element, Val("id", id), Val("str", str), Val("data", data));
    }

    private static XElement Id(string element, Entry entry)
    {
        return Elem(element, Val("id", entry.Id), Val("str", entry.Str), Val("data", entry.Data));
    }

    private static (int whole, int frac) SplitLevel(decimal level)
    {
        if (level <= 0) return (0, 0);
        var w = (int)decimal.Truncate(level);
        var f = (int)((level - w) * 100);
        if (f < 100) return (w, f);
        w += 1;
        f -= 100;
        return (w, f);
    }

    private static XElement DiffId(Difficulty d)
    {
        return Id("type", DiffMap[d].id, DiffMap[d].str, DiffMap[d].data);
    }

    public static XDocument BuildStageXml(int id, Entry noteFieldLane)
    {
        return Doc("StageData",
            Val("dataName", $"stage{id:000000}"),
            Id("netOpenName", DefaultNetOpenName),
            Id("releaseTagName", Entry.Default),
            Id("name", id, $"custom_stage_{id}"),
            Id("notesFieldLine", noteFieldLane),
            Path("notesFieldFile", $"nf_custom_stage_{id}.afb"),
            Path("baseFile", $"st_custom_stage_{id}.afb"),
            Elem("objectFile", Val("path", null))
        );
    }

    public static XDocument BuildCueFileXml(int? id)
    {
        if (id is null) throw new OperationCanceledException(CommonStrings.Error_song_id_is_not_set);
        var idStr = $"{id:0000}";
        return Doc("CueFileData",
            Val("dataName", $"cueFile{id:000000}"),
            Id("name", id, $"music{idStr}"),
            Path("acbFile", $"music{idStr}.acb"),
            Path("awbFile", $"music{idStr}.awb"));
    }

    public static XDocument BuildMusicXml(ChartMeta meta)
    {
        var id = meta.Id ?? throw new OperationCanceledException(CommonStrings.Error_song_id_is_not_set);
        var idStr = $"{id:0000}";

        return Doc("MusicData",
            Val("dataName", $"music{idStr}"),
            Id("releaseTagName", Entry.Default),
            Id("netOpenName", DefaultNetOpenName),
            Bool("disableFlag"),
            Val("exType", meta.Difficulty == Difficulty.WorldsEnd ? 2 : 0),
            Id("name", id, meta.Title),
            Val("sortName", meta.SortName),
            Id("artistName", id, meta.Artist),
            Elem("genreNames",
                Elem("list", Id("StringID", 1000, "自制譜"))),
            Id("worksName", Entry.Default),
            Id("labelName", Entry.Default),
            Path("jaketFile", $"CHU_UI_Jacket_{idStr}.dds"),
            Bool("firstLock"),
            Bool("enableUltima"),
            Bool("isGiftMusic"),
            Val("releaseDate", meta.ReleaseDate.ToString("yyyyMMdd")),
            Val("priority", 0),
            Id("cueFileName", id, $"music{idStr}"),
            Id("worldsEndTagName", meta.WeTag),
            Val("starDifType", (int)meta.WeDifficulty),
            Id("stageName", meta.Stage),
            Elem("fumens", Enum.GetValues<Difficulty>().Select(diff =>
            {
                decimal? level = meta.Difficulty == diff ? meta.Level : null;
                return BuildFumen(diff, id, level);
            })));
    }

    private static XElement BuildFumen(Difficulty diff, int id, decimal? level)
    {
        var (whole, frac) = level is null ? (0, 0) : SplitLevel((decimal)level);
        return Elem("MusicFumenData",
            DiffId(diff),
            Bool("enable", level is not null),
            Path("file", $"{id:0000}_{(int)diff:00}.c2s"),
            Val("level", whole),
            Val("levelDecimal", frac),
            Val("notesDesigner", null),
            Val("defaultBpm", 0));
    }

    public static XDocument BuildWeEventXml(ChartMeta meta)
    {
        var id = meta.Id ?? throw new OperationCanceledException(CommonStrings.Error_song_id_is_not_set);
        var eventId = meta.WeEventId ?? throw new OperationCanceledException(CommonStrings.Error_we_event_id_is_not_set);

        return Doc("EventData",
            Val("dataName", $"event{eventId:00000000}"),
            Id("netOpenName", DefaultNetOpenName),
            Id("name", eventId, "WORLD'S END曲開放"),
            Val("text", null),
            Id("ddsBannerName", Entry.Default),
            Val("periodDispType", 1),
            Bool("alwaysOpen", true),
            Bool("teamOnly"),
            Bool("isKop"),
            Val("priority", 0),
            Elem("substances",
                Val("type", 3),
                Elem("flag", Val("value", 0)),
                Elem("information",
                    Val("informationType", 0),
                    Val("informationDispType", 0),
                    Id("mapFilterID", Entry.Default),
                    Elem("courseNames", Elem("list")),
                    Val("text", null),
                    Elem("image", Path("path", null)),
                    Id("movieName", Entry.Default),
                    Elem("presentNames", Elem("list"))
                ),
                Elem("map",
                    Val("tagText", null),
                    Id("mapName", Entry.Default),
                    Elem("musicNames", Elem("list"))
                ),
                Elem("music",
                    Val("musicType", 1),
                    Elem("musicNames",
                        Elem("list",
                            Id("StringID", id, meta.Title)
                        )
                    )
                ),
                Elem("advertiseMovie",
                    Id("firstMovieName", Entry.Default),
                    Id("secondMovieName", Entry.Default)
                ),
                Elem("recommendMusic",
                    Elem("musicNames", Elem("list"))
                ),
                Elem("release", Val("value", 0)),
                Elem("course",
                    Elem("courseNames", Elem("list")),
                    Elem("quest",
                        Elem("questNames", Elem("list")))
                ),
                Elem("duel",
                    Id("duelName", Entry.Default)
                ),
                Elem("cmission",
                    Id("cmissionName", Entry.Default)
                ),
                Elem("changeSurfBoardUI",
                    Val("value", 0)
                ),
                Elem("avatarAccessoryGacha",
                    Id("avatarAccessoryGachaName", Entry.Default)
                ),
                Elem("rightsInfo",
                    Elem("rightsNames", Elem("list"))
                ),
                Elem("playRewardSet",
                    Id("playRewardSetName", Entry.Default)
                ),
                Elem("dailyBonusPreset",
                    Id("dailyBonusPresetName", Entry.Default)
                ),
                Elem("matchingBonus",
                    Id("timeTableName", Entry.Default)
                ),
                Elem("unlockChallenge",
                    Id("unlockChallengeName", Entry.Default)
                )
            )
        );
    }
}