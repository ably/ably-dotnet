# Migration Guide: MsgPack.Cli to MessagePack-CSharp

## Overview

This document provides a comprehensive guide for migrating the Ably .NET SDK from **MsgPack.Cli** (v1.0.1) to **MessagePack-CSharp** (v3.1.4).

## Why Migrate?

| Aspect | MsgPack.Cli | MessagePack-CSharp |
|--------|-------------|-------------------|
| **Performance** | Moderate | 🚀 **5-10x faster** |
| **Maintenance** | ⚠️ Last updated 2018 | ✅ Actively maintained |
| **.NET Support** | Up to .NET Framework 4.8 | ✅ .NET 6/7/8, .NET Standard 2.0+ |
| **AOT Support** | Limited | ✅ Full AOT/IL2CPP support |
| **Binary Size** | Larger | ✅ Smaller footprint |
| **API Design** | Older patterns | ✅ Modern, attribute-based |

## Current Status

✅ **Good News:** Your code already uses MessagePack-CSharp attributes!
- `[MessagePackObject]` on classes
- `[Key(n)]` on properties

❌ **Problem:** Project files reference the wrong package (MsgPack.Cli)

## Migration Steps

### Step 1: Update MsgPack Shared Project (Keep as Shared Project)

**Current Architecture:**
- `IO.Ably.Shared.MsgPack` is a Shared Project (`.shproj`) that gets imported into platform-specific projects
- `tools/MsgPackSerializerGenerator` is a separate tool that manually generates serializers
- Custom serializers use old MsgPack.Cli API

**New Simplified Architecture:**
- **Keep** `IO.Ably.Shared.MsgPack` as a shared project (`.shproj`) to avoid circular dependencies
- Update consuming projects to add `MessagePack` v3.1.4 package reference
- Configure MessagePack source generator in consuming projects
- Custom formatters rewritten to use MessagePack-CSharp API
- Remove `tools/MsgPackSerializerGenerator` project entirely

**Why keep it as a shared project?**
- ⚠️ Converting to `.csproj` would create circular dependency:
  - `IO.Ably.Shared.MsgPack.csproj` would need to import `IO.Ably.Shared.projitems` (for types)
  - `IO.Ably.NETStandard20.csproj` already imports `IO.Ably.Shared.projitems`
  - `IO.Ably.NETStandard20.csproj` would reference `IO.Ably.Shared.MsgPack.csproj`
  - Result: Both projects importing same shared items = circular dependency
- ✅ Shared projects are just file includes, no separate assembly
- ✅ Source generator can still run in consuming projects

#### 1.1 Update Consuming Projects with MessagePack Package

**SDK-Style Projects (.NET Standard, .NET 6+):**
- [`src/IO.Ably.NETStandard20/IO.Ably.NETStandard20.csproj`](src/IO.Ably.NETStandard20/IO.Ably.NETStandard20.csproj:49)
- [`src/IO.Ably.Android/IO.Ably.Android.csproj`](src/IO.Ably.Android/IO.Ably.Android.csproj)
- [`src/IO.Ably.iOS/IO.Ably.iOS.csproj`](src/IO.Ably.iOS/IO.Ably.iOS.csproj)

**Change package reference:**

```xml
<!-- OLD -->
<PackageReference Include="MsgPack.Cli" Version="1.0.1" />

<!-- NEW -->
<PackageReference Include="MessagePack" Version="3.1.4" />
<PackageReference Include="MessagePack.Analyzer" Version="3.1.4">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

**Keep the shared project import:**

```xml
<!-- KEEP - Shared Project Import -->
<Import Project="..\IO.Ably.Shared.MsgPack\IO.Ably.Shared.MsgPack.projitems" Label="Shared" />
```

**Legacy .NET Framework Project:**
- [`src/IO.Ably.NETFramework/IO.Ably.NETFramework.csproj`](src/IO.Ably.NETFramework/IO.Ably.NETFramework.csproj)

**Changes needed:**

1. **Add MsgPack shared project import** (currently missing - line 108):
```xml
<!-- ADD this import after IO.Ably.Shared import -->
<Import Project="..\IO.Ably.Shared\IO.Ably.Shared.projitems" Label="Shared" />
<Import Project="..\IO.Ably.Shared.MsgPack\IO.Ably.Shared.MsgPack.projitems" Label="Shared" />
```

2. **Remove `EXCLUDE_MSGPACK` flag** (line 24):
```xml
<!-- OLD -->
<DefineConstants>TRACE;DEBUG;EXCLUDE_MSGPACK</DefineConstants>

