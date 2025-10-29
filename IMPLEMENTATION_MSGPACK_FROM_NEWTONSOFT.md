# MsgPack Serialization Implementation Plan

## Overview
This document outlines the implementation plan to align MsgPack serialization with Newtonsoft JSON serialization in the ably-dotnet project. The goal is to ensure feature parity and eliminate committed auto-generated code by generating serializers at compile time.

---

## Current State Analysis

### ✅ What's Working
- **Type-specific custom serializers exist** for `DateTimeOffset`, `TimeSpan`, and `Capability`
- **SerializationContext** properly configured in [`MsgPackHelper.cs`](src/IO.Ably.Shared.MsgPack/MsgPackHelper.cs)
- **Generated serializers** for 19 model types currently exist:
  - Core messages: `Message`, `PresenceMessage`, `ProtocolMessage`
  - Auth/Connection: `TokenDetails`, `TokenRequest`, `ConnectionDetails`, `ErrorInfo`
  - Statistics: `Stats`, `MessageCount`, `MessageTypes`, `RequestCount`, `ResourceCount`, `ConnectionTypes`, `InboundMessageTraffic`, `OutboundMessageTraffic`
  - Enums: `PresenceAction`, `ProtocolMessage.MessageAction`, `HttpStatusCode`
  - Other: `Capability`

### ❌ Issues Identified
1. **Missing `MessageExtras` serializer** - JSON has [`MessageExtrasConverter`](src/IO.Ably.Shared/CustomSerialisers/MessageExtrasConverter.cs) but MsgPack doesn't
   - **CONFIRMED**: No MessageExtras serializer registered in MsgPackHelper.cs
   - MessageExtras uses JToken internally and has a custom JSON converter
2. **Auto-generated code committed** - ~290 lines per serializer in version control (not 585+ as initially stated)
   - **CONFIRMED**: Generated serializers are committed to the repository
3. **Manual generation process** - Serializers generated via skipped test in [`MsgPackMessageSerializerTests.cs`](src/IO.Ably.Tests.Shared/MsgPackMessageSerializerTests.cs:16)
   - **CONFIRMED**: Test at line 16 is marked with `[Fact(Skip = "true")]`
4. **Missing serializers** for additional classes with `[JsonProperty]` attributes:
   - `AuthDetails` - Used in `ProtocolMessage.Auth` (**No serializer found**)
   - `ChannelParams` - Used in `ProtocolMessage.Params` (extends Dictionary) (**No serializer found**)
   - `DeltaExtras` - Part of `MessageExtras` (defined as nested class in MessageExtras.cs) (**No serializer found**)
   - `RecoveryKeyContext` - Used for connection recovery (**No serializer found**)
   - `ChannelDetails`, `ChannelStatus`, `ChannelOccupancy`, `ChannelMetrics` - Channel metadata types (**No serializers found**)
   - Push-related classes: `DeviceDetails`, `PushChannelSubscription` (**No serializers found**)

### 📝 Important Note About MsgPack Attributes

**Critical Distinction:**
- **Runtime Serialization:** MsgPack.Cli requires `[MessagePackObject]` and `[Key]` attributes (does NOT respect Newtonsoft attributes)
- **Generated Serializers (Our Approach):** `SerializerGenerator` uses reflection at **generation time** to create serializer code, so attributes are optional

**Current Implementation:**
- ✅ Uses `SerializerGenerator.GenerateCode()` to create custom serializer classes
- ✅ Generator inspects types via reflection when generating code
- ✅ Models have NO MsgPack attributes currently and work fine
- ✅ Generated serializers are compiled code, not runtime reflection
- ✅ Generated code handles null checks and uses JsonProperty names for field mapping

**Recommendation:** While MsgPack attributes aren't strictly required for generated serializers, adding them provides:
- Better documentation of serialization intent
- Explicit control over property ordering
- Future-proofing if switching to runtime serialization
- Consistency with MsgPack best practices

**Note:** The generated serializers already respect JsonProperty names (e.g., "clientId", "connectionId") as seen in the generated code.

---

## Implementation Tasks

## 1. Add Missing MessageExtras Serializer

