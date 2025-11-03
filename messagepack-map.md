# MessagePack Map Format Migration Guide

## Overview

This document outlines the migration from MessagePack **Array Format** to **Map Format** for cross-platform compatibility with the Ably protocol. The Map format allows properties to be serialized in any order, which is essential when deserializing MessagePack data from non-.NET SDKs (JavaScript, Python, etc.) or the Ably server.

## Why Migrate to Map Format?

### Current Issue (Array Format)
- Properties serialized as: `[value0, value1, value2, ...]`
- **Order-dependent**: Deserializer expects values at specific array indices
- **Problem**: Other SDKs may serialize properties in different order
- **Risk**: Deserialization fails or produces incorrect data

### Solution (Map Format)
- Properties serialized as: `{"propertyName": value, ...}`
- **Order-independent**: Deserializer looks up values by property name
- **Benefit**: Compatible with any SDK regardless of serialization order
- **Standard**: Aligns with Ably protocol specification

## Migration Strategy

### Step 1: Change MessagePackObject Attribute

**From (Array Format):**
```csharp
[MessagePackObject]
public class MyClass
```

**To (Map Format with camelCase keys):**
```csharp
[MessagePackObject(keyAsPropertyName: true)]
public class MyClass
```

### Step 2: Replace [Key(n)] with [Key("propertyName")]

**From (Integer Keys):**
```csharp
[Key(0)]
[JsonProperty("clientId")]
public string ClientId { get; set; }
```

**To (String Keys in camelCase):**
```csharp
[Key("clientId")]
[JsonProperty("clientId")]
public string ClientId { get; set; }
```

**Important:** Use the **same name** as the `[JsonProperty]` attribute to maintain consistency between JSON and MessagePack serialization.

### Step 3: Handle [IgnoreMember] Properties

Properties marked with `[IgnoreMember]` remain unchanged:

```csharp
[IgnoreMember]
[JsonIgnore]
public bool IsEmpty => ...;
```

## Classes Requiring Migration

### Core Message Types

#### 1. Message
**File:** `src/IO.Ably.Shared/Types/Message.cs`

**Current:**
```csharp
[MessagePackObject]
public class Message : IMessage
{
    [Key(0)] [JsonProperty("id")] public string Id { get; set; }
    [Key(1)] [JsonProperty("clientId")] public string ClientId { get; set; }
    [Key(2)] [JsonProperty("connectionId")] public string ConnectionId { get; set; }
    [Key(3)] [JsonProperty("connectionKey")] public string ConnectionKey { get; set; }
    [Key(4)] [JsonProperty("name")] public string Name { get; set; }
    [Key(5)] [JsonProperty("data")] public object Data { get; set; }
    [Key(6)] [JsonProperty("encoding")] public string Encoding { get; set; }
    [Key(7)] [JsonProperty("extras")] public MessageExtras Extras { get; set; }
    [Key(8)] [JsonProperty("timestamp")] public DateTimeOffset? Timestamp { get; set; }
    [IgnoreMember] [JsonIgnore] public bool IsEmpty => ...;
}
```

**Migrated:**
```csharp
[MessagePackObject(keyAsPropertyName: true)]
public class Message : IMessage
{
    [Key("id")] [JsonProperty("id")] public string Id { get; set; }
    [Key("clientId")] [JsonProperty("clientId")] public string ClientId { get; set; }
    [Key("connectionId")] [JsonProperty("connectionId")] public string ConnectionId { get; set; }
    [Key("connectionKey")] [JsonProperty("connectionKey")] public string ConnectionKey { get; set; }
    [Key("name")] [JsonProperty("name")] public string Name { get; set; }
    [Key("data")] [JsonProperty("data")] public object Data { get; set; }
    [Key("encoding")] [JsonProperty("encoding")] public string Encoding { get; set; }
    [Key("extras")] [JsonProperty("extras")] public MessageExtras Extras { get; set; }
    [Key("timestamp")] [JsonProperty("timestamp")] public DateTimeOffset? Timestamp { get; set; }
    [IgnoreMember] [JsonIgnore] public bool IsEmpty => ...;
}
```

#### 2. PresenceMessage
**File:** `src/IO.Ably.Shared/Types/PresenceMessage.cs`

**Properties to migrate:**
- `[Key("id")]` - id
- `[Key("action")]` - action
- `[Key("clientId")]` - clientId
- `[Key("connectionId")]` - connectionId
- `[Key("data")]` - data
- `[Key("encoding")]` - encoding
- `[Key("timestamp")]` - timestamp
- `[Key("memberKey")]` - memberKey (if present)

#### 3. ProtocolMessage
**File:** `src/IO.Ably.Shared/Types/ProtocolMessage.cs`

