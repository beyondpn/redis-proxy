// ***********************************************************
// File     : Config.cs
// Author   : beyondpn
// Created  : 2016年12月9日
// Porpuse  : FileDescription
// ***********************************************************

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace RedisProxy
{
    class Config
    {
        private Config()
        {
        }

        public string RedisHost { get; private set; }

        public int RedisPort { get; private set; }

        public int RedisDB { get; private set; }

        public string ProxyBindHost { get; private set; } = "0.0.0.0";

        public int ProxyBindPort { get; private set; } = 6379;

        private void SetConfig(string key,string value)
        {
            switch (key)
            {
                case "redis.host"   : RedisHost = value; break;
                case "redis.port"   : RedisPort = int.Parse(value); break;
                case "redis.db"     : RedisDB = int.Parse(value); break;
                case "proxy.host"   : ProxyBindHost = value; break;
                case "proxy.port"   : ProxyBindPort = int.Parse(value); break;                
            }
        }

        public static Config Load()
        {
            Config config = new Config();
            using(var reader = File.OpenText("proxy_config"))
            {
                string line;
                while((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if(line.StartsWith("#")) continue;

                    string[] array = line.Split('=');
                    if (array.Length != 2) continue;

                    string key = array[0].Trim();
                    string value = array[1].Trim();
                    config.SetConfig(key, value);
                }
            }
            return config;
        }
    }
}
