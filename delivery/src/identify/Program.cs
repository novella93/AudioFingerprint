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
    static Identify.Program.State State = Identify.Program.State.CHECK_INPUT_PARAMETERS;
    static Identify.Audio.Files Files = new Identify.Audio.Files();
    static List<float[]>[] MagnitudeSpectrograms = new List<float[]>[0];
    static List<List<List<(int, float)>>> PeakMap = new List<List<List<(int, float)>>>();
    static List<FingerprintEntry> FingerprintEntries = new();
    static List<FingerprintEntry> DB = new();
    static Dictionary<int, string> TrackNames = new();
    static void Main(string[] args)
    {
        bool execute = true;
        Stopwatch.Start();
        while (execute)
        {
            switch (State)
            {
                case Identify.Program.State.CHECK_INPUT_PARAMETERS:
                    if (args.Length == 4)
                    {
                        int validArguments = 0;
                        if (args[0] == Identify.Program.DATABASE_PARAMETER_TOKEN)
                        {
                            validArguments++;
                            if (File.Exists(args[1]))
                            {
                                validArguments++;
                            }
                            else
                            {
                                Console.WriteLine(Identify.Program.ErrorTexts.ERROR_INPUT_PATH_DOES_NOT_EXIST);
                                State = Identify.Program.State.ERROR;
                            }
                        }
                        if (args[2] == Identify.Program.INPUT_PARAMETER_TOKEN)
                        {
                            validArguments++;
                            if (args[3] != "")
                            {
                                validArguments++;

                            }
                            else
                            {
                                Console.WriteLine(Identify.Program.ErrorTexts.ERROR_INPUT_PATH_DOES_NOT_EXIST);
                                State = Identify.Program.State.ERROR;
                            }
                        }
                        if (validArguments == 4)
                        {
                            State = Identify.Program.State.READ_SONG;
                        }
                        else
                        {
                            Console.WriteLine(Identify.Program.ErrorTexts.ERROR_INVALID_ARGUMENTS);
                            State = Identify.Program.State.ERROR;
                        }
                    }
                    else
                    {
                        Console.WriteLine(Identify.Program.ErrorTexts.ERROR_INVALID_NUMBER_OF_ARGUMENTS);
                        State = Identify.Program.State.ERROR;
                    }
                    break;

                case Identify.Program.State.READ_SONG:
                    Files.OriginalsPath = new string[1];
                    Files.OriginalsName = new string[1];
                    Files.OriginalsPath[0] = args[3];
                    Files.OriginalsName[0] = Path.GetFileNameWithoutExtension(Files.OriginalsPath[0]);
                    Files.MonoPath = new string[Files.OriginalsPath.Length];
                    Files.MonoName = new string[Files.OriginalsName.Length];
                    if (Files.OriginalsPath.Length > 0)
                    {
                        State = Identify.Program.State.CREATE_FOLDERS;
                    }
                    else
                    {
                        Console.WriteLine(Identify.Program.ErrorTexts.ERROR_NO_AUDIO_FILES_FOUND);
                        State = Identify.Program.State.ERROR;
                    }
                    break;

                case Identify.Program.State.CREATE_FOLDERS:
                    if (!Directory.Exists(Identify.Program.RESULTS_FOLDER_NAME))
                    {
                        Directory.CreateDirectory(Identify.Program.RESULTS_FOLDER_NAME);
                    }
                    if (!Directory.Exists(Identify.Program.RESULTS_FOLDER_NAME + "\\" + Identify.Audio.MONO_FILES_FOLDER_NAME))
                    {
                        Directory.CreateDirectory(Identify.Program.RESULTS_FOLDER_NAME + "\\" + Identify.Audio.MONO_FILES_FOLDER_NAME);
                    }
                    if (!Directory.Exists(Identify.Program.RESULTS_FOLDER_NAME + "\\" + Identify.Audio.SPECTROGRAM_FILES_FOLDER_NAME))
                    {
                        Directory.CreateDirectory(Identify.Program.RESULTS_FOLDER_NAME + "\\" + Identify.Audio.SPECTROGRAM_FILES_FOLDER_NAME);
                    }
                    State = Identify.Program.State.CONVERT_SONG_TO_MONO;
                    break;


                case Identify.Program.State.CONVERT_SONG_TO_MONO:
                    Files.MonoPath = new string[Files.OriginalsPath.Length];
                    Files.MonoName = new string[Files.OriginalsName.Length];
                    if (!File.Exists(Identify.Program.RESULTS_FOLDER_NAME + "\\" + Identify.Audio.MONO_FILES_FOLDER_NAME + "\\" + Files.OriginalsName[0]))
                    {
                        using var audio = new AudioFileReader(Files.OriginalsPath[0]);

                        if (audio.WaveFormat.Channels == (int)Identify.Audio.Channels.STEREO)
                        {
                            var monoProvider = new StereoToMonoSampleProvider(audio) { LeftVolume = 0.5f, RightVolume = 0.5f };
                            WaveFileWriter.CreateWaveFile16(Identify.Program.RESULTS_FOLDER_NAME + "\\" + Identify.Audio.MONO_FILES_FOLDER_NAME + "\\" + Files.OriginalsName[0], monoProvider);
                        }
                        else if (audio.WaveFormat.Channels == (int)Identify.Audio.Channels.MONO)
                        {
                            File.Copy(Files.OriginalsPath[0], Identify.Program.RESULTS_FOLDER_NAME + "\\" + Identify.Audio.MONO_FILES_FOLDER_NAME + "\\" + Files.OriginalsName[0]);
                        }
                    }
                    Files.MonoName[0] = Files.OriginalsName[0];
                    Files.MonoPath[0] = Identify.Program.RESULTS_FOLDER_NAME + "\\" + Identify.Audio.MONO_FILES_FOLDER_NAME + "\\" + Files.OriginalsName[0];
                    State = Identify.Program.State.GET_SPECTROGRAMS;
                    break;

                case Identify.Program.State.GET_SPECTROGRAMS:
                    Array.Resize(ref MagnitudeSpectrograms, Files.MonoPath.Length);
                    if (!File.Exists(Identify.Program.RESULTS_FOLDER_NAME + "\\" + Identify.Audio.SPECTROGRAM_FILES_FOLDER_NAME + "\\" + Files.MonoName[0] + ".msgpack"))
                    {
                        var fileStream = File.OpenRead(Files.MonoPath[0]);
                        var audio = new WaveFile(fileStream);
                        var signal = audio[Channels.Left];
                        int windowSize = Identify.Audio.SPECTROGRAM_WINDOWS_SIZE;
                        int hopSize = Identify.Audio.SPECTROGRAM_HOP_SIZE;
                        var stft = new Stft(windowSize, hopSize, WindowType.Hann);
                        MagnitudePhaseList magPhaseSpectrogram = stft.MagnitudePhaseSpectrogram(signal);
                        MagnitudeSpectrograms[0] = magPhaseSpectrogram.Magnitudes;
                        File.WriteAllBytes(Identify.Program.RESULTS_FOLDER_NAME + "\\" + Identify.Audio.SPECTROGRAM_FILES_FOLDER_NAME + "\\" + Files.MonoName[0] + ".msgpack", MessagePackSerializer.Serialize(MagnitudeSpectrograms[0]));
                    }
                    else
                    {
                        MagnitudeSpectrograms[0] = MessagePackSerializer.Deserialize<List<float[]>>(File.ReadAllBytes(Identify.Program.RESULTS_FOLDER_NAME + "\\" + Identify.Audio.SPECTROGRAM_FILES_FOLDER_NAME + "\\" + Files.MonoName[0] + ".msgpack"));
                    }
                    State = Identify.Program.State.FIND_PEAKS;
                    break;

                case Identify.Program.State.FIND_PEAKS:
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
                                if (mag < Identify.Audio.MIN_PEAK_AMPLITUDE)
                                {
                                    continue;
                                }
                                if (Identify.Audio.IsLocalPeak(frame, f))
                                {
                                    candidates.Add((f, mag));
                                }

                            }
                            var top = candidates.OrderByDescending(p => p.mag).Take(Identify.Audio.MAX_PEAKS_PER_FRAME);
                            PeakMap[spectrogramIdx][frameIdx].AddRange(top);
                        }
                    }
                    State = Identify.Program.State.GENERATE_HASHES;
                    break;

                case Identify.Program.State.GENERATE_HASHES:
                    var Fingerprints = new List<(int hash, int offset, int trackId)>();
                    for (int trackId = 0; trackId < PeakMap.Count; trackId++)
                    {
                        for (int anchorTime = 0; anchorTime < PeakMap[trackId].Count; anchorTime++)
                        {
                            var anchorFrame = PeakMap[trackId][anchorTime];
                            foreach (var (f1, mag1) in anchorFrame)
                            {
                                for (int t2 = anchorTime + 1; t2 <= anchorTime + Identify.Audio.HASH_TARGET_ZONE_TIME && t2 < PeakMap[trackId].Count; t2++)
                                {
                                    foreach (var (f2, mag2) in PeakMap[trackId][t2])
                                    {
                                        if (Math.Abs(f2 - f1) > Identify.Audio.HASH_TARGET_ZONE_FREQ)
                                        {
                                            continue;
                                        }
                                        int deltaT = t2 - anchorTime;
                                        int hash = (f1 & 0x3FF) << 20 | (f2 & 0x3FF) << 10 | (deltaT & 0x3FF);
                                        Fingerprints.Add((hash, anchorTime, trackId));
                                        if (Fingerprints.Count > Identify.Audio.MAX_HASHES_PER_TRACK)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    FingerprintEntries = Fingerprints.Select(f => new FingerprintEntry(f.hash, f.offset, f.trackId)).ToList();
                    // string jsonPath = Identify.Program.RESULTS_FOLDER_NAME + "\\" + Files.MonoName[0] + ".json";
                    // using (FileStream stream = File.Create(jsonPath))
                    // {
                    //     var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                    //     JsonSerializer.SerializeAsync(stream, FingerprintEntries, jsonOptions).GetAwaiter().GetResult();
                    // }

                    State = Identify.Program.State.LOAD_DB;
                    break;

                case Identify.Program.State.LOAD_DB:
                    Files.DBPath = args[1];
                    var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
                    FingerprintDatabase database = MessagePackSerializer.Deserialize<FingerprintDatabase>(File.ReadAllBytes(args[1]), options);
                    DB = database.Fingerprints;
                    TrackNames = database.TrackNames;
                    State = Identify.Program.State.MATCH_HASHES;
                    break;

                case Identify.Program.State.MATCH_HASHES:
                    var sampleHashes = (FingerprintEntries ?? new List<FingerprintEntry>()).Select(f => (f.Hash, f.Offset)).ToList();
                    var hashIndex = DB.GroupBy(fp => fp.Hash).ToDictionary(g => g.Key, g => g.Select(fp => (fp.TrackId, fp.Offset)).ToList());
                    var votes = new Dictionary<(int trackId, int deltaOffset), int>();
                    foreach (var (hash, sampleOffset) in sampleHashes)
                    {
                        if (hashIndex.TryGetValue(hash, out var matches))
                        {
                            foreach (var (trackId, dbOffset) in matches)
                            {
                                int deltaOffset = dbOffset - sampleOffset;
                                var key = (trackId, deltaOffset);

                                if (votes.ContainsKey(key))
                                    votes[key]++;
                                else
                                    votes[key] = 1;
                            }
                        }
                    }
                    var best = votes.GroupBy(v => v.Key.trackId).Select(g => new { TrackId = g.Key, MaxVotes = g.Max(v => v.Value) }).OrderByDescending(x => x.MaxVotes).FirstOrDefault();
                    if (best != null)
                    {
                        Console.WriteLine($"Best match: trackId = {best.TrackId}, Matches = {best.MaxVotes}");

                        if (TrackNames.TryGetValue(best.TrackId, out var name))
                        {
                            Console.WriteLine($"Filename: {name}");
                        }
                        else
                        {
                            Console.WriteLine("Track name not found in database.");
                        }

                    }
                    else
                    {
                        Console.WriteLine("No match found.");
                    }
                    State = Identify.Program.State.CLEAN_TEMPORAL_FILES;
                    break;

                case Identify.Program.State.CLEAN_TEMPORAL_FILES:
                    string resultsPath = Identify.Program.RESULTS_FOLDER_NAME;
                    string monoPath = Path.Combine(resultsPath, Identify.Audio.MONO_FILES_FOLDER_NAME);
                    string spectrogramPath = Path.Combine(resultsPath, Identify.Audio.SPECTROGRAM_FILES_FOLDER_NAME);
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
                    State = Identify.Program.State.SUCCESS;
                    break;

                case Identify.Program.State.SUCCESS:
                    execute = false;
                    break;

                case Identify.Program.State.ERROR:
                    execute = false;
                    break;
            }
        }
        Stopwatch.Stop();
        Console.WriteLine($"Execution time: {Stopwatch.ElapsedMilliseconds} ms");
    }
}