**Current:**
```csharp
[MessagePackObject(AllowPrivate = true)]
public class ProtocolMessage
```

**Migrated:**
```csharp
[MessagePackObject(keyAsPropertyName: true)]
public class ProtocolMessage
```

**Properties to migrate:**
- `[Key("params")]` - params
- `[Key("action")]` - action
- `[Key("auth")]` - auth
- `[Key("flags")]` - flags
- `[Key("count")]` - count
- `[IgnoreMember]` - error (keep as IgnoreMember)
- `[Key("id")]` - id
- `[Key("channel")]` - channel
- `[Key("channelSerial")]` - channelSerial
- `[Key("connectionId")]` - connectionId
- `[Key("msgSerial")]` - msgSerial
- `[Key("timestamp")]` - timestamp
- `[Key("messages")]` - messages
- `[Key("presence")]` - presence
- `[Key("connectionDetails")]` - connectionDetails

### Auth & Connection Types

#### 4. ConnectionDetails
**File:** `src/IO.Ably.Shared/Types/ConnectionDetails.cs`

**Properties:**
- `[Key("clientId")]` - clientId
- `[Key("connectionKey")]` - connectionKey
- `[Key("maxFrameSize")]` - maxFrameSize
- `[Key("maxInboundRate")]` - maxInboundRate
- `[Key("maxMessageSize")]` - maxMessageSize
- `[Key("serverId")]` - serverId
- `[Key("connectionStateTtl")]` - connectionStateTtl

#### 5. AuthDetails
**File:** `src/IO.Ably.Shared/Types/AuthDetails.cs`

**Properties:**
- `[Key("accessToken")]` - accessToken

#### 6. TokenRequest
**File:** `src/IO.Ably.Shared/TokenRequest.cs`

**Current:**
```csharp
[MessagePackObject(AllowPrivate = true)]
```

**Migrated:**
```csharp
[MessagePackObject(keyAsPropertyName: true)]
```

**Properties:**
- `[Key("keyName")]` - keyName
- `[Key("clientId")]` - clientId
- `[Key("nonce")]` - nonce
- `[Key("mac")]` - mac
- `[Key("capability")]` - capability
- `[Key("timestamp")]` - timestamp
- `[Key("ttl")]` - ttl

#### 7. TokenDetails
**File:** `src/IO.Ably.Shared/TokenDetails.cs`

**Current:**
```csharp
[MessagePackObject(AllowPrivate = true)]
```

**Migrated:**
```csharp
[MessagePackObject(keyAsPropertyName: true)]
```

**Properties:**
- `[Key("token")]` - token
- `[Key("expires")]` - expires
- `[Key("issued")]` - issued
- `[Key("capability")]` - capability
- `[Key("clientId")]` - clientId

### Statistics Types

#### 8. Stats
**File:** `src/IO.Ably.Shared/Statistics.cs`

**Properties:**
- `[Key("intervalId")]` - intervalId
- `[Key("inbound")]` - inbound
- `[Key("outbound")]` - outbound
- `[Key("persisted")]` - persisted
- `[Key("connections")]` - connections
- `[Key("channels")]` - channels
- `[Key("apiRequests")]` - apiRequests
- `[Key("tokenRequests")]` - tokenRequests

#### 9. ConnectionTypes
**File:** `src/IO.Ably.Shared/Statistics.cs`

**Properties:**
- `[Key("all")]` - all
- `[Key("plain")]` - plain
- `[Key("tls")]` - tls

#### 10. MessageCount
**File:** `src/IO.Ably.Shared/Statistics.cs`

**Properties:**
- `[Key("count")]` - count
- `[Key("data")]` - data

#### 11. MessageTypes
**File:** `src/IO.Ably.Shared/Statistics.cs`

**Properties:**
- `[Key("all")]` - all
- `[Key("messages")]` - messages
- `[Key("presence")]` - presence

#### 12. InboundMessageTraffic
**File:** `src/IO.Ably.Shared/Statistics.cs`

**Properties:**
- `[Key("realtime")]` - realtime
- `[Key("rest")]` - rest
- `[Key("webhook")]` - webhook
- `[Key("all")]` - all

#### 13. OutboundMessageTraffic
**File:** `src/IO.Ably.Shared/Statistics.cs`

**Properties:**
- `[Key("realtime")]` - realtime
- `[Key("rest")]` - rest
- `[Key("webhook")]` - webhook
- `[Key("push")]` - push
- `[Key("all")]` - all

#### 14. RequestCount
**File:** `src/IO.Ably.Shared/Statistics.cs`

**Properties:**
- `[Key("succeeded")]` - succeeded
- `[Key("failed")]` - failed
- `[Key("refused")]` - refused

#### 15. ResourceCount
**File:** `src/IO.Ably.Shared/Statistics.cs`

