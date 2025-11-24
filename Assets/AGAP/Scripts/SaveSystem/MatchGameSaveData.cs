using System;

namespace AGAP
{
    [Serializable]
    public class MatchGameSaveData
    {
        public int rows;
        public int columns;
        public int score;
        public int[] cardIds;
        public bool[] matchedFlags;
    }
}