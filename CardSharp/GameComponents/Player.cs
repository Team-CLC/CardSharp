﻿using System;
using System.Collections.Generic;

namespace CardSharp
{
    public class Player : MessageSenderBase, IEquatable<Player>
    {
        public override int GetHashCode()
        {
            return PlayerId.GetHashCode();
        }

        public Player(string playerId)
        {
            PlayerId = playerId;
        }

        public string PlayerId { get; }
        public List<Card> Cards { get; internal set; }
        public PlayerType Type { get; internal set; } = PlayerType.Farmer;
        public bool GiveUp { get; internal set; }
        public bool FirstBlood { get; internal set; } = true;
        public bool Multiplied { get; internal set; }
        public bool PublicCards { get; internal set; }
        public bool HostedEnabled { get; internal set; }

        public bool Equals(Player other)
        {
            return other != null &&
                   PlayerId == other.PlayerId;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Player);
        }

        public static bool operator ==(Player player1, Player player2)
        {
            return EqualityComparer<Player>.Default.Equals(player1, player2);
        }

        public static bool operator !=(Player player1, Player player2)
        {
            return !(player1 == player2);
        }

        public string ToAtCode()
        {
#if !DEBUG
            return $"[CQ:at,qq={PlayerId}]";
#else
            return $"{PlayerId}";
#endif
        }
        

        public void SendCards(Desk desk)
        {
            AddMessage($"{desk.DeskId} {Cards.ToFormatString()}");
        }
    }

    public enum PlayerType
    {
        Farmer,
        Landlord
    }
}