<!-- NEW -->
<DefineConstants>TRACE;DEBUG</DefineConstants>
```

3. **Add MessagePack package** to `packages.config`:
```xml
<package id="MessagePack" version="3.1.4" targetFramework="net462" />
<package id="MessagePack.Annotations" version="3.1.4" targetFramework="net462" />
<package id="System.Runtime.CompilerServices.Unsafe" version="6.0.0" targetFramework="net462" />
<package id="System.Threading.Tasks.Extensions" version="4.5.4" targetFramework="net462" />
<package id="System.Memory" version="4.5.5" targetFramework="net462" />
<package id="System.Buffers" version="4.5.1" targetFramework="net462" />
```

4. **Add Reference elements** to the project file:
```xml
<Reference Include="MessagePack, Version=3.1.0.0, Culture=neutral, PublicKeyToken=b4a0369545f0a1be">
  <HintPath>..\packages\MessagePack.3.1.4\lib\netstandard2.0\MessagePack.dll</HintPath>
</Reference>
<Reference Include="MessagePack.Annotations, Version=3.1.0.0, Culture=neutral, PublicKeyToken=b4a0369545f0a1be">
  <HintPath>..\packages\MessagePack.Annotations.3.1.4\lib\netstandard2.0\MessagePack.Annotations.dll</HintPath>
</Reference>
```

#### 1.2 Configure MessagePack Source Generator in Consuming Projects

Add source generator configuration to each consuming project (e.g., `IO.Ably.NETStandard20.csproj`):

```xml
<!-- Configure MessagePack Source Generator -->
<PropertyGroup>
  <MessagePackGenerator_ResolverName>AblyGeneratedResolver</MessagePackGenerator_ResolverName>
  <MessagePackGenerator_Namespace>IO.Ably.CustomSerialisers</MessagePackGenerator_Namespace>
</PropertyGroup>
```

**How it works:**
- Source generator runs in each consuming project during build
- Scans all types with `[MessagePackObject]` from both shared projects
- Generates formatters in `obj/` folder of consuming project
- No circular dependencies because shared projects are just file includes

#### 1.3 Update Test Projects

**Files to update:**
- [`src/IO.Ably.Tests.DotNET/IO.Ably.Tests.DotNET.csproj`](src/IO.Ably.Tests.DotNET/IO.Ably.Tests.DotNET.csproj:28)

**Change:**
```xml
<!-- OLD -->
<PackageReference Include="MsgPack.Cli" Version="1.0.1" />

<!-- NEW -->
<PackageReference Include="MessagePack" Version="3.1.4" />
```

**Note:** `IO.Ably.Tests.NETFramework` references `IO.Ably.NETFramework.csproj`, so once that project is updated with MessagePack (see step 1.1), the test project will automatically get the dependency transitively. No direct changes needed to the test project itself.

#### 1.4 Legacy Xamarin Projects

**Files to update:**
- [`src/IO.Ably.iOS/IO.Ably.iOS.csproj`](src/IO.Ably.iOS/IO.Ably.iOS.csproj:70-71)
- [`src/IO.Ably.Android/IO.Ably.Android.csproj`](src/IO.Ably.Android/IO.Ably.Android.csproj:68-69)
- [`examples/AndroidSample/AndroidSample.csproj`](examples/AndroidSample/AndroidSample.csproj:65-66)

**Change:**
```xml
<!-- OLD -->
<Reference Include="MsgPack, Version=1.0.0.0, Culture=neutral, processorArchitecture=MSIL">
  <HintPath>..\packages\MsgPack.Cli.1.0.1\lib\[platform]\MsgPack.dll</HintPath>
</Reference>

