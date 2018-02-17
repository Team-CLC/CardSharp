﻿using System;
using System.IO;

namespace CardSharp.GameComponents
{
    public class PlayerConfig : IPlayerConfig
    {
        static PlayerConfig()
        {
            if (!Directory.Exists(Constants.ConfigDir)) Directory.CreateDirectory(Constants.ConfigDir);
        }

        public PlayerConfig(string playerid, int point = default, DateTime lastTime = default)
        {
            PlayerID = playerid ?? throw new ArgumentNullException(nameof(playerid));
            Point = point;
            LastTime = lastTime;
        }

        public string PlayerID { get; }
        public int Point { get; set; }
        public DateTime LastTime { get; set; }

        public static PlayerConfig GetConfig(Player player)
        {
            var path = GetConfigPath(player.PlayerId);
            return File.Exists(path)
                ? File.ReadAllText(path).JsonDeserialize<PlayerConfig>()
                : new PlayerConfig(player.PlayerId);
        }

        public void Save()
        {
            File.WriteAllText(GetConfigPath(PlayerID), this.ToJsonString());
        }

        public void AddPoint()
        {
            Point += Constants.PointAdd;
            LastTime = DateTime.Now;
            Save();
        }

        private static string GetConfigPath(string playerid)
        {
            return Path.Combine(Constants.ConfigDir, $"{playerid}.json");
        }
    }
}