**Properties:**
- `[Key("peak")]` - peak
- `[Key("min")]` - min
- `[Key("mean")]` - mean
- `[Key("opened")]` - opened
- `[Key("refused")]` - refused

### Channel Types

#### 16. ChannelDetails
**File:** `src/IO.Ably.Shared/Rest/ChannelDetails.cs`

**Properties:**
- `[Key("channelId")]` - channelId
- `[Key("status")]` - status

#### 17. ChannelStatus
**File:** `src/IO.Ably.Shared/Rest/ChannelDetails.cs`

**Properties:**
- `[Key("isActive")]` - isActive
- `[Key("occupancy")]` - occupancy

#### 18. ChannelOccupancy
**File:** `src/IO.Ably.Shared/Rest/ChannelDetails.cs`

**Properties:**
- `[Key("metrics")]` - metrics

#### 19. ChannelMetrics
**File:** `src/IO.Ably.Shared/Rest/ChannelDetails.cs`

**Properties:**
- `[Key("connections")]` - connections
- `[Key("publishers")]` - publishers
- `[Key("subscribers")]` - subscribers
- `[Key("presenceConnections")]` - presenceConnections
- `[Key("presenceMembers")]` - presenceMembers
- `[Key("presenceSubscribers")]` - presenceSubscribers

### Push Types

#### 20. DeviceDetails
**File:** `src/IO.Ably.Shared/Push/DeviceDetails.cs`

**Properties:**
- `[Key("id")]` - id
- `[Key("platform")]` - platform
- `[Key("formFactor")]` - formFactor
- `[Key("clientId")]` - clientId
- `[Key("metadata")]` - metadata
- `[Key("push")]` - push
- `[Key("deviceSecret")]` - deviceSecret

#### 21. DeviceDetails.PushData (nested class)
**File:** `src/IO.Ably.Shared/Push/DeviceDetails.cs`

**Properties:**
- `[Key("recipient")]` - recipient
- `[Key("state")]` - state
- `[Key("errorReason")]` - errorReason

#### 22. PushChannelSubscription
**File:** `src/IO.Ably.Shared/Push/PushChannelSubscription.cs`

**Properties:**
- `[Key("channel")]` - channel
- `[Key("deviceId")]` - deviceId
- `[Key("clientId")]` - clientId

## Implementation Checklist

### Phase 1: Update Class Attributes
- [ ] Update all `[MessagePackObject]` to `[MessagePackObject(keyAsPropertyName: true)]`
- [ ] Remove `AllowPrivate = true` parameter (not needed with map format)

### Phase 2: Update Property Keys
- [ ] Replace all `[Key(n)]` with `[Key("propertyName")]` using camelCase
- [ ] Ensure key names match `[JsonProperty]` attribute values
- [ ] Keep `[IgnoreMember]` properties unchanged

### Phase 3: Testing
- [ ] Build solution and verify no compilation errors
- [ ] Run unit tests for serialization/deserialization
- [ ] Test cross-platform compatibility with other SDKs
- [ ] Verify backward compatibility with existing data (if needed)

### Phase 4: Documentation
- [ ] Update CHANGELOG.md with breaking changes
- [ ] Update README.md with new serialization format
- [ ] Document migration path for existing users

## Breaking Changes

⚠️ **WARNING:** This is a **BREAKING CHANGE** for MessagePack serialization.

### Impact:
- Existing MessagePack-serialized data will **NOT** deserialize correctly
- Binary format changes from array `[v0, v1, v2]` to map `{"k0": v0, "k1": v1}`
- Requires coordinated update across all services using MessagePack

### Migration Path:
1. **Version API endpoints** - Support both formats during transition
2. **Dual deserialization** - Try map format first, fall back to array format
3. **Gradual rollout** - Update services one at a time
4. **Data migration** - Re-serialize existing persisted data

## Benefits After Migration

✅ **Cross-platform compatibility** - Works with any SDK regardless of property order  
✅ **Protocol compliance** - Aligns with Ably protocol specification  
✅ **Flexibility** - Can add/remove properties without breaking deserialization  
✅ **Debugging** - Map format is more readable in debugging tools  
✅ **Future-proof** - Easier to extend and maintain

## References

- [MessagePack-CSharp Documentation](https://github.com/MessagePack-CSharp/MessagePack-CSharp)
- [MessagePack Specification](https://github.com/msgpack/msgpack/blob/master/spec.md)
- [Ably Protocol Specification](https://ably.com/docs/realtime/protocol)

---

**Status:** 📋 **PENDING IMPLEMENTATION**  
**Priority:** 🔴 **HIGH** - Required for cross-platform compatibility  
**Estimated Effort:** 4-6 hours
