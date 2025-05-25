using MessagePack;

namespace Declarations
{
    [MessagePackObject]
    public record FingerprintEntry(
        [property: Key(0)] int Hash,
        [property: Key(1)] int Offset,
        [property: Key(2)] int TrackId
    );

    [MessagePackObject]
    public class FingerprintDatabase
    {
        [Key(0)]
        public List<FingerprintEntry> Fingerprints { get; set; }

        [Key(1)]
        public Dictionary<int, string> TrackNames { get; set; }
    }


    public class Identify
    {

        public class Audio
        {
            public const string MONO_FILES_FOLDER_NAME = "mono";
            public const string SPECTROGRAM_FILES_FOLDER_NAME = "spectrograms";
            public const int SPECTROGRAM_WINDOWS_SIZE = 8192;
            public const int SPECTROGRAM_HOP_SIZE = 4096;
            public enum Channels
            {
                MONO = 1,
                STEREO = 2
            }

            public class Files
            {
                public string[] OriginalsPath = { };
                public string[] OriginalsName = { };
                public string[] MonoName = { };
                public string[] MonoPath = { };
                public string DBPath = "";
            }

            public const float MIN_PEAK_AMPLITUDE = 10.0f;
            public const int MAX_PEAKS_PER_FRAME = 10;
            public const int HASH_TARGET_ZONE_TIME = 50;
            public const int HASH_TARGET_ZONE_FREQ = 50;
            public const int MAX_HASHES_PER_TRACK = 10000;

            public static bool IsLocalPeak(float[] frame, int frameIdx)
            {
                float current = frame[frameIdx];
                for (int i = -2; i <= 2; i++)
                {
                    if (i == 0) continue;
                    int idx = frameIdx + i;
                    if (idx < 0 || idx >= frame.Length) continue;
                    if (frame[idx] >= current)
                        return false;
                }
                return true;
            }
        }

        public class Program
        {
            public const string RESULTS_FOLDER_NAME = "results";
            public const string DATABASE_PARAMETER_TOKEN = "-d";
            public const string INPUT_PARAMETER_TOKEN = "-i";
            public enum State
            {
                CHECK_INPUT_PARAMETERS,
                CREATE_FOLDERS,
                READ_SONG,
                CONVERT_SONG_TO_MONO,
                GET_SPECTROGRAMS,
                FIND_PEAKS,
                GENERATE_HASHES,
                LOAD_DB,
                MATCH_HASHES,
                SUCCESS,
                ERROR
            }

            public class ErrorTexts
            {
                public const string ERROR_INPUT_PATH_DOES_NOT_EXIST = "Input path does not exist.";
                public const string ERROR_INVALID_OUTPUT_PATH = "Invalid output path.";
                public const string ERROR_INVALID_NUMBER_OF_ARGUMENTS = "Invalid number of arguments provided.";
                public const string ERROR_INVALID_ARGUMENTS = "Check parameters provided.";
                public const string ERROR_NO_AUDIO_FILES_FOUND = "No files found in the provided song folder.";

            }
        }
    }
}
