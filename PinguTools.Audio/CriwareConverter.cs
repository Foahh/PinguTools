using SonicAudioLib.Archives;
using SonicAudioLib.CriMw;
using System.Security.Cryptography;
using System.Text;
using VGAudio.Codecs.CriHca;
using VGAudio.Containers.Hca;
using VGAudio.Containers.Wave;
using VGAudio.Formats.Pcm16;

/*
 * Originally by Margrithm
 * https://margrithm.girlsband.party/
 */

namespace PinguTools.Audio;

public class CriwareConverter
{
    public required ulong Key { get; init; }

    public async Task<(byte[] Acb, byte[] Awb)> CreateAsync(string cueName, byte[] dummyData, byte[] waveData, double loopStart, double loopEnd)
    {
        var waveReader = new WaveReader();
        var pcmData = waveReader.Read(waveData);
        var pcmFormat = pcmData.GetFormat<Pcm16Format>();

        byte[] hcaBytes;
        var hcaWriter = new HcaWriter
        {
            Configuration = new HcaConfiguration
            {
                Bitrate = 131_072,
                Quality = CriHcaQuality.Highest,
                TrimFile = false,
                EncryptionKey = new CriHcaKey(Key)
            }
        };

        await using (var hcaMs = new MemoryStream())
        {
            hcaWriter.WriteToStream(pcmData, hcaMs);
            hcaBytes = hcaMs.ToArray();
        }

        return BuildAcbInternal(dummyData, hcaBytes, pcmFormat, cueName, loopStart, loopEnd);
    }

    private static (byte[] Acb, byte[] Awb) BuildAcbInternal(byte[] template, byte[] hca, Pcm16Format format, string name, double loopStart, double loopEnd)
    {
        var cueSheetTable = new CriTable();

        cueSheetTable.Load(template);
        cueSheetTable.Rows[0]["Name"] = name;

        var cueTable = new CriTable();
        cueTable.Load((byte[])cueSheetTable.Rows[0]["CueTable"]);

        var lengthMs = (int)(format.SampleCount / (float)format.SampleRate * 1000f);
        cueTable.Rows[0]["Length"] = lengthMs;

        cueTable.WriterSettings = CriTableWriterSettings.Adx2Settings;
        cueSheetTable.Rows[0]["CueTable"] = cueTable.Save();

        var trackEventTable = new CriTable();
        trackEventTable.Load((byte[])cueSheetTable.Rows[0]["TrackEventTable"]);

        var cmdStream = new MemoryStream((byte[])trackEventTable.Rows[1]["Command"]);
        using (var bw = new BinaryWriter(cmdStream, Encoding.Default, true))
        {
            cmdStream.Position = 3;
            bw.Write(BSwap32((uint)(loopStart * 1000f)));

            cmdStream.Position = 17;
            bw.Write(BSwap32((uint)(loopEnd * 1000f)));
        }
        trackEventTable.Rows[1]["Command"] = cmdStream.ToArray();
        cueSheetTable.Rows[0]["TrackEventTable"] = trackEventTable.Save();

        var awbArchive = new CriAfs2Archive
        {
            new CriAfs2Entry
            {
                Id = 0,
                Bytes = hca
            }
        };
        var awbBytes = awbArchive.Save();

        var streamAwbHashTbl = new CriTable();
        streamAwbHashTbl.Load((byte[])cueSheetTable.Rows[0]["StreamAwbHash"]);

        var sha = SHA1.HashData(awbBytes);
        streamAwbHashTbl.Rows[0]["Name"] = name;
        streamAwbHashTbl.Rows[0]["Hash"] = sha;
        cueSheetTable.Rows[0]["StreamAwbHash"] = streamAwbHashTbl.Save();

        var waveformTable = new CriTable();
        waveformTable.Load((byte[])cueSheetTable.Rows[0]["WaveformTable"]);

        waveformTable.Rows[0]["SamplingRate"] = (ushort)format.SampleRate;
        waveformTable.Rows[0]["NumSamples"] = format.SampleCount;
        cueSheetTable.Rows[0]["WaveformTable"] = waveformTable.Save();

        cueSheetTable.WriterSettings = CriTableWriterSettings.Adx2Settings;
        var acbBytes = cueSheetTable.Save();

        return (acbBytes, awbBytes);
    }

    private static uint BSwap32(uint v)
    {
        return v >> 24 | (v & 0x00FF_0000) >> 8 | (v & 0x0000_FF00) << 8 | v << 24;
    }
}