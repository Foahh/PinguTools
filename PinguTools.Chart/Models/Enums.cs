using System.ComponentModel;

namespace PinguTools.Chart.Models;

public enum AirDirection
{
    [Description("Up")] IR,
    [Description("Up Left")] UL,
    [Description("Up Right")] UR,
    [Description("Down")] DW,
    [Description("Down Left")] DL,
    [Description("Down Right")] DR
}

public enum Color
{
    [Description("Default")] DEF,
    [Description("Red")] RED,
    [Description("Orange")] ORN,
    [Description("Yellow")] YEL,
    [Description("Green")] GRN,
    [Description("Cyan")] CYN,
    [Description("Blue")] BLU,
    [Description("Purple")] PPL,
    [Description("Pink")] PNK,
    [Description("White")] VLT,
    [Description("Gray")] GRY,
    [Description("Black")] BLK,
    [Description("None")] NON
}

public enum ExEffect
{
    [Description("Up")] UP,
    [Description("Down")] DW,
    [Description("Center")] CE,
    [Description("Left")] LS,
    [Description("Right")] RS,
    [Description("Rotate Left")] LC,
    [Description("Rotate Right")] RC,
    [Description("InOut")] BS
    // [Description("OutIn")] Unknown
}

public enum Joint
{
    [Description("Control")] C,
    [Description("Step")] D
}