### 1.1 Create MessageExtrasMessagePackSerializer

**File:** `src/IO.Ably.Shared.MsgPack/CustomSerialisers/MessageExtrasMessagePackSerializer.cs`

```csharp
using IO.Ably.Types;
using MsgPack;
using MsgPack.Serialization;
using Newtonsoft.Json.Linq;

namespace IO.Ably.CustomSerialisers
{
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class MessageExtrasMessagePackSerializer : MessagePackSerializer<MessageExtras>
    {
        public MessageExtrasMessagePackSerializer(SerializationContext ownerContext)
            : base(ownerContext)
        {
        }

        protected override void PackToCore(Packer packer, MessageExtras objectTree)
        {
            if (objectTree == null)
            {
                packer.PackNull();
                return;
            }

            var json = objectTree.ToJson();
            if (json == null)
            {
                packer.PackNull();
            }
            else
            {
                // Serialize as JSON string for compatibility
                packer.Pack(json.ToString(Newtonsoft.Json.Formatting.None));
            }
        }

        protected override MessageExtras UnpackFromCore(Unpacker unpacker)
        {
            if (unpacker.LastReadData.IsNil)
            {
                return null;
            }

            var jsonString = unpacker.LastReadData.AsString();
            if (string.IsNullOrEmpty(jsonString))
            {
                return null;
            }

            var jToken = JToken.Parse(jsonString);
            return MessageExtras.From(jToken);
        }
    }
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
```

**Note:** MessageExtras has a complex structure with JToken internally and a nested DeltaExtras class. The approach of serializing as JSON string ensures compatibility with the existing JSON serialization behavior.

### 1.2 Register in MsgPackHelper

**File:** `src/IO.Ably.Shared.MsgPack/MsgPackHelper.cs`

Add registration after line 23:

```csharp
context.Serializers.Register(new MessageExtrasMessagePackSerializer(context));
```

### 1.3 Update Project File

**File:** `src/IO.Ably.Shared.MsgPack/IO.Ably.Shared.MsgPack.projitems`

Add after line 13:

```xml
<Compile Include="$(MSBuildThisFileDirectory)CustomSerialisers\MessageExtrasMessagePackSerializer.cs" />
```

---

## 2. Generate Serializers at Compile Time

### 2.1 Create MsgPack Generator Tool Project

**Directory:** `tools/MsgPackSerializerGenerator/`

**File:** `tools/MsgPackSerializerGenerator/MsgPackSerializerGenerator.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MsgPack.Cli" Version="1.0.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\IO.Ably.NetStandard20\IO.Ably.NetStandard20.csproj" />
  </ItemGroup>
</Project>
```

**File:** `tools/MsgPackSerializerGenerator/Program.cs`

```csharp
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using IO.Ably;
using IO.Ably.Types;
using IO.Ably.Push;
using IO.Ably.Rest;
using IO.Ably.Realtime;
using MsgPack.Serialization;

namespace MsgPackSerializerGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: MsgPackSerializerGenerator <output-directory>");
                return;
            }

            var outputDirectory = args[0];
            Console.WriteLine($"Generating MsgPack serializers to: {outputDirectory}");

            // Ensure output directory exists
            Directory.CreateDirectory(outputDirectory);

            var applicationLibraryAssembly = typeof(ProtocolMessage).Assembly;
            
            // Types to generate serializers for
            var typesToGenerate = new[]
            {
                // Core message types
                typeof(Message),
                typeof(PresenceMessage),
                typeof(ProtocolMessage),
                typeof(ProtocolMessage.MessageAction),
                typeof(PresenceAction),
                
                // Auth and connection types
                typeof(TokenRequest),
                typeof(TokenDetails),
                typeof(ConnectionDetails),
                typeof(ErrorInfo),
                typeof(Capability),
                typeof(AuthDetails),
                
                // Statistics types
                typeof(Stats),
                typeof(MessageCount),
                typeof(MessageTypes),
                typeof(RequestCount),
                typeof(ResourceCount),
                typeof(ConnectionTypes),
                typeof(OutboundMessageTraffic),
                typeof(InboundMessageTraffic),
                
                // Additional types
                typeof(ChannelParams),
                typeof(MessageExtras.DeltaExtras), // Note: DeltaExtras is a nested class
                typeof(RecoveryKeyContext),
                typeof(System.Net.HttpStatusCode),
                
                // Channel metadata types
                typeof(ChannelDetails),
                typeof(ChannelStatus),
                typeof(ChannelOccupancy),
                typeof(ChannelMetrics),
                
                // Push-related types (include if push is used)
                typeof(DeviceDetails),
                typeof(DeviceDetails.PushData),
                typeof(PushChannelSubscription),
            };

            SerializerGenerator.GenerateCode(
                new SerializerCodeGenerationConfiguration
                {
                    Namespace = "IO.Ably.CustomSerialisers",
                    OutputDirectory = outputDirectory,
                    EnumSerializationMethod = EnumSerializationMethod.ByUnderlyingValue,
                    IsRecursive = true,
                    PreferReflectionBasedSerializer = false,
                    SerializationMethod = SerializationMethod.Map
                },
                typesToGenerate);

            Console.WriteLine("Serializer generation complete!");
        }
    }
}
```