<!-- NEW -->
<PackageReference Include="MessagePack" Version="3.1.4" />
```

**Keep the shared project import:**
```xml
<!-- KEEP -->
<Import Project="..\IO.Ably.Shared.MsgPack\IO.Ably.Shared.MsgPack.projitems" Label="Shared" />
```

#### 1.5 Remove packages.config (if exists)

Delete or update any `packages.config` files that reference MsgPack.Cli.

#### 1.6 Remove Old Serializer Generator Tool

**Delete entire directory:**
- `tools/MsgPackSerializerGenerator/`

**Remove from solution files:**
- Remove project reference from any `.sln` files that include `MsgPackSerializerGenerator.csproj`

**Remove build targets (if exists):**
- Delete `src/IO.Ably.Shared.MsgPack/IO.Ably.Shared.MsgPack.targets` (if it exists)
- Remove import from `IO.Ably.NETStandard20.csproj`:
  ```xml
  <!-- REMOVE this line -->
  <Import Project="..\IO.Ably.Shared.MsgPack\IO.Ably.Shared.MsgPack.targets" Condition="Exists('..\IO.Ably.Shared.MsgPack\IO.Ably.Shared.MsgPack.targets')" />
  ```

**Why remove it?**
- ✅ MessagePack.Generator source generator handles code generation automatically
- ✅ No manual tool execution needed
- ✅ Simpler build process
- ✅ Better IDE integration

### Step 2: Update Using Statements

**Files affected:** All C# files using MessagePack

**Change:**
```csharp
// OLD
using MsgPack;
using MsgPack.Serialization;

// NEW
using MessagePack;
```

**Files to update:**
- [`src/IO.Ably.Shared/Push/DeviceDetails.cs`](src/IO.Ably.Shared/Push/DeviceDetails.cs:1-2)
- [`src/IO.Ably.Shared/Types/Message.cs`](src/IO.Ably.Shared/Types/Message.cs:7)
- [`src/IO.Ably.Shared/Types/PresenceMessage.cs`](src/IO.Ably.Shared/Types/PresenceMessage.cs:2)
- [`src/IO.Ably.Shared/Types/ProtocolMessage.cs`](src/IO.Ably.Shared/Types/ProtocolMessage.cs:6)
- [`src/IO.Ably.Shared/Types/ErrorInfo.cs`](src/IO.Ably.Shared/Types/ErrorInfo.cs:5)
- [`src/IO.Ably.Shared/Types/ConnectionDetails.cs`](src/IO.Ably.Shared/Types/ConnectionDetails.cs:2)
- [`src/IO.Ably.Shared/Types/AuthDetails.cs`](src/IO.Ably.Shared/Types/AuthDetails.cs:1)
- [`src/IO.Ably.Shared/Types/ChannelParams.cs`](src/IO.Ably.Shared/Types/ChannelParams.cs:3)
- [`src/IO.Ably.Shared/Types/MessageExtras.cs`](src/IO.Ably.Shared/Types/MessageExtras.cs:2)
- [`src/IO.Ably.Shared/TokenRequest.cs`](src/IO.Ably.Shared/TokenRequest.cs:4)
- [`src/IO.Ably.Shared/TokenDetails.cs`](src/IO.Ably.Shared/TokenDetails.cs:2)
- [`src/IO.Ably.Shared/Statistics.cs`](src/IO.Ably.Shared/Statistics.cs:3)
- [`src/IO.Ably.Shared/Realtime/RecoveryKeyContext.cs`](src/IO.Ably.Shared/Realtime/RecoveryKeyContext.cs:4)
- [`src/IO.Ably.Shared/Push/PushChannelSubscription.cs`](src/IO.Ably.Shared/Push/PushChannelSubscription.cs:1)
- [`src/IO.Ably.Shared/Rest/ChannelDetails.cs`](src/IO.Ably.Shared/Rest/ChannelDetails.cs:1)

### Step 3: Update Serialization Code

#### 3.1 MsgPackHelper.cs

**File:** [`src/IO.Ably.Shared.MsgPack/MsgPackHelper.cs`](src/IO.Ably.Shared.MsgPack/MsgPackHelper.cs:4-5)

**OLD API:**
```csharp
using MsgPack;
using MsgPack.Serialization;

public static class MsgPackHelper
{
    private static readonly SerializationContext Context = new SerializationContext();
    
    public static byte[] Serialise(object value, Type type)
    {
        var serializer = Context.GetSerializer(type);
        using (var stream = new MemoryStream())
        {
            serializer.Pack(stream, value);
            return stream.ToArray();
        }
    }
    
    public static object Deserialise(byte[] byteArray, Type type)
    {
        var serializer = Context.GetSerializer(type);
        using (var stream = new MemoryStream(byteArray))
        {
            return serializer.Unpack(stream);
        }
    }
}
```

**NEW API:**
```csharp
using MessagePack;
using MessagePack.Resolvers;

public static class MsgPackHelper
{
    private static readonly MessagePackSerializerOptions Options =
        MessagePackSerializerOptions.Standard
            .WithResolver(StandardResolver.Instance);
    
