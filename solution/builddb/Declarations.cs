
namespace Declarations
{
    public class BuildDB
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
            }
            
            public const float MIN_PEAK_AMPLITUDE = 10.0f;
            public const int MAX_PEAKS_PER_FRAME = 10;

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
            public const string INPUT_PARAMETER_TOKEN = "-i";
            public const string OUTPUT_PARAMETER_TOKEN = "-o";
            public enum State
            {
                CHECK_INPUT_PARAMETERS,
                CREATE_FOLDERS,
                READ_SONGS_LIST,
                CONVERT_SONGS_TO_MONO,
                // GET_SPECTROGRAMS_IMAGES,
                GET_SPECTROGRAMS,
                FIND_PEAKS,
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
