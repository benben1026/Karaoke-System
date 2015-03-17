using System;
namespace P2P_Karaoke_System
{
    [Serializable]
    partial class Audio
    {
        public override string ToString()
        {
            return Title+"\n"+Album+"\n"+Artist;
        }
    }
}