    public static byte[] Serialise(object value, Type type)
    {
        return MessagePackSerializer.Serialize(type, value, Options);
    }
    
    public static object Deserialise(byte[] byteArray, Type type)
    {
        return MessagePackSerializer.Deserialize(type, byteArray, Options);
    }
    
    public static T Deserialise<T>(byte[] byteArray)
    {
        return MessagePackSerializer.Deserialize<T>(byteArray, Options);
    }
}
```

**Note:** In v3.x, `ContractlessStandardResolver` is deprecated. Use `StandardResolver` with proper attributes.

#### 3.2 Remove MessagePackObject References

**File:** [`src/IO.Ably.Tests.Shared/MessagePack/SerializationTests.cs`](src/IO.Ably.Tests.Shared/MessagePack/SerializationTests.cs:124)

**Change:**
```csharp
// OLD
var decodedMessagePack = MsgPackHelper.Deserialise(value.FromBase64(), typeof(MessagePackObject)).ToString();

// NEW - MessagePackObject doesn't exist in MessagePack-CSharp
// Instead, deserialize to the actual type or use dynamic
var decodedMessagePack = MsgPackHelper.Deserialise<Dictionary<string, object>>(value.FromBase64());
```

### Step 4: Update Custom Serializers

MessagePack-CSharp uses **Formatters** instead of custom serializers. The API is completely different.

#### 4.1 Custom Formatter Pattern

**OLD (MsgPack.Cli):**
```csharp
public class CustomSerializer : MessagePackSerializer<MyType>
{
    protected internal override void PackToCore(Packer packer, MyType objectTree)
    {
        packer.Pack(objectTree.Value);
    }
    
    protected internal override MyType UnpackFromCore(Unpacker unpacker)
    {
        return new MyType { Value = unpacker.LastReadData.AsString() };
    }
}
```

**NEW (MessagePack-CSharp):**
```csharp
public class CustomFormatter : IMessagePackFormatter<MyType>
{
    public void Serialize(ref MessagePackWriter writer, MyType value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value);
    }
    
    public MyType Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new MyType { Value = reader.ReadString() };
    }
}
```

#### 4.2 Files Requiring Custom Formatter Updates

**Directory:** [`src/IO.Ably.Shared.MsgPack/CustomSerialisers/`](src/IO.Ably.Shared.MsgPack/CustomSerialisers/)

Files to rewrite:
1. **CapabilityMessagePackSerializer.cs** → `CapabilityFormatter.cs`
2. **DateTimeOffsetMessagePackSerializer.cs** → `DateTimeOffsetFormatter.cs`
3. **TimespanMessagePackSerializer.cs** → `TimespanFormatter.cs`
4. **MessageExtrasMessagePackSerializer.cs** → `MessageExtrasFormatter.cs`

**Example: DateTimeOffset Formatter**

```csharp
using MessagePack;
using MessagePack.Formatters;

namespace IO.Ably.CustomSerialisers
{
    public class DateTimeOffsetFormatter : IMessagePackFormatter<DateTimeOffset>
    {
        public void Serialize(ref MessagePackWriter writer, DateTimeOffset value, MessagePackSerializerOptions options)
        {
            writer.Write(value.ToUnixTimeMilliseconds());
        }

        public DateTimeOffset Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var milliseconds = reader.ReadInt64();
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        }
    }
}
```

#### 4.3 Register Custom Formatters

Create a custom resolver:

```csharp
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace IO.Ably.CustomSerialisers
{
    public class AblyResolver : IFormatterResolver
    {
        public static readonly AblyResolver Instance = new AblyResolver();

        private AblyResolver() { }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                Formatter = (IMessagePackFormatter<T>)GetFormatterHelper(typeof(T));
            }

            private static object GetFormatterHelper(Type t)
            {
                if (t == typeof(DateTimeOffset))
                    return new DateTimeOffsetFormatter();
                if (t == typeof(TimeSpan))
                    return new TimespanFormatter();
                if (t == typeof(Capability))
                    return new CapabilityFormatter();
                if (t == typeof(MessageExtras))
                    return new MessageExtrasFormatter();

                return null;
            }
        }
    }

    public static class AblyMessagePackOptions
    {
        public static readonly MessagePackSerializerOptions Standard = 
            MessagePackSerializerOptions.Standard
                .WithResolver(CompositeResolver.Create(
                    AblyResolver.Instance,
                    StandardResolver.Instance
                ));
    }
}
```

### Step 5: Configure MessagePack Source Generator

The MessagePack source generator is automatically included with the `MessagePack` package and runs during build. Configuration is done via MSBuild properties in consuming projects.

#### 5.1 Source Generator Configuration

**Add to each consuming project (e.g., `IO.Ably.NETStandard20.csproj`):**

```xml
<PropertyGroup>
  <!-- Configure the generated resolver name -->
  <MessagePackGenerator_ResolverName>AblyGeneratedResolver</MessagePackGenerator_ResolverName>
  
  <!-- Configure the namespace for generated code -->
  <MessagePackGenerator_Namespace>IO.Ably.CustomSerialisers</MessagePackGenerator_Namespace>
