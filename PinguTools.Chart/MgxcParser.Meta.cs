using PinguTools.Common;
using System.Text;
using System.Text.RegularExpressions;
using mgxc = PinguTools.Chart.Models.mgxc;

namespace PinguTools.Chart;

public partial class MgxcParser
{
    protected bool ParseMeta(mgxc.Chart mgxcChart, string[] args)
    {
        if (args[0] == "TITLE")
        {
            if (args.Length < 2) return false;
            mgxcChart.Meta.Title = args[1];
            mgxcChart.Meta.SortName = GetSortName(mgxcChart.Meta.Title);
            return true;
        }

        if (args[0] == "SORT")
        {
            return true;
        }

        if (args[0] == "ARTIST")
        {
            if (args.Length < 2) return false;
            mgxcChart.Meta.Artist = args[1];
            return true;
        }

        if (args[0] == "GENRE")
        {
            return true;
        }

        if (args[0] == "DESIGNER")
        {
            if (args.Length < 2) return false;
            mgxcChart.Meta.Designer = args[1];
            return true;
        }

        if (args[0] == "DIFFICULTY")
        {
            if (args.Length < 2) return false;
            var diff = int.Parse(args[1]);
            mgxcChart.Meta.Difficulty = diff switch
            {
                0 => Difficulty.Basic,
                1 => Difficulty.Advanced,
                2 => Difficulty.Expert,
                3 => Difficulty.Master,
                4 => Difficulty.WorldsEnd,
                5 => Difficulty.Ultima,
                _ => Difficulty.Master
            };
            return true;
        }

        if (args[0] == "PLAYLEVEL")
        {
            if (args.Length < 2) return false;
            if (mgxcChart.Meta.Difficulty != Difficulty.WorldsEnd) return true;
            mgxcChart.Meta.WeDifficulty = int.Parse(args[1].Trim('+')) switch
            {
                1 => StarDifficulty.S1,
                2 => StarDifficulty.S2,
                3 => StarDifficulty.S3,
                4 => StarDifficulty.S4,
                5 => StarDifficulty.S5,
                _ => StarDifficulty.NA
            };
            return true;
        }

        if (args[0] == "WEATTRIBUTE")
        {
            if (args.Length < 2) return false;
            var attr = weTags?.FirstOrDefault(x => x.Str == args[1]);
            if (attr != null) mgxcChart.Meta.WeTag = attr;
            return true;
        }

        if (args[0] == "CHARTCONST")
        {
            if (args.Length < 2) return false;
            if (mgxcChart.Meta.Difficulty == Difficulty.WorldsEnd) return true;
            mgxcChart.Meta.Level = decimal.Parse(args[1]);
            return true;
        }

        if (args[0] == "SONGID")
        {
            if (args.Length < 2) return false;
            mgxcChart.Meta.MgxcId = args[1];
            if (int.TryParse(mgxcChart.Meta.MgxcId, out var id)) mgxcChart.Meta.Id = id;
            return true;
        }

        if (args[0] == "BGM")
        {
            if (args.Length < 2) return false;
            mgxcChart.Meta.BgmFileName = args[1];
            return true;
        }

        if (args[0] == "BGMOFFSET")
        {
            if (args.Length < 2) return false;
            mgxcChart.Meta.BgmOffset = decimal.Parse(args[1]);
            return true;
        }

        if (args[0] == "BGMPREVIEW")
        {
            if (args.Length < 3) return false;
            mgxcChart.Meta.BgmPreviewStart = decimal.Parse(args[1]);
            mgxcChart.Meta.BgmPreviewStop = decimal.Parse(args[2]);
            return true;
        }

        if (args[0] == "JACKET")
        {
            if (args.Length < 2) return false;
            mgxcChart.Meta.JacketFileName = args[1];
            return true;
        }

        if (args[0] == "BG")
        {
            if (args.Length < 2) return false;
            mgxcChart.Meta.BgFileName = args[1];
            return true;
        }

        if (args[0] == "BGSCENE")
        {
            return true;
        }

        if (args[0] == "BGSYNC")
        {
            return true;
        }

        if (args[0] == "FIELDCOL")
        {
            return true;
        }

        if (args[0] == "FIELDBG")
        {
            return true;
        }

        if (args[0] == "FIELDSCENE")
        {
            return true;
        }

        if (args[0] == "MAINTIL")
        {
            if (args.Length < 2) return false;
            mgxcChart.Meta.MainTil = int.Parse(args[1]);
            return true;
        }

        if (args[0] == "MAINBPM")
        {
            if (args.Length < 2) return false;
            mgxcChart.Meta.MainBpm = decimal.Parse(args[1]);
            return true;
        }

        if (args[0] == "TUTORIAL")
        {
            return true;
        }

        if (args[0] == "SOFFSET")
        {
            if (args.Length < 2) return false;
            mgxcChart.Meta.BgmEnableBarOffset = ToBoolean(args[1]);
            return true;
        }

        if (args[0] == "USECLICK")
        {
            return true;
        }

        if (args[0] == "EXLONG")
        {
            return true;
        }

        if (args[0] == "BGMWAITEND")
        {
            return true;
        }

        if (args[0] == "AUTHOR_LIST")
        {
            return true;
        }

        if (args[0] == "AUTHORSITES")
        {
            return true;
        }

        if (args[0] == "DLURL")
        {
            return true;
        }

        if (args[0] == "COPYRIGHT")
        {
            return true;
        }

        if (args[0] == "LICENSE")
        {
            return true;
        }

        if (args[0] == "COMMENT")
        {
            return true;
        }

        if (args[0] == "BMARKER")
        {
            return true;
        }

        if (args[0] == "BKMRK")
        {
            return true;
        }

        return false;
    }

    private static bool ToBoolean(string str)
    {
        return str.ToLowerInvariant() is "1" or "true" or "y" or "yes" or "enable" or "enabled";
    }

    private static string GetSortName(string? s)
    {
        if (s is null) return string.Empty;
        var t = s.ToUpperInvariant().Normalize(NormalizationForm.FormKC);
        t = WhitespaceRegex().Replace(t, "_");
        t = SpecialCharacterRegex().Replace(t, "");
        return t;
    }

    [GeneratedRegex(@"\s+")] private static partial Regex WhitespaceRegex();
    [GeneratedRegex(@"[^\p{L}\p{N}_]")] private static partial Regex SpecialCharacterRegex();
}