using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Scripts/Core/CardEnums.cs
namespace CardGame
{
    public enum CardSuit
    {
        Club,
        Diamond,
        Heart,
        Spade,
        Joker
    }

    public enum CardRank
    {
        Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten,
        Jack,
        Queen, 
        King, 
        Ace,
        SmallJoker,
        BigJoker
    }
}