</PropertyGroup>
```

**How it works:**
1. Source generator scans all types with `[MessagePackObject]` attribute from both `IO.Ably.Shared` and `IO.Ably.Shared.MsgPack` shared projects
2. Automatically generates formatters during compilation of the consuming project
3. Generated code is placed in `obj/` folder of the consuming project (not committed to source control)
4. Formatters are compiled into the consuming project's assembly for AOT/IL2CPP support
5. No manual tool execution required - happens automatically on build
6. Each consuming project gets its own generated formatters (no sharing needed)

**Generated Resolver Usage:**
```csharp
// The generated resolver will be available as:
// IO.Ably.CustomSerialisers.AblyGeneratedResolver

// Use it in your options:
var options = MessagePackSerializerOptions.Standard
    .WithResolver(CompositeResolver.Create(
        AblyResolver.Instance,              // Custom formatters
        AblyGeneratedResolver.Instance,     // Auto-generated formatters
        StandardResolver.Instance           // Built-in formatters
    ));
```

#### 5.2 Verify Source Generator is Working

After building a consuming project, you can verify the source generator ran successfully:

```bash
# Build the consuming project
dotnet build src/IO.Ably.NETStandard20/IO.Ably.NETStandard20.csproj

# Check generated files (they'll be in the consuming project's obj folder)
# Example path: src/IO.Ably.NETStandard20/obj/Debug/netstandard2.0/generated/MessagePack/MessagePack.SourceGenerator/
```

**Generated files include:**
- `AblyGeneratedResolver.cs` - The resolver that registers all formatters
- Individual formatter files for each `[MessagePackObject]` type

**Build output will show:**
```
MessagePack.SourceGenerator: Found X types with [MessagePackObject] attribute
MessagePack.SourceGenerator: Generated formatters for X types
```

#### 5.3 Optional: Manual Code Generation (Advanced)

If you need to generate code manually (e.g., for debugging or inspection), you can use the command-line tool:

```bash
# Install global tool (optional)
dotnet tool install -g MessagePack.Generator

# Generate formatters manually
dotnet mpc -i path/to/IO.Ably.dll -o Generated/MessagePackFormatters.cs
```

**Note:** This is rarely needed since the source generator handles everything automatically during build.

#### 5.4 Troubleshooting Source Generator

**Issue: Source generator not running**
- Ensure `MessagePack` package version is 3.1.4 or higher
- Clean and rebuild: `dotnet clean && dotnet build`
- Check build output for source generator messages

**Issue: Types not being discovered**
- Verify types have `[MessagePackObject]` attribute
- Ensure types are `public` or `internal`
- Check that properties have `[Key(n)]` attributes

**Issue: Build errors with generated code**
- Check for circular dependencies in your types
- Ensure all referenced types are also annotated
- Review custom formatters for conflicts

### Step 6: Update Test Files

**File:** [`src/IO.Ably.Tests.Shared/MsgPackMessageSerializerTests.cs`](src/IO.Ably.Tests.Shared/MsgPackMessageSerializerTests.cs:7)

**Change:**
```csharp
// OLD
using MsgPack.Serialization;

// NEW
using MessagePack;
```

Update test assertions to use new API:
```csharp
// OLD
var serializer = SerializationContext.Default.GetSerializer<Message>();
var bytes = serializer.PackSingleObject(message);

