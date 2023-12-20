using SharpConfig;

namespace AccountCreator
{
    public class Settings
    {
        /* SAVED CONFIG VALUES */
        public Configuration config;
        public int MaxThreads;
        /* END SAVED CONFIG VALUES */

        public Settings(string file)
        {
            Configuration config = Configuration.LoadFromFile(file);
            MaxThreads = config["App"]["MaxThreads"].IntValue;
        }
    }
}
