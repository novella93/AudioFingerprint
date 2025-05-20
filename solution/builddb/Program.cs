using Declarations;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
class Program
{
    // State of the program
    static BuildDB.Program.State State = BuildDB.Program.State.CHECK_INPUT_PARAMETERS;
    // Audio files handling
    static BuildDB.Audio.Files Files = new BuildDB.Audio.Files();
    static void Main(string[] args)
    {
        bool execute = true;
        while (execute)
        {
            switch (State)
            {
                case BuildDB.Program.State.CHECK_INPUT_PARAMETERS:
                    if (args.Length == 4)
                    {
                        int validArguments = 0;
                        if (args[0] == "-i")
                        {
                            validArguments++;
                            if (Directory.Exists(args[1]))
                            {
                                validArguments++;
                            }
                            else
                            {
                                Console.WriteLine("Input path does not exist.");
                                State = BuildDB.Program.State.ERROR;
                            }
                        }
                        if (args[2] == "-o")
                        {
                            validArguments++;
                            if (args[3] != "")
                            {
                                validArguments++;

                            }
                            else
                            {
                                Console.WriteLine("Wrong output path.");
                                State = BuildDB.Program.State.ERROR;
                            }
                        }
                        if (validArguments == 4)
                        {
                            State = BuildDB.Program.State.READ_SONGS_LIST;
                        }
                        else
                        {
                            Console.WriteLine("Check parameters provided.");
                            State = BuildDB.Program.State.ERROR;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid number of arguments provided.");
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
                        Console.WriteLine("No files found in the provided song folder.");
                        State = BuildDB.Program.State.ERROR;
                    }
                    break;

                case BuildDB.Program.State.CREATE_FOLDERS:
                    if (!Directory.Exists("results"))
                    {
                        Directory.CreateDirectory("results");
                    }
                    if (!Directory.Exists("results\\mono"))
                    {
                        Directory.CreateDirectory("results\\mono");
                    }
                    State = BuildDB.Program.State.CONVERT_SONGS_TO_MONO;
                    break;

                case BuildDB.Program.State.CONVERT_SONGS_TO_MONO:
                    for (int idx = 0; idx < Files.OriginalsPath.Length; idx++)
                    {
                        using var audio = new AudioFileReader(Files.OriginalsPath[idx]);
                        if (audio.WaveFormat.Channels == (int)BuildDB.Audio.Channels.STEREO)
                        {
                            var monoProvider = new StereoToMonoSampleProvider(audio) { LeftVolume = 0.5f, RightVolume = 0.5f };
                            WaveFileWriter.CreateWaveFile16("results\\mono\\" + Files.OriginalsName[idx], monoProvider);
                        }
                        else if (audio.WaveFormat.Channels == (int)BuildDB.Audio.Channels.MONO)
                        {
                            File.Copy(Files.OriginalsPath[idx], "results\\mono\\" + Files.OriginalsName[idx]);
                        }
                        Files.MonoName[idx] = Files.OriginalsName[idx];
                        Files.MonoPath[idx] = "results\\mono\\" + Files.OriginalsName[idx];
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
    }
}