### 2.2 Add MSBuild Target to Generate Serializers

**File:** `src/IO.Ably.Shared.MsgPack/IO.Ably.Shared.MsgPack.targets`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  
  <PropertyGroup>
    <MsgPackGeneratorProject>$(MSBuildThisFileDirectory)..\..\tools\MsgPackSerializerGenerator\MsgPackSerializerGenerator.csproj</MsgPackGeneratorProject>
    <MsgPackOutputDirectory>$(MSBuildThisFileDirectory)CustomSerialisers\GeneratedSerializers</MsgPackOutputDirectory>
  </PropertyGroup>

  <!-- Generate MsgPack serializers before compilation -->
  <Target Name="GenerateMsgPackSerializers" BeforeTargets="CoreCompile" Condition="'$(DesignTimeBuild)' != 'true' AND '$(BuildingProject)' == 'true'">
    <Message Text="Generating MsgPack serializers..." Importance="high" />
    
    <!-- Build the generator tool first -->
    <MSBuild Projects="$(MsgPackGeneratorProject)" Targets="Build" Properties="Configuration=$(Configuration)" />
    
    <!-- Run the generator tool -->
    <Exec Command="dotnet &quot;$(MSBuildThisFileDirectory)..\..\tools\MsgPackSerializerGenerator\bin\$(Configuration)\net6.0\MsgPackSerializerGenerator.dll&quot; &quot;$(MsgPackOutputDirectory)&quot;"
          WorkingDirectory="$(MSBuildThisFileDirectory)" />
    
    <Message Text="MsgPack serializers generated successfully" Importance="high" />
  </Target>

  <!-- Clean generated files -->
  <Target Name="CleanMsgPackSerializers" BeforeTargets="CoreClean">
    <ItemGroup>
      <GeneratedSerializers Include="$(MsgPackOutputDirectory)\*.cs" />
    </ItemGroup>
    <Delete Files="@(GeneratedSerializers)" />
  </Target>

  <!-- Include generated files in compilation -->
  <ItemGroup>
    <Compile Include="$(MsgPackOutputDirectory)\*.cs" />
  </ItemGroup>

</Project>
```

### 2.3 Import Targets in Project Files

**File:** `src/IO.Ably.NetStandard20/IO.Ably.NetStandard20.csproj`

Add before closing `</Project>` tag:

```xml
  <!-- Import MsgPack serializer generation targets -->
  <Import Project="..\IO.Ably.Shared.MsgPack\IO.Ably.Shared.MsgPack.targets" Condition="Exists('..\IO.Ably.Shared.MsgPack\IO.Ably.Shared.MsgPack.targets')" />
