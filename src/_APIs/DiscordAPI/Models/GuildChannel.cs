﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordAPI.Models
{
    public class GuildChannel : Channel
    {

        [JsonProperty("parent_id")]
        public string ParentId { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("is_private")]
        public bool Private { get; set; }

        [JsonProperty("permission_overwrites")]
        public IEnumerable<Overwrite> PermissionOverwrites { get; set; }

        [JsonProperty("topic")]
        public string Topic { get; set; }

        [JsonProperty("bitrate")]
        public int Bitrate { get; set; }

        [JsonProperty("user_limit")]
        public string UserLimit { get; set; }

        [JsonProperty("nsfw")]
        public bool NSFW { get; set; }
    }
}
