using System.Collections.Generic;
using System;

[Serializable]
public class AuthTokenData
{
    public string cookie;
    public string socketURL;
    public string nameSpace;
}

[Serializable]
public class MessageData
{
    public string type;
    public Data payload = new();
}

[Serializable]
public class Data
{
    public int betIndex;
}

// InIt Data Classes

[Serializable]
public class Root
{
    // Initial Data
    public string id { get; set; }
    public GameData gameData { get; set; }
    public UiData uiData { get; set; }
    public Player player { get; set; }
    public Features features { get; set; }

    // Results Data
    public bool success { get; set; }
    public List<List<string>> matrix { get; set; }
    public Payload payload { get; set; }
}

[Serializable]
public class Player
{
    public double balance { get; set; }
}

[Serializable]
public class BalanceSyncPayload
{
    public double balance;
}

[Serializable]
public class GameData
{
    public List<List<int>> lines { get; set; }
    public List<double> bets { get; set; }
}

[Serializable]
public class UiData
{
    public Paylines paylines { get; set; }
}

[Serializable]
public class Paylines
{
    public List<Symbol> symbols { get; set; }
}

[Serializable]
public class Symbol
{
    public int id { get; set; }
    public string name { get; set; }
    public List<double> multiplier { get; set; }
    public string description { get; set; }
}

[Serializable]
public class Features
{
    public Jackpots jackpots { get; set; }
}

[Serializable]
public class Jackpots
{
    public double mini { get; set; }
    public double minor { get; set; }
    public double major { get; set; }
    public double grand { get; set; }
}

//Result Data Classes

[Serializable]
public class Payload
{
    public double winAmount { get; set; }

    // All list fields default to empty so missing JSON fields never produce null.
    // BonusResultData omits lineWins, mysteryRevealed, boost_positions, and bs entirely —
    // without defaults these deserialize as null and crash on .Count.
    public List<LineWin> lineWins { get; set; } = new List<LineWin>();
    public List<B> bs { get; set; } = new List<B>();
    public int bs_count { get; set; }
    public bool is_boost { get; set; }
    public List<BoostPosition> boost_positions { get; set; } = new List<BoostPosition>();
    public double boostWin { get; set; }
    public int scatterCount { get; set; }
    public bool freeSpinTriggered { get; set; }
    public List<MysteryRevealed> mysteryRevealed { get; set; } = new List<MysteryRevealed>();
    public LevelProgress levelProgress { get; set; }
    public State state { get; set; }
    public bool is_grand { get; set; }
    public double grand_win { get; set; }

}

[Serializable]
public class BoostPosition
{
    public int reel { get; set; }
    public int position { get; set; }
}

[Serializable]
public class MysteryRevealed
{
    public int reel { get; set; }
    public int position { get; set; }
    public double value { get; set; }
    public string jackpotName { get; set; }
    public bool isBoost { get; set; }
}

[Serializable]
public class LineWin
{
    public double amount { get; set; }
    public int line { get; set; }
    public int occurrences { get; set; }
    public List<List<int>> positions { get; set; }
    public int symbol { get; set; }
    public string type { get; set; }
}

[Serializable]
public class B
{
    public int reel { get; set; }
    public int position { get; set; }
    public double value { get; set; }
    public bool isJackpot { get; set; }
    public string jackpotName { get; set; }
}

[Serializable]
public class LevelProgress
{
    public int level { get; set; }
    public double level_percent { get; set; }
    public double totalMoonsCollected { get; set; }
    public double total_progress { get; set; }
}

[Serializable]
public class State
{
    public string mode { get; set; }
    public int respinsLeft { get; set; }
    public int freeSpinsLeft { get; set; }
    public double totalFreeSpinWin { get; set; }                                        // added — present in BonusResultData
    public List<LockedSymbol> lockedSymbols { get; set; } = new List<LockedSymbol>();
    public List<string> fortuneSlideQueue { get; set; } = new List<string>();
}

[Serializable]
public class LockedSymbol
{
    public int reel { get; set; }
    public int position { get; set; }
    public double value { get; set; }
    public bool isJackpot { get; set; }
    public string jackpotName { get; set; }
    public bool isMystery { get; set; }
    public bool isBoost { get; set; }
}