```

**Note:** Also add to other project files that use MsgPack serialization:
- `src/IO.Ably.NETFramework/IO.Ably.NETFramework.csproj`
- `src/IO.Ably.Android/IO.Ably.Android.csproj`
- `src/IO.Ably.iOS/IO.Ably.iOS.csproj`

### 2.4 Update .gitignore

**File:** `.gitignore`

Add at the end of the file:

```gitignore
# MsgPack Generated Serializers
src/IO.Ably.Shared.MsgPack/CustomSerialisers/GeneratedSerializers/*.cs
!src/IO.Ably.Shared.MsgPack/CustomSerialisers/GeneratedSerializers/.gitkeep
```

**File:** `src/IO.Ably.Shared.MsgPack/CustomSerialisers/GeneratedSerializers/.gitkeep`

Create empty file to preserve directory structure.

**Important:** Before implementing this change, you'll need to:
1. Remove all existing generated serializer files from git tracking
2. Run `git rm --cached src/IO.Ably.Shared.MsgPack/CustomSerialisers/GeneratedSerializers/*.cs`
3. Commit the removal
4. Add the .gitignore entries
5. Add the .gitkeep file

---

## 3. Add MsgPack Attributes to Models (Recommended Best Practice)

### 3.1 Understanding MsgPack.Cli Attribute Behavior

**Important Facts:**
1. **MsgPack.Cli does NOT respect Newtonsoft.Json attributes** (`[JsonProperty]`, `[JsonIgnore]`)
2. **Runtime serialization requires** `[MessagePackObject]` and `[Key]` attributes
3. **Generated serializers (our approach)** use reflection at generation time, making attributes optional but recommended

### 3.2 Why Add Attributes Despite Using Generated Serializers?

**Benefits:**
- ✅ **Explicit documentation** - Clear intent for serialization
- ✅ **Property ordering control** - `[Key(0)]`, `[Key(1)]` defines order
- ✅ **Ignore properties** - `[IgnoreMember]` for computed properties
- ✅ **Future-proofing** - If switching to runtime serialization
- ✅ **Best practices** - Aligns with MsgPack.Cli conventions
- ✅ **Dual compatibility** - Keep both JSON and MsgPack attributes

### 3.3 Current Model State

All models currently have:
- ✅ `[JsonProperty("propertyName")]` for JSON serialization
- ✅ `[JsonIgnore]` for excluded properties
- ❌ NO MsgPack attributes (works but not ideal)

### 3.4 Recommended Annotation Pattern

Add MsgPack attributes **alongside** existing JSON attributes:

```csharp
using MsgPack.Serialization;
using Newtonsoft.Json;

[MessagePackObject]  // MsgPack class marker
public class Message : IMessage
{
    [Key(0)]  // MsgPack property index
    [JsonProperty("id")]  // JSON property name
    public string Id { get; set; }

    [Key(1)]
    [JsonProperty("clientId")]
    public string ClientId { get; set; }

    [Key(2)]
    [JsonProperty("connectionId")]
    public string ConnectionId { get; set; }

    [Key(3)]
    [JsonProperty("connectionKey")]
    public string ConnectionKey { get; set; }

    [Key(4)]
    [JsonProperty("name")]
    public string Name { get; set; }

    [Key(5)]
    [JsonProperty("timestamp")]
    public DateTimeOffset? Timestamp { get; set; }

    [Key(6)]
    [JsonProperty("data")]
    [JsonConverter(typeof(MessageDataConverter))]
    public object Data { get; set; }

    [Key(7)]
    [JsonProperty("extras")]
    public MessageExtras Extras { get; set; }

    [Key(8)]
    [JsonProperty("encoding")]
    public string Encoding { get; set; }

    [IgnoreMember]  // MsgPack ignore
    [JsonIgnore]    // JSON ignore
    public bool IsEmpty => Equals(this, DefaultInstance);
}
```

### 3.5 Implementation Strategy

**Phase 3A: Core Models** (Priority: HIGH)
- Add `[MessagePackObject]` to class
- Add `[Key(index)]` to each serializable property
- Add `[IgnoreMember]` to computed/ignored properties
- Keep all existing `[JsonProperty]` and `[JsonIgnore]` attributes

**Models to Annotate:**

**Core Message Types:**
1. `Message` - 9 properties (id, clientId, connectionId, connectionKey, name, timestamp, data, extras, encoding)
2. `PresenceMessage` - 8 properties (id, action, clientId, connectionId, connectionKey, data, encoding, timestamp)
3. `ProtocolMessage` - 14 properties (action, auth, flags, count, error, id, channel, channelSerial, connectionId, msgSerial, timestamp, messages, presence, connectionDetails, params)
4. `MessageExtras` - Custom serializer (no attributes needed - uses JToken internally)
5. `DeltaExtras` - 2 properties (format, from) - nested class in MessageExtras

**Auth & Connection Types:**
6. `ErrorInfo` - 5 properties (code, statusCode, message, cause, href)
7. `ConnectionDetails` - 7 properties (clientId, connectionKey, maxFrameSize, maxInboundRate, maxMessageSize, serverId, connectionStateTtl)
8. `TokenRequest` - Properties need verification (file not found in Types folder, likely in root Shared folder)
9. `TokenDetails` - Properties need verification (file not found in Types folder, likely in root Shared folder)
10. `Capability` - Has custom serializer already
11. `AuthDetails` - 1 property (accessToken)

**Statistics Types:**
12. `Stats` - 3 properties + nested types
13. `MessageCount` - 2 properties
14. `MessageTypes` - 3 properties
15. `RequestCount` - 3 properties
16. `ResourceCount` - 7 properties
17. `ConnectionTypes` - 3 properties
18. `InboundMessageTraffic` - 3 properties
19. `OutboundMessageTraffic` - 3 properties

**Additional Types:**
20. `ChannelParams` - Dictionary-based (extends Dictionary<string, string>, may need custom serializer)
21. `RecoveryKeyContext` - Properties need verification (internal class)
22. `ChannelDetails` - Properties need verification
23. `ChannelStatus` - Properties need verification
24. `ChannelOccupancy` - Properties need verification
25. `ChannelMetrics` - Properties need verification

**Push Types (if used):**
26. `DeviceDetails` - 7 properties (id, platform, formFactor, clientId, metadata, push, deviceSecret)
27. `DeviceDetails.PushData` - 3 properties (recipient, state, errorReason)
28. `PushChannelSubscription` - Properties need verification

**Phase 3B: Validation** (Priority: MEDIUM)
- Verify generated serializers still work
- Confirm no breaking changes
- Test serialization output matches

---

## 4. Testing & Validation

### 4.1 Update Test to Validate Generation

**File:** `src/IO.Ably.Tests.Shared/MsgPackMessageSerializerTests.cs`

Remove the skipped `Generate()` test (line 15-34) and replace with validation test:

```csharp
[Fact]
public void ValidateGeneratedSerializersExist()
{
    // Use reflection to access the private Context field
    var contextField = typeof(MsgPackHelper).GetField("Context",
        BindingFlags.NonPublic | BindingFlags.Static);
    var context = contextField?.GetValue(null) as SerializationContext;
    
    Assert.NotNull(context);
    
    // Verify custom serializers
    Assert.NotNull(context.GetSerializer<DateTimeOffset>());
    Assert.NotNull(context.GetSerializer<TimeSpan>());
    Assert.NotNull(context.GetSerializer<Capability>());
    Assert.NotNull(context.GetSerializer<MessageExtras>());
    
    // Verify generated serializers - Core types
    Assert.NotNull(context.GetSerializer<Message>());
    Assert.NotNull(context.GetSerializer<PresenceMessage>());
    Assert.NotNull(context.GetSerializer<ProtocolMessage>());
    Assert.NotNull(context.GetSerializer<ErrorInfo>());
    Assert.NotNull(context.GetSerializer<ConnectionDetails>());
    Assert.NotNull(context.GetSerializer<TokenRequest>());
    Assert.NotNull(context.GetSerializer<TokenDetails>());
    
    // Verify generated serializers - Additional types
    Assert.NotNull(context.GetSerializer<AuthDetails>());
    Assert.NotNull(context.GetSerializer<MessageExtras.DeltaExtras>());
    Assert.NotNull(context.GetSerializer<RecoveryKeyContext>());
    
    // Verify generated serializers - Stats types
    Assert.NotNull(context.GetSerializer<Stats>());
    Assert.NotNull(context.GetSerializer<MessageCount>());
    Assert.NotNull(context.GetSerializer<MessageTypes>());
}
```

### 4.2 Add Integration Tests

Create new test file: `src/IO.Ably.Tests.Shared/MsgPackSerializationIntegrationTests.cs`

```csharp
public class MsgPackSerializationIntegrationTests
{
    [Fact]
    public void MessageWithExtras_SerializesAndDeserializes()
    {
        var original = new Message("test", "data")
        {
            Extras = new MessageExtras(JObject.Parse("{\"delta\":{\"from\":\"1\",\"format\":\"vcdiff\"}}"))
        };

        var bytes = MsgPackHelper.Serialise(original);
        var deserialized = MsgPackHelper.Deserialise<Message>(bytes);

        Assert.Equal(original.Name, deserialized.Name);
        Assert.NotNull(deserialized.Extras);
        Assert.NotNull(deserialized.Extras.Delta);
    }

    [Fact]
    public void AllModels_HaveMsgPackAttributes()
    {
        var modelTypes = new[]
        {
            typeof(Message),
            typeof(PresenceMessage),
            typeof(ProtocolMessage),
            typeof(ErrorInfo),
            typeof(ConnectionDetails),
            typeof(TokenRequest),
            typeof(TokenDetails),
            typeof(AuthDetails),
            typeof(Stats),
            typeof(DeltaExtras),
            typeof(RecoveryKeyContext)
        };

        foreach (var type in modelTypes)
        {
            var attr = type.GetCustomAttribute<MessagePackObjectAttribute>();
            Assert.NotNull(attr);
        }
    }
}
```

---

## 5. Implementation Checklist

### Phase 1: MessageExtras Serializer (Priority: HIGH)
- [ ] Create `MessageExtrasMessagePackSerializer.cs`
- [ ] Register in `MsgPackHelper.cs`
- [ ] Update `IO.Ably.Shared.MsgPack.projitems`
- [ ] Test serialization/deserialization

### Phase 2: Compile-Time Generation (Priority: HIGH)
- [ ] Create `tools/MsgPackSerializerGenerator/` project
- [ ] Implement `Program.cs` with type list
- [ ] Create `IO.Ably.Shared.MsgPack.targets`
- [ ] Import targets in `IO.Ably.NetStandard20.csproj`
- [ ] Update `.gitignore`
- [ ] Create `.gitkeep` file
- [ ] Remove committed generated files from git
- [ ] Test build process generates files correctly

### Phase 3: Add MsgPack Attributes (Priority: HIGH - Best Practice)

**Core Message Types:**
- [ ] Annotate `Message` with `[MessagePackObject]` and `[Key]` attributes
- [ ] Annotate `PresenceMessage` with `[MessagePackObject]` and `[Key]`
- [ ] Annotate `ProtocolMessage` with `[MessagePackObject]` and `[Key]`
- [ ] Annotate `DeltaExtras` with `[MessagePackObject]` and `[Key]`

**Auth & Connection Types:**
- [ ] Annotate `ErrorInfo` with `[MessagePackObject]` and `[Key]`
- [ ] Annotate `ConnectionDetails` with `[MessagePackObject]` and `[Key]`
- [ ] Annotate `TokenRequest` with `[MessagePackObject]` and `[Key]`
- [ ] Annotate `TokenDetails` with `[MessagePackObject]` and `[Key]`
- [ ] Annotate `AuthDetails` with `[MessagePackObject]` and `[Key]`

**Statistics Types:**
- [ ] Annotate `Stats` with `[MessagePackObject]` and `[Key]`
- [ ] Annotate `MessageCount` with `[MessagePackObject]` and `[Key]`
- [ ] Annotate `MessageTypes` with `[MessagePackObject]` and `[Key]`
- [ ] Annotate `RequestCount` with `[MessagePackObject]` and `[Key]`
- [ ] Annotate `ResourceCount` with `[MessagePackObject]` and `[Key]`
- [ ] Annotate `ConnectionTypes` with `[MessagePackObject]` and `[Key]`
- [ ] Annotate `InboundMessageTraffic` with `[MessagePackObject]` and `[Key]`
- [ ] Annotate `OutboundMessageTraffic` with `[MessagePackObject]` and `[Key]`

**Additional Types:**
- [ ] Annotate `RecoveryKeyContext` with `[MessagePackObject]` and `[Key]`
- [ ] Evaluate `ChannelParams` (may need custom serializer as it extends Dictionary)
- [ ] Verify all `[JsonIgnore]` properties also have `[IgnoreMember]`
- [ ] Add `[IgnoreMember]` to computed properties (e.g., `IsEmpty`)

**Push Types (if used):**
- [ ] Annotate `DeviceDetails` with `[MessagePackObject]` and `[Key]`
- [ ] Annotate `PushChannelSubscription` with `[MessagePackObject]` and `[Key]`

### Phase 4: Testing & Validation (Priority: HIGH)
- [ ] Update existing MsgPack tests
- [ ] Add integration tests
- [ ] Run full test suite
- [ ] Verify serialization compatibility
- [ ] Performance testing

### Phase 5: Documentation (Priority: LOW)
- [ ] Update README with build requirements
- [ ] Document serializer generation process
- [ ] Add comments to custom serializers
- [ ] Update CHANGELOG

---

## Migration Notes

### Breaking Changes
- **None expected** - Adding attributes doesn't change generated serializer behavior
- Generated serializers produce identical output with or without attributes
- **Important:** Removing committed generated files from git is not a breaking change for consumers

### Backward Compatibility
- ✅ Existing serialized data remains compatible
- ✅ No API changes required
- ✅ Attributes are additive, not breaking
- ✅ Generated code will be functionally identical to committed code

### Build Requirements
- .NET 6.0 SDK required for generator tool (build time only)
- MsgPack.Cli 1.0.1 (already referenced)
- MSBuild 15.0+ (for targets file support)

### Important Clarifications

**MsgPack.Cli Attribute Behavior:**
1. **Runtime serialization** (using `SerializationContext.GetSerializer<T>()` directly):
   - **REQUIRES** `[MessagePackObject]` and `[Key]` attributes
   - **DOES NOT** respect `[JsonProperty]` or `[JsonIgnore]`
   
2. **Generated serializers** (using `SerializerGenerator.GenerateCode()`):
   - Uses reflection at **generation time** to inspect types
   - Attributes are **optional** but **recommended**
   - Generated code is compiled, not reflection-based at runtime

**Our Implementation:**
- Uses `SerializerGenerator` approach (option #2)
- Currently works without MsgPack attributes
- **Recommendation:** Add attributes for best practices and future-proofing

---

## Success Criteria

1. ✅ MessageExtras serializes/deserializes correctly with MsgPack
2. ✅ No generated code in version control (only .gitkeep file)
3. ✅ Build automatically generates serializers at compile time
4. ✅ All models have MsgPack attributes (alongside JSON attributes) for documentation
5. ✅ All existing tests pass
6. ✅ Serialization output matches previous version (backward compatible)
7. ✅ Clean builds work on CI/CD without manual intervention
8. ✅ Dual-attribute approach (JSON + MsgPack) working correctly
9. ✅ Generator tool builds and runs cross-platform (Windows, Linux, macOS)
10. ✅ Generated files are excluded from source control

---

## Timeline Estimate

- **Phase 1:** 2-4 hours (MessageExtras serializer)
- **Phase 2:** 4-6 hours (Compile-time generation)
- **Phase 3:** 10-12 hours (Add MsgPack attributes to 25+ models)
- **Phase 4:** 4-6 hours (Testing & validation)
- **Phase 5:** 2-3 hours (Documentation)

**Total:** 22-31 hours

**Note:** Phase 3 is now recommended (not optional) for best practices and future-proofing. The increased time accounts for the additional classes identified (25+ models instead of 20+).

---

## References

- [MsgPack.Cli Documentation](https://github.com/msgpack/msgpack-cli)
- [Newtonsoft.Json Documentation](https://www.newtonsoft.com/json/help/html/Introduction.htm)
- [MSBuild Targets](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-targets)
