using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Pitempmqtt.Models;

internal record AnnouncePayload(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("mac")] string Mac,
    [property: JsonPropertyName("ip")] string Ip
 );

