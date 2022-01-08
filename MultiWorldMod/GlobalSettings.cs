﻿namespace ItemSyncMod
{
    public class GlobalSettings
    {
        public string URL { get; set; } = "18.189.16.129";

#if (DEBUG)
        internal readonly int DefaultPort = 38282;
#else
        internal readonly int DefaultPort = 38281;
#endif

        public int ReadyID { get; set; }

        public string UserName { get; set; } = "WhoAmI";
    }
}