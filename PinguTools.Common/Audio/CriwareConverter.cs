using PinguTools.Common.Resources;
using SonicAudioLib.Archives;
using SonicAudioLib.CriMw;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
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

namespace PinguTools.Common.Audio;

public class CriwareConverter
{
    public required ulong Key { get; init; }

    public async Task CreateAsync(string acbPath, string awbPath, string cueName, Stream waveStream, double loopStart, double loopEnd, CancellationToken ct = default)
    {
        var waveReader = new WaveReader();
        var data = waveReader.Read(waveStream);
        var format = data.GetFormat<Pcm16Format>();
        if (format.SampleRate != 48000) throw new InvalidOperationException(string.Format(CommonStrings.Error_invalid_sample_rate_48000, format.SampleRate));

        ct.ThrowIfCancellationRequested();

        var hcaWriter = new HcaWriter
        {
            Configuration = new HcaConfiguration
            {
                Bitrate = 16384 * 8,
                Quality = CriHcaQuality.Highest,
                TrimFile = false,
                EncryptionKey = new CriHcaKey(Key)
            }
        };

        var hcaPath = Path.Combine(ResourceManager.TempPath, $"{cueName}.hca");
        await using (var tempFs = File.Create(hcaPath)) hcaWriter.WriteToStream(data, tempFs);

        ct.ThrowIfCancellationRequested();

        var cueSheetTable = new CriTable();

        cueSheetTable.Load(CommonResources.dummy_acb);
        cueSheetTable.Rows[0]["Name"] = cueName;

        var cueTable = new CriTable();
        cueTable.Load((byte[])cueSheetTable.Rows[0]["CueTable"]);

        var lengthMs = (int)(format.SampleCount / (float)format.SampleRate * 1000f);
        cueTable.Rows[0]["Length"] = lengthMs;

        cueTable.WriterSettings = CriTableWriterSettings.Adx2Settings;
        cueSheetTable.Rows[0]["CueTable"] = cueTable.Save();

        var trackEventTable = new CriTable();
        trackEventTable.Load((byte[])cueSheetTable.Rows[0]["TrackEventTable"]);

        var cmdStream = new MemoryStream((byte[])trackEventTable.Rows[1]["Command"]);
        await using (var bw = new BinaryWriter(cmdStream, Encoding.Default, true))
        {
            cmdStream.Position = 3;
            bw.WriteUInt32BigEndian((uint)(loopStart * 1000f));
            cmdStream.Position = 17;
            bw.WriteUInt32BigEndian((uint)(loopEnd * 1000f));
        }
        trackEventTable.Rows[1]["Command"] = cmdStream.ToArray();
        cueSheetTable.Rows[0]["TrackEventTable"] = trackEventTable.Save();

        await using var hcaFs = File.OpenRead(hcaPath);
        var awbEntry = new CriAfs2Entry { Stream = hcaFs };
        var awbArchive = new CriAfs2Archive { awbEntry };
        await using var awbStream = File.Open(awbPath, FileMode.Create, FileAccess.ReadWrite);
        awbArchive.Save(awbStream);
        awbStream.Position = 0;

        var streamAwbHashTbl = new CriTable();
        streamAwbHashTbl.Load((byte[])cueSheetTable.Rows[0]["StreamAwbHash"]);

        var sha = await SHA1.HashDataAsync(awbStream, ct);
        streamAwbHashTbl.Rows[0]["Name"] = cueName;
        streamAwbHashTbl.Rows[0]["Hash"] = sha;
        cueSheetTable.Rows[0]["StreamAwbHash"] = streamAwbHashTbl.Save();

        var waveformTable = new CriTable();
        waveformTable.Load((byte[])cueSheetTable.Rows[0]["WaveformTable"]);

        waveformTable.Rows[0]["SamplingRate"] = (ushort)format.SampleRate;
        waveformTable.Rows[0]["NumSamples"] = format.SampleCount;
        cueSheetTable.Rows[0]["WaveformTable"] = waveformTable.Save();

        cueSheetTable.WriterSettings = CriTableWriterSettings.Adx2Settings;
        await using (var acbStream = File.Create(acbPath)) cueSheetTable.Save(acbStream);
    }
}

public static class BinaryWriterExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt32BigEndian(this BinaryWriter bw, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        bw.Write(buffer);
    }
}