// NEW
var bytes = MessagePackSerializer.Serialize(message, AblyMessagePackOptions.Standard);
var deserialized = MessagePackSerializer.Deserialize<Message>(bytes, AblyMessagePackOptions.Standard);
```

### Step 7: Handle Breaking Changes

#### 7.1 No More MessagePackObject Type

MessagePack-CSharp doesn't have a `MessagePackObject` type. Use concrete types or `object` with dynamic deserialization.

**OLD:**
```csharp
MessagePackObject obj = MsgPackHelper.Deserialise(bytes, typeof(MessagePackObject));
```

**NEW:**
```csharp
// Deserialize to specific type
var message = MessagePackSerializer.Deserialize<Message>(bytes);

// Or use dynamic
dynamic obj = MessagePackSerializer.Deserialize<object>(bytes);
```

#### 7.2 Enum Serialization

MessagePack-CSharp serializes enums as integers by default (more efficient).

If you need string serialization:
```csharp
[MessagePackObject]
public class MyClass
{
    [Key(0)]
    [MessagePackFormatter(typeof(EnumAsStringFormatter<MyEnum>))]
    public MyEnum Status { get; set; }
}
```

#### 7.3 DateTime Serialization

MessagePack-CSharp uses MessagePack timestamp extension type by default.

For custom DateTime handling, use a custom formatter (see Step 4.2).

### Step 8: Validation & Testing

#### 8.1 Build Verification

```bash
# Clean solution
dotnet clean

# Restore packages
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test
```

#### 8.2 Test Checklist

- [ ] All projects build successfully
- [ ] Unit tests pass
- [ ] Serialization/deserialization works correctly
- [ ] Custom formatters work as expected
- [ ] No runtime errors in production scenarios
- [ ] Performance benchmarks show improvement

#### 8.3 Binary Compatibility

⚠️ **IMPORTANT:** MessagePack-CSharp produces **different binary output** than MsgPack.Cli.

**Impact:**
- Existing serialized data may not deserialize correctly
- Need migration strategy for persisted data
- API clients may need updates

**Migration Strategy:**
1. Version your API endpoints
2. Support both formats during transition period
3. Provide data migration tools if needed

### Step 9: Performance Optimization

MessagePack-CSharp offers several optimization options:

```csharp
var options = MessagePackSerializerOptions.Standard
    .WithCompression(MessagePackCompression.Lz4BlockArray) // Optional compression
    .WithSecurity(MessagePackSecurity.UntrustedData)       // Security settings
    .WithResolver(CompositeResolver.Create(
        AblyResolver.Instance,
        StandardResolver.Instance
    ));
```

### Step 10: Documentation Updates

Update documentation to reflect:
- New package dependency
- API changes
- Performance improvements
- Breaking changes for consumers

## Troubleshooting

### Issue: "Type is not registered in this resolver"

**Solution:** Ensure your custom resolver is registered:
```csharp
var options = MessagePackSerializerOptions.Standard
    .WithResolver(CompositeResolver.Create(
        YourCustomResolver.Instance,
        StandardResolver.Instance
    ));
```

### Issue: "Cannot deserialize readonly property"

**Solution:** Add a constructor or make property settable:
```csharp
[MessagePackObject]
public class MyClass
{
    [Key(0)]
    public string Name { get; set; } // Must have setter
}
```

### Issue: "Circular reference detected"

**Solution:** Use `[IgnoreMember]` or implement custom formatter:
```csharp
[MessagePackObject]
public class Node
{
    [Key(0)]
    public string Value { get; set; }
    
    [IgnoreMember] // Skip serialization
    public Node Parent { get; set; }
}
```

## Summary of Changes

| Component | Action | Files Affected |
|-----------|--------|----------------|
| **Project References** | Update package | 6 .csproj files |
| **Using Statements** | Change namespace | ~20 .cs files |
| **MsgPackHelper** | Rewrite API | 1 file |
| **Custom Serializers** | Convert to Formatters | 4 files |
| **Tests** | Update assertions | Multiple test files |
| **Serializer Generator** | Replace tool | 1 project |

## Estimated Effort

**For Ably .NET SDK Migration:**
- **Project structure changes:** 2-3 hours
  - Convert shared project to standard project
  - Update all project references
  - Remove old generator tool
- **Code updates:** 3-4 hours
  - Update using statements
  - Rewrite MsgPackHelper
  - Convert 4 custom serializers to formatters
  - Create custom resolver
- **Testing & validation:** 2-3 hours
  - Update test code
  - Run full test suite
  - Verify serialization compatibility
- **Total estimated time:** 7-10 hours

**Complexity factors:**
- ✅ Code already uses MessagePack attributes (saves time!)
- ✅ Well-defined custom serializers (clear conversion path)
- ⚠️ Need to verify binary compatibility with existing data
- ⚠️ Multiple platform targets (iOS, Android, .NET Standard)

## Key Improvements in v3.x

### 1. Source Generator Support
- Automatic formatter generation at compile time
- No runtime reflection needed
- Better AOT/trimming support
- Faster startup time

### 2. Performance Enhancements
- Even faster than v2.x (up to 10x faster than MsgPack.Cli)
- Reduced allocations
- Better memory efficiency
- Optimized for modern .NET runtime

### 3. .NET 6+ Optimizations
- Span<T> and Memory<T> support
- Modern C# features (records, init-only properties)
- Better async/await performance
- Native AOT support

### 4. Simplified API
- No separate annotations package needed
- Cleaner resolver system
- Better error messages
- Improved debugging experience

### 5. Security Enhancements
- Built-in security options
- Protection against malicious payloads
- Configurable depth limits
- Type verification

## References

- [MessagePack-CSharp GitHub](https://github.com/MessagePack-CSharp/MessagePack-CSharp)
- [MessagePack-CSharp v3.x Release Notes](https://github.com/MessagePack-CSharp/MessagePack-CSharp/releases/tag/v3.0.0)
- [MessagePack-CSharp Documentation](https://github.com/MessagePack-CSharp/MessagePack-CSharp#readme)
- [Migration Guide from v2 to v3](https://github.com/MessagePack-CSharp/MessagePack-CSharp/blob/master/doc/migration.md)
- [MessagePack Specification](https://github.com/msgpack/msgpack/blob/master/spec.md)
- [Performance Benchmarks](https://github.com/MessagePack-CSharp/MessagePack-CSharp#performance)

## Support

For issues during migration:
1. Check MessagePack-CSharp GitHub issues
2. Review migration examples in the repository
3. Consult the comprehensive documentation

## Migration Checklist

Use this checklist to track your migration progress:

### Phase 1: Project Structure
- [ ] Keep `IO.Ably.Shared.MsgPack` as `.shproj` (no conversion needed)
- [ ] Update `IO.Ably.NETStandard20.csproj` - replace MsgPack.Cli with MessagePack v3.1.4
- [ ] Update `IO.Ably.NETFramework.csproj` - add MsgPack import, remove `EXCLUDE_MSGPACK`, add MessagePack package
- [ ] Update `IO.Ably.Android.csproj` - replace MsgPack.Cli with MessagePack v3.1.4
- [ ] Update `IO.Ably.iOS.csproj` - replace MsgPack.Cli with MessagePack v3.1.4
- [ ] Update `IO.Ably.Tests.DotNET.csproj` - replace MsgPack.Cli with MessagePack v3.1.4
- [ ] Configure source generator properties in each consuming project
- [ ] Delete `tools/MsgPackSerializerGenerator/` directory
- [ ] Remove build targets import from `IO.Ably.NETStandard20.csproj` (if exists)
- [ ] Update solution files to remove generator tool project
- [ ] Search for and remove any other `EXCLUDE_MSGPACK` or `MSGPACK`-related conditional compilation flags

### Phase 2: Code Updates
- [ ] Update using statements in ~20 files
- [ ] Rewrite `MsgPackHelper.cs` with new API
- [ ] Convert `CapabilityMessagePackSerializer` to `CapabilityFormatter`
- [ ] Convert `DateTimeOffsetMessagePackSerializer` to `DateTimeOffsetFormatter`
- [ ] Convert `TimespanMessagePackSerializer` to `TimespanFormatter`
- [ ] Convert `MessageExtrasMessagePackSerializer` to `MessageExtrasFormatter`
- [ ] Create `AblyResolver` class
- [ ] Create `AblyMessagePackOptions` class
- [ ] Update test files with new API

### Phase 3: Validation
- [ ] Build solution successfully
- [ ] Run unit tests
- [ ] Verify serialization/deserialization
- [ ] Test custom formatters
- [ ] Verify source generator output
- [ ] Test on all target platforms
- [ ] Performance benchmarking
- [ ] Binary compatibility testing

### Phase 4: Documentation
- [ ] Update README with new dependencies
- [ ] Update CHANGELOG
- [ ] Document breaking changes
- [ ] Update developer documentation

---

**Last Updated:** 2025-10-31
**Migration Status:** 📋 Planning Phase - Ready for Implementation
