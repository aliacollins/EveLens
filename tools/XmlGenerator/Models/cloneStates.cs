using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EveLens.XmlGenerator.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class cloneStates
    {
        [JsonProperty]
        public Dictionary<int, int> skills { get; set;  }

        [JsonProperty]
        public string internalDescription { get; set; }
    }
}
