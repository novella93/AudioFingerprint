using Declarations;
using MessagePack;
using MessagePack.Resolvers;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NWaves.Audio;
using NWaves.Transforms;
using NWaves.Windows;
using System.Diagnostics;

class Program
{
    static Stopwatch Stopwatch = new Stopwatch();
    static BuildDB.Program.State State = BuildDB.Program.State.CHECK_INPUT_PARAMETERS;
    static BuildDB.Audio.Files Files = new BuildDB.Audio.Files();
    static List<float[]>[] MagnitudeSpectrograms = new List<float[]>[0];
    static List<List<List<(int, float)>>> PeakMap = new List<List<List<(int, float)>>>();
    static void Main(string[] args)
    {
        bool execute = true;
        Stopwatch.Start();
        while (execute)
        {
            switch (State)
            {
                case BuildDB.Program.State.CHECK_INPUT_PARAMETERS:
                    if (args.Length == 4)
                    {
                        int validArguments = 0;
                        if (args[0] == BuildDB.Program.INPUT_PARAMETER_TOKEN)
                        {
                            validArguments++;
                            if (Directory.Exists(args[1]))
                            {
                                validArguments++;
                            }
                            else
                            {
                                Console.WriteLine(BuildDB.Program.ErrorTexts.ERROR_INPUT_PATH_DOES_NOT_EXIST);
                                State = BuildDB.Program.State.ERROR;
                            }
                        }
                        if (args[2] == BuildDB.Program.OUTPUT_PARAMETER_TOKEN)
                        {
                            validArguments++;
                            if (args[3] != "")
                            {
                                validArguments++;

                            }
                            else
                            {
                                Console.WriteLine(BuildDB.Program.ErrorTexts.ERROR_INVALID_OUTPUT_PATH);
                                State = BuildDB.Program.State.ERROR;
                            }
                        }
                        if (validArguments == 4)
                        {
                            State = BuildDB.Program.State.READ_SONGS_LIST;
                        }
                        else
                        {
                            Console.WriteLine(BuildDB.Program.ErrorTexts.ERROR_INVALID_ARGUMENTS);
                            State = BuildDB.Program.State.ERROR;
                        }
                    }
                    else
                    {
                        Console.WriteLine(BuildDB.Program.ErrorTexts.ERROR_INVALID_NUMBER_OF_ARGUMENTS);
                        State = BuildDB.Program.State.ERROR;
                    }
                    break;

                case BuildDB.Program.State.READ_SONGS_LIST:
                    Files.OriginalsPath = Directory.GetFiles(args[1]);
                    Files.OriginalsName = Files.OriginalsPath.Select(path => Path.GetFileNameWithoutExtension(path)).ToArray();
                    Files.MonoPath = new string[Files.OriginalsPath.Length];
                    Files.MonoName = new string[Files.OriginalsName.Length];
                    if (Files.OriginalsPath.Length > 0)
                    {
                        State = BuildDB.Program.State.CREATE_FOLDERS;
                    }
                    else
                    {
                        Console.WriteLine(BuildDB.Program.ErrorTexts.ERROR_NO_AUDIO_FILES_FOUND);
                        State = BuildDB.Program.State.ERROR;
                    }
                    break;

                case BuildDB.Program.State.CREATE_FOLDERS:
                    if (!Directory.Exists(BuildDB.Program.RESULTS_FOLDER_NAME))
                    {
                        Directory.CreateDirectory(BuildDB.Program.RESULTS_FOLDER_NAME);
                    }
                    if (!Directory.Exists(BuildDB.Program.RESULTS_FOLDER_NAME + "\\" + BuildDB.Audio.MONO_FILES_FOLDER_NAME))
                    {
                        Directory.CreateDirectory(BuildDB.Program.RESULTS_FOLDER_NAME + "\\" + BuildDB.Audio.MONO_FILES_FOLDER_NAME);
                    }
                    if (!Directory.Exists(BuildDB.Program.RESULTS_FOLDER_NAME + "\\" + BuildDB.Audio.SPECTROGRAM_FILES_FOLDER_NAME))
                    {
                        Directory.CreateDirectory(BuildDB.Program.RESULTS_FOLDER_NAME + "\\" + BuildDB.Audio.SPECTROGRAM_FILES_FOLDER_NAME);
                    }
                    State = BuildDB.Program.State.CONVERT_SONGS_TO_MONO;
                    break;

                case BuildDB.Program.State.CONVERT_SONGS_TO_MONO:
                    for (int idx = 0; idx < Files.OriginalsPath.Length; idx++)
                    {
                        if (!File.Exists(BuildDB.Program.RESULTS_FOLDER_NAME + "\\" + BuildDB.Audio.MONO_FILES_FOLDER_NAME + "\\" + Files.OriginalsName[idx]))
                        {
                            using var audio = new AudioFileReader(Files.OriginalsPath[idx]);
                            if (audio.WaveFormat.Channels == (int)BuildDB.Audio.Channels.STEREO)
                            {
                                var monoProvider = new StereoToMonoSampleProvider(audio) { LeftVolume = 0.5f, RightVolume = 0.5f };
                                WaveFileWriter.CreateWaveFile16(BuildDB.Program.RESULTS_FOLDER_NAME + "\\" + BuildDB.Audio.MONO_FILES_FOLDER_NAME + "\\" + Files.OriginalsName[idx], monoProvider);
                            }
                            else if (audio.WaveFormat.Channels == (int)BuildDB.Audio.Channels.MONO)
                            {
                                File.Copy(Files.OriginalsPath[idx], BuildDB.Program.RESULTS_FOLDER_NAME + "\\" + BuildDB.Audio.MONO_FILES_FOLDER_NAME + "\\" + Files.OriginalsName[idx]);
                            }
                        }
                        Files.MonoName[idx] = Files.OriginalsName[idx];
                        Files.MonoPath[idx] = BuildDB.Program.RESULTS_FOLDER_NAME + "\\" + BuildDB.Audio.MONO_FILES_FOLDER_NAME + "\\" + Files.OriginalsName[idx];
                    }
                    State = BuildDB.Program.State.GET_SPECTROGRAMS;
                    break;

                case BuildDB.Program.State.GET_SPECTROGRAMS:
                    Array.Resize(ref MagnitudeSpectrograms, Files.MonoPath.Length);
                    for (int idx = 0; idx < Files.MonoPath.Length; idx++)
                    {
                        if (!File.Exists(BuildDB.Program.RESULTS_FOLDER_NAME + "\\" + BuildDB.Audio.SPECTROGRAM_FILES_FOLDER_NAME + "\\" + Files.MonoName[idx] + ".msgpack"))
                        {
                            var fileStream = File.OpenRead(Files.MonoPath[idx]);
                            var audio = new WaveFile(fileStream);
                            var signal = audio[Channels.Left];
                            int windowSize = BuildDB.Audio.SPECTROGRAM_WINDOWS_SIZE;
                            int hopSize = BuildDB.Audio.SPECTROGRAM_HOP_SIZE;
                            var stft = new Stft(windowSize, hopSize, WindowType.Hann);
                            MagnitudePhaseList magPhaseSpectrogram = stft.MagnitudePhaseSpectrogram(signal);
                            MagnitudeSpectrograms[idx] = magPhaseSpectrogram.Magnitudes;
                            File.WriteAllBytes(BuildDB.Program.RESULTS_FOLDER_NAME + "\\" + BuildDB.Audio.SPECTROGRAM_FILES_FOLDER_NAME + "\\" + Files.MonoName[idx] + ".msgpack", MessagePackSerializer.Serialize(MagnitudeSpectrograms[idx]));
                        }
                        else
                        {
                            MagnitudeSpectrograms[idx] = MessagePackSerializer.Deserialize<List<float[]>>(File.ReadAllBytes(BuildDB.Program.RESULTS_FOLDER_NAME + "\\" + BuildDB.Audio.SPECTROGRAM_FILES_FOLDER_NAME + "\\" + Files.MonoName[idx] + ".msgpack"));
                        }
                    }
                    State = BuildDB.Program.State.FIND_PEAKS;
                    break;

                case BuildDB.Program.State.FIND_PEAKS:
                    for (int spectrogramIdx = 0; spectrogramIdx < MagnitudeSpectrograms.Length; spectrogramIdx++)
                    {
                        var spectrogramPeaks = new List<List<(int, float)>>();
                        for (int frameIdx = 0; frameIdx < MagnitudeSpectrograms[spectrogramIdx].Count; frameIdx++)
                        {
                            spectrogramPeaks.Add(new List<(int, float)>());
                        }
                        PeakMap.Add(spectrogramPeaks);
                    }
                    for (int spectrogramIdx = 0; spectrogramIdx < MagnitudeSpectrograms.Length; spectrogramIdx++)
                    {
                        for (int frameIdx = 0; frameIdx < MagnitudeSpectrograms[spectrogramIdx].Count; frameIdx++)
                        {
                            var frame = MagnitudeSpectrograms[spectrogramIdx][frameIdx];
                            var candidates = new List<(int f, float mag)>();
                            for (int f = 0; f < frame.Length; f++)
                            {
                                float mag = frame[f];
                                if (mag < BuildDB.Audio.MIN_PEAK_AMPLITUDE)
                                {
                                    continue;
                                }
                                if (BuildDB.Audio.IsLocalPeak(frame, f))
                                {
                                    candidates.Add((f, mag));
                                }

                            }
                            var top = candidates.OrderByDescending(p => p.mag).Take(BuildDB.Audio.MAX_PEAKS_PER_FRAME);
                            PeakMap[spectrogramIdx][frameIdx].AddRange(top);
                        }
                    }
                    State = BuildDB.Program.State.GENERATE_HASHES;
                    break;

                case BuildDB.Program.State.GENERATE_HASHES:
                    {
                        var Fingerprints = new List<(int hash, int offset, int trackId)>();
                        for (int trackId = 0; trackId < PeakMap.Count; trackId++)
                        {
                            for (int anchorTime = 0; anchorTime < PeakMap[trackId].Count; anchorTime++)
                            {
                                var anchorFrame = PeakMap[trackId][anchorTime];
                                foreach (var (f1, mag1) in anchorFrame)
                                {
                                    for (int t2 = anchorTime + 1; t2 <= anchorTime + BuildDB.Audio.HASH_TARGET_ZONE_TIME && t2 < PeakMap[trackId].Count; t2++)
                                    {
                                        foreach (var (f2, mag2) in PeakMap[trackId][t2])
                                        {
                                            if (Math.Abs(f2 - f1) > BuildDB.Audio.HASH_TARGET_ZONE_FREQ)
                                                continue;

                                            int deltaT = t2 - anchorTime;
                                            int hash = (f1 & 0x3FF) << 20 | (f2 & 0x3FF) << 10 | (deltaT & 0x3FF);
                                            Fingerprints.Add((hash, anchorTime, trackId));

                                            if (Fingerprints.Count > BuildDB.Audio.MAX_HASHES_PER_TRACK)
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                        var fingerprintEntries = Fingerprints.Select(f => new FingerprintEntry(f.hash, f.offset, f.trackId)).ToList();
                        var trackNames = new Dictionary<int, string>();
                        for (int i = 0; i < Files.OriginalsName.Length; i++)
                        {
                            trackNames[i] = Files.OriginalsName[i];
                        }
                        var database = new FingerprintDatabase
                        {
                            Fingerprints = fingerprintEntries,
                            TrackNames = trackNames
                        };
                        string dbPath = BuildDB.Program.RESULTS_FOLDER_NAME + "\\" + args[3] + ".msgpack";
                        var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
                        File.WriteAllBytes(dbPath, MessagePackSerializer.Serialize(database, options));
                        State = BuildDB.Program.State.CLEAN_TEMPORAL_FILES;
                        break;
                    }

                case BuildDB.Program.State.CLEAN_TEMPORAL_FILES:
                    string resultsPath = BuildDB.Program.RESULTS_FOLDER_NAME;
                    string monoPath = Path.Combine(resultsPath, BuildDB.Audio.MONO_FILES_FOLDER_NAME);
                    string spectrogramPath = Path.Combine(resultsPath, BuildDB.Audio.SPECTROGRAM_FILES_FOLDER_NAME);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    try
                    {
                        foreach (var file in Directory.GetFiles(spectrogramPath, "*", SearchOption.AllDirectories))
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                        }
                        Directory.Delete(spectrogramPath, recursive: true);
                    }
                    catch (Exception)
                    {
                    }
                    try
                    {
                        foreach (var file in Directory.GetFiles(monoPath, "*", SearchOption.AllDirectories))
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                        }
                        Directory.Delete(monoPath, recursive: true);
                    }
                    catch (Exception)
                    {
                    }
                    State = BuildDB.Program.State.SUCCESS;
                    break;

                case BuildDB.Program.State.SUCCESS:
                    execute = false;
                    break;

                case BuildDB.Program.State.ERROR:
                    execute = false;
                    break;
            }
        }
        Stopwatch.Stop();
        Console.WriteLine($"Execution time: {Stopwatch.ElapsedMilliseconds} ms");
    }
}