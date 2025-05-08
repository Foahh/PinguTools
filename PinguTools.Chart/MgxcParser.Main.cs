using PinguTools.Chart.Localization;
using PinguTools.Chart.Models;
using PinguTools.Common;
using mgxc = PinguTools.Chart.Models.mgxc;

namespace PinguTools.Chart;

public partial class MgxcParser
{
    protected bool ParseHeader(mgxc.Chart mgxc, string[] args)
    {
        if (args[0] == "BPM")
        {
            if (args.Length < 3) return false;
            var e = new mgxc.BpmEvent
            {
                Tick = int.Parse(args[1]),
                Bpm = decimal.Parse(args[2])
            };
            mgxc.Events.AppendChild(e);
            return true;
        }

        if (args[0] == "BEAT")
        {
            if (args.Length < 4) return false;
            var e = new mgxc.BeatEvent
            {
                Bar = int.Parse(args[1]),
                Numerator = int.Parse(args[2]),
                Denominator = int.Parse(args[3])
            };
            if (e.Bar >= 0) mgxc.Events.AppendChild(e);
            return true;
        }

        if (args[0] == "TIL")
        {
            if (args.Length < 4) return false;
            var e = new mgxc.TimelineEvent
            {
                Timeline = int.Parse(args[1]),
                Tick = int.Parse(args[2]),
                Speed = decimal.Parse(args[3])
            };
            mgxc.Events.AppendChild(e);
            return true;
        }

        if (args[0] == "SPDMOD")
        {
            if (args.Length < 3) return false;
            var e = new mgxc.NoteSpeedEvent
            {
                Tick = int.Parse(args[1]),
                Speed = decimal.Parse(args[2])
            };
            mgxc.Events.AppendChild(e);
            return true;
        }

        return false;
    }

    protected bool ParseNotes(mgxc.Chart mgxc, string[] args)
    {
        if (args.Length < 10) return false;

        mgxc.Note? note = null;
        var isChildNote = false;
        var isPairNote = false;

        if (args[0].Length == 0) return false;
        var noteName = args[0][^1];

        if (noteName == 't')
        {
            note = new mgxc.Tap();
        }
        else if (noteName == 'e')
        {
            var exNote = new mgxc.ExTap();
            exNote.Effect = args[2] switch
            {
                "U" => ExEffect.UP,
                "D" => ExEffect.DW,
                "C" => ExEffect.CE,
                "L" => ExEffect.LS,
                "R" => ExEffect.RS,
                "RL" => ExEffect.LC,
                "RR" => ExEffect.RC,
                "IO" => ExEffect.BS,
                "OI" => ExEffect.CE, // OutIn is not supported in C2S
                _ => ExEffect.UP
            };
            note = exNote;
        }
        else if (noteName == 'f')
        {
            note = new mgxc.Flick();
        }
        else if (noteName == 'd')
        {
            note = new mgxc.Damage();
        }
        else if (noteName == 'h')
        {
            if (args[1] == "BG")
            {
                note = new mgxc.Hold();
            }
            else if (args[1] == "EN")
            {
                note = new mgxc.HoldJoint();
                isChildNote = true;
            }
        }
        else if (noteName == 's')
        {
            if (args[1] == "BG")
            {
                note = new mgxc.Slide();
            }
            else
            {
                var exNote = new mgxc.SlideJoint();
                if (args[1] == "ST" || args[1] == "EN")
                {
                    exNote.Joint = Joint.D;
                    note = exNote;
                    isChildNote = true;
                }
                else if (args[1] == "LC" || args[1] == "CC")
                {
                    exNote.Joint = Joint.C;
                    note = exNote;
                    isChildNote = true;
                }
            }
        }
        else if (noteName == 'a')
        {
            var exNote = new mgxc.Air();
            switch (args[2])
            {
                case "U": exNote.Direction = AirDirection.IR; break;
                case "D": exNote.Direction = AirDirection.DW; break;
                case "UL": exNote.Direction = AirDirection.UL; break;
                case "UR": exNote.Direction = AirDirection.UR; break;
                case "DL": exNote.Direction = AirDirection.DL; break;
                case "DR": exNote.Direction = AirDirection.DR; break;
                default: exNote.Direction = AirDirection.IR; break;
            }
            exNote.Color = args[3] == "IV" ? Color.PNK : Color.DEF;
            note = exNote;
            isPairNote = true;
        }
        else if (noteName is 'H' or 'S') // AirHold should be considered as AirSlide after CHUNITHM NEW
        {
            if (args[1] == "BG")
            {
                var exNote = new mgxc.AirSlide();
                isPairNote = true;

                if (lastNote is mgxc.Air oldLastNote)
                {
                    lastNote.Parent?.RemoveChild(lastNote);
                    lastNote = oldLastNote.PairNote;
                    exNote.Color = oldLastNote.Color;
                }

                exNote.Height = decimal.Parse(args[7]);
                note = exNote;
            }
            else
            {
                var exNote = new mgxc.AirSlideJoint();
                exNote.Joint = args[1] == "ST" || args[1] == "EN" ? Joint.D : Joint.C;
                exNote.Height = decimal.Parse(args[7]);
                note = exNote;
                isChildNote = true;
            }
        }
        else if (noteName == 'C')
        {
            var color = int.Parse(args[9]) switch
            {
                0 => Color.DEF,
                1 => Color.RED,
                2 => Color.ORN,
                3 => Color.YEL,
                4 => Color.GRN,
                5 => Color.CYN,
                6 => Color.BLU,
                7 => Color.PPL,
                8 => Color.PNK,
                9 => Color.VLT,
                10 => Color.GRY,
                11 => Color.BLK,
                35 => Color.NON,
                _ => Color.DEF
            };
            if (args[1] == "BG")
            {
                var exNote = new mgxc.AirCrash
                {
                    Joint = args[3] == "AT" ? Joint.D : Joint.C,
                    Color = color,
                    Height = decimal.Parse(args[7])
                };
                note = exNote;
            }
            else
            {
                var exNote = new mgxc.AirCrashJoint();
                if (args[1] == "ST")
                {
                    exNote.Joint = Joint.D;
                }
                else if (args[1] == "LC")
                {
                    exNote.Joint = Joint.C;
                    exNote.Height = decimal.Parse(args[7]);
                }
                else if (args[1] == "EN")
                {
                    exNote.Joint = args[3] == "AT" ? Joint.D : Joint.C;
                    exNote.Height = decimal.Parse(args[7]);
                }
                note = exNote;
                isChildNote = true;
            }
        }

        if (note == null) return false;

        note.Tick = int.Parse(args[4]);
        note.Lane = int.Parse(args[5]);
        note.Width = int.Parse(args[6]);
        note.Timeline = int.Parse(args[8]);

        if (isChildNote) lastParentNote?.AppendChild(note);
        else mgxc.Notes.AppendChild(note);

        if (isPairNote)
        {
            switch (lastNote)
            {
                case mgxc.PositiveNote lastP when note is mgxc.NegativeNote newN:
                    lastP.MakePair(newN);
                    break;
                case mgxc.NegativeNote lastN when note is mgxc.PositiveNote newP:
                    lastN.MakePair(newP);
                    break;
                default:
                    diagnostic.Report(DiagnosticSeverity.Error, Strings.Diag_pairing_notes_incompatible, note);
                    diagnostic.Report(DiagnosticSeverity.Error, Strings.Diag_pairing_notes_incompatible, lastNote);
                    break;
            }
        }

        if (!isChildNote) lastParentNote = note;

        lastNote = note;
        return true;
    }
}