
namespace Declarations
{
    public class BuildDB
    {
        public class Audio
        {
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
        }

        public class Program
        {
            public enum State
            {
                CHECK_INPUT_PARAMETERS,
                CREATE_FOLDERS,
                READ_SONGS_LIST,
                CONVERT_SONGS_TO_MONO,
                GET_SPECTROGRAMS_IMAGES,
                GET_SPECTROGRAMS,
                SUCCESS,
                ERROR
            }
        }
    }
}
