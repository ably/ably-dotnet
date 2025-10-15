
# Newtonsoft.Json Migration Plan: 9.0.1 → 13.0.1

## Executive Summary

This document outlines the migration strategy for upgrading Newtonsoft.Json from version 9.0.1 to 13.0.1 in the ably-dotnet project. This upgrade spans four major versions (10.0, 11.0, 12.0, and 13.0) and addresses critical security vulnerabilities while introducing several breaking changes that require code modifications.

**Migration Priority:** HIGH - Security vulnerabilities in 9.0.1 require immediate attention.

**Estimated Effort:** Medium-High - Requires code changes, thorough testing, and validation across multiple platforms.

**Current Branch:** `fix/upgrade-newtonsoft-dep`

**Special Note:** Unity, Xamarin, and .NET Framework platforms will use ILRepack.exe to merge Newtonsoft.Json and Ably DLLs, simplifying deployment for these platforms.

---

## Table of Contents

1. [Motivation](#motivation)
2. [Breaking Changes Analysis](#breaking-changes-analysis)
3. [Impact Assessment](#impact-assessment)
4. [Migration Steps](#migration-steps)
5. [Code Changes Required](#code-changes-required)
6. [Testing Strategy](#testing-strategy)
7. [Rollback Plan](#rollback-plan)
8. [References](#references)

---

## Motivation

### Security Vulnerabilities

Version 9.0.1 contains multiple high-severity security vulnerabilities:

- **CVE-2024-21907**: Stack overflow vulnerability leading to potential DoS attacks
- **Denial of Service Protection**: Enhanced protection against malformed JSON
- **Regex Timeout Protection**: Configurable timeout for JSONPath regex operations
- **Memory Exhaustion**: Protection against large JSON payloads causing memory issues

### Compatibility Requirements

- **NU1903 Warning**: .NET 8/9 projects generate security warnings with Newtonsoft.Json 9.0.1
- **Unity Compatibility**: Version 13.0.1 will be merged with Ably DLL using ILRepack
- **Xamarin Support**: Android/iOS projects will use ILRepack for DLL merging
- **.NET Framework**: Will also use ILRepack for DLL merging
- **Modern .NET Support**: Better support for .NET 6, 7, 8, and 9

---

## Breaking Changes Analysis

### Version 10.0 Breaking Changes

#### 1. Constructor Selection Changes

**Impact:** HIGH

**Description:** Version 10.0 introduced stricter rules for constructor selection during deserialization. Classes with multiple constructors now require explicit `[JsonConstructor]` attribute.

**Affected Code Pattern:**
```csharp
// Before (9.0.1) - Multiple constructors worked automatically
public class Message
{
    public Message() { }
    public Message(string name, object data) { ... }
}
```

**Required Change:**
```csharp
// After (13.0.1) - Requires explicit marking
public class Message
{
    public Message() { }
    
    [JsonConstructor]  // Required for non-default constructor
    public Message(string name, object data) { ... }
}
```

### Version 12.0 Breaking Changes

#### 1. MaxDepth Default Change

**Impact:** MEDIUM

**Description:** `JsonReader` and `JsonSerializer` `MaxDepth` property now defaults to 64 instead of unlimited.

**Required Change:**
```csharp
// In JsonHelper.GetJsonSettings()
private static JsonSerializerSettings GetJsonSettings()
{
    var res = new JsonSerializerSettings
    {
        Converters = new List<JsonConverter> { ... },
        DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
        NullValueHandling = NullValueHandling.Ignore,
        MaxDepth = null  // Add this to maintain unlimited depth behavior
    };
    return res;
}
```

### Version 13.0 Breaking Changes

#### 1. Exception Type Changes

**Impact:** MEDIUM

**Description:** Type mismatch errors now throw `JsonSerializationException` instead of `InvalidCastException`.

---

## Impact Assessment

### Affected Project Files

#### 1. Core Project Configuration Files (10 files)

All project files need version update from 9.0.1 to 13.0.1:

1. **[`src/IO.Ably.NETStandard20/IO.Ably.NETStandard20.csproj`](src/IO.Ably.NETStandard20/IO.Ably.NETStandard20.csproj:49)**
   ```xml
   <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
   ```

2. **[`src/IO.Ably.NETFramework/IO.Ably.NETFramework.csproj`](src/IO.Ably.NETFramework/IO.Ably.NETFramework.csproj:63-64)** (Will use ILRepack)
   - Update Version to 13.0.0.0
   - Update HintPath to 13.0.1

3. **[`src/IO.Ably.Android/IO.Ably.Android.csproj`](src/IO.Ably.Android/IO.Ably.Android.csproj:71-72)** (Will use ILRepack)

4. **[`src/IO.Ably.iOS/IO.Ably.iOS.csproj`](src/IO.Ably.iOS/IO.Ably.iOS.csproj:73-75)** (Will use ILRepack)

5. **[`src/IO.Ably.Push.Android/IO.Ably.Push.Android.csproj`](src/IO.Ably.Push.Android/IO.Ably.Push.Android.csproj)** (Will use ILRepack)

6. **[`src/IO.Ably.Push.iOS/IO.Ably.Push.iOS.csproj`](src/IO.Ably.Push.iOS/IO.Ably.Push.iOS.csproj)** (Will use ILRepack)

7. **[`src/IO.Ably.Tests.DotNET/IO.Ably.Tests.DotNET.csproj`](src/IO.Ably.Tests.DotNET/IO.Ably.Tests.DotNET.csproj:30)**
   ```xml
   <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
   ```

8. **[`src/IO.Ably.Tests.NETFramework/IO.Ably.Tests.NETFramework.csproj`](src/IO.Ably.Tests.NETFramework/IO.Ably.Tests.NETFramework.csproj:122-123)**
   ```xml
   <PackageReference Include="Newtonsoft.Json">
     <Version>13.0.1</Version>
   ```

9. **[`nuget/io.ably.nuspec`](nuget/io.ably.nuspec:31,35,39)** (3 occurrences)
   ```xml
   <dependency id="Newtonsoft.Json" version="13.0.1" />
   ```

10. **[`nuget/io.ably.push.android.nuspec`](nuget/io.ably.push.android.nuspec)** and **[`nuget/io.ably.push.ios.nuspec`](nuget/io.ably.push.ios.nuspec)**

#### 2. Package Configuration Files

Update `packages.config` files in:
- `src/IO.Ably.NETFramework/packages.config` (ILRepack)
- `src/IO.Ably.Android/packages.config` (ILRepack)
- `src/IO.Ably.iOS/packages.config` (ILRepack)
- `src/IO.Ably.Push.Android/packages.config` (ILRepack)
- `src/IO.Ably.Push.iOS/packages.config` (ILRepack)

### Affected Source Files

#### Critical Files - Requires Code Changes

1. **[`src/IO.Ably.Shared/JsonHelper.cs`](src/IO.Ably.Shared/JsonHelper.cs)**
   - **Lines 18-33**: Add `MaxDepth = null` to `JsonSerializerSettings`
   - **Lines 108-109**: Remove deprecated `CheckAdditionalContent`
   - **Impact:** Core serialization functionality

2. **[`src/IO.Ably.Shared/Types/Message.cs`](src/IO.Ably.Shared/Types/Message.cs)**
   - **Lines 31-41**: Add `[JsonConstructor]` attribute
   - **Lines 188-199, 209-220**: Update exception handling for `JsonSerializationException`
   - **Uses:** `[JsonProperty]` attributes (lines 44-84), `[JsonIgnore]` (line 101)

3. **[`src/IO.Ably.Shared/Types/ErrorInfo.cs`](src/IO.Ably.Shared/Types/ErrorInfo.cs)**
   - **Lines 107-137**: Add `[JsonConstructor]` to appropriate constructor
   - **Lines 183-196**: Update exception handling to catch `JsonException`
   - **Uses:** `[JsonProperty]` attributes, `JObject.Parse()`

4. **[`src/IO.Ably.Shared/TokenDetails.cs`](src/IO.Ably.Shared/TokenDetails.cs)**
   - **Lines 51-62**: Add `[JsonConstructor]` if needed
   - **Uses:** `[JsonProperty]` with `NullValueHandling.Ignore` (line 21)

5. **[`src/IO.Ably.Shared/TokenRequest.cs`](src/IO.Ably.Shared/TokenRequest.cs)**
   - **Lines 20-28**: Add `[JsonConstructor]` to parameterless constructor
   - **Uses:** `[JsonProperty]` with `NullValueHandling.Ignore` (lines 50, 56)

#### Files with Heavy JSON Usage - Review Required

6. **[`src/IO.Ably.Shared/TokenResponse.cs`](src/IO.Ably.Shared/TokenResponse.cs)**
   - Uses `[JsonProperty("access_token")]`

7. **[`src/IO.Ably.Shared/Rest/ChannelDetails.cs`](src/IO.Ably.Shared/Rest/ChannelDetails.cs)**
   - Multiple `[JsonProperty]` attributes for all properties

8. **[`src/IO.Ably.Shared/Realtime/RecoveryKeyContext.cs`](src/IO.Ably.Shared/Realtime/RecoveryKeyContext.cs)**
   - Uses `[JsonProperty]` for serialization

9. **[`src/IO.Ably.Shared/Types/AuthDetails.cs`](src/IO.Ably.Shared/Types/AuthDetails.cs)**
   - Uses `[JsonProperty("accessToken")]`

10. **[`src/IO.Ably.Shared/ClientOptions.cs`](src/IO.Ably.Shared/ClientOptions.cs)**
    - Multiple `[JsonIgnore]` attributes (lines 109, 342, 416, 423)

#### Custom JsonConverter Implementations (5 files)

All custom converters need thorough testing:

11. **[`src/IO.Ably.Shared/CustomSerialisers/MessageDataConverter.cs`](src/IO.Ably.Shared/CustomSerialisers/MessageDataConverter.cs)**
    - Uses `NullValueHandling.Include` override

12. **[`src/IO.Ably.Shared/CustomSerialisers/CapabilityJsonConverter.cs`](src/IO.Ably.Shared/CustomSerialisers/CapabilityJsonConverter.cs)**
    - Uses `JToken.Load(reader)`

13. **[`src/IO.Ably.Shared/CustomSerialisers/MessageExtrasConverter.cs`](src/IO.Ably.Shared/CustomSerialisers/MessageExtrasConverter.cs)**
    - Uses `JToken.Load(reader)`

14. **[`src/IO.Ably.Shared/CustomSerialisers/TimeSpanJsonConverter.cs`](src/IO.Ably.Shared/CustomSerialisers/TimeSpanJsonConverter.cs)**
    - Uses `JToken.Load(reader)` and `JTokenType` checking

15. **[`src/IO.Ably.Shared/CustomSerialisers/DateTimeOffsetJsonConverter.cs`](src/IO.Ably.Shared/CustomSerialisers/DateTimeOffsetJsonConverter.cs)**
    - Uses `JToken.Load(reader)` and `JTokenType` checking

#### Files with JObject/JToken Usage

16. **[`src/IO.Ably.Shared/Push/DeviceDetails.cs`](src/IO.Ably.Shared/Push/DeviceDetails.cs)**
    - Uses `JObject` for Metadata and Recipient properties
    - Multiple `[JsonProperty]` attributes

17. **[`src/IO.Ably.Shared/Push/PushAdmin.cs`](src/IO.Ably.Shared/Push/PushAdmin.cs)**
    - Heavy `JObject` usage for push payloads (lines 90, 121-122, 157-163)

18. **[`src/IO.Ably.Shared/Push/PushChannelSubscription.cs`](src/IO.Ably.Shared/Push/PushChannelSubscription.cs)**
    - Uses `[JsonProperty]` attributes

19. **[`src/IO.Ably.Shared/Push/LocalDevice.cs`](src/IO.Ably.Shared/Push/LocalDevice.cs)**
    - Uses `[JsonIgnore]` for DeviceIdentityToken

20. **[`src/IO.Ably.Shared/AblyRest.cs`](src/IO.Ably.Shared/AblyRest.cs)**
    - Uses `JToken.Parse()` for request body (lines 343-344)

21. **[`src/IO.Ably.Shared/AblyAuth.cs`](src/IO.Ably.Shared/AblyAuth.cs)**
    - Uses `JObject.Parse()` for auth response (line 369)

22. **[`src/IO.Ably.Shared/Types/MessageExtras.cs`](src/IO.Ably.Shared/Types/MessageExtras.cs)**
    - Extensive `JToken` usage with custom converter
    - `[JsonIgnore]` attributes

23. **[`src/IO.Ably.Shared/Types/PresenceMessage.cs`](src/IO.Ably.Shared/Types/PresenceMessage.cs)**
    - Multiple `[JsonProperty]` attributes
    - `[JsonIgnore]` for MemberKey

24. **[`src/IO.Ably.Shared/Types/ProtocolMessage.cs`](src/IO.Ably.Shared/Types/ProtocolMessage.cs)**
    - Extensive `[JsonProperty]` usage
    - `[JsonIgnore]` for AckRequired

25. **[`src/IO.Ably.Shared/Types/ConnectionDetails.cs`](src/IO.Ably.Shared/Types/ConnectionDetails.cs)**
    - Multiple `[JsonProperty]` attributes

26. **[`src/IO.Ably.Shared/Capability.cs`](src/IO.Ably.Shared/Capability.cs)**
    - Uses `JObject.Parse()`, `JArray`, `JToken` for capability parsing

27. **[`src/IO.Ably.Shared/Statistics.cs`](src/IO.Ably.Shared/Statistics.cs)**
    - Uses `[JsonProperty]` with `NullValueHandling.Ignore` (lines 272-297)

28. **[`src/IO.Ably.Shared/HttpPaginatedResponse.cs`](src/IO.Ably.Shared/HttpPaginatedResponse.cs)**
    - Uses `JToken.Parse()` and `JArray` for response parsing

#### Realtime Components with JSON Usage

29. **[`src/IO.Ably.Shared/Realtime/RealtimeChannel.cs`](src/IO.Ably.Shared/Realtime/RealtimeChannel.cs)**
    - Uses `JObject.FromObject()` for state serialization

30. **[`src/IO.Ably.Shared/Realtime/Presence.cs`](src/IO.Ably.Shared/Realtime/Presence.cs)**
    - Uses `JArray` and `JObject.FromObject()`

31. **[`src/IO.Ably.Shared/Realtime/PresenceMap.cs`](src/IO.Ably.Shared/Realtime/PresenceMap.cs)**
    - Uses `JObject.FromObject()` for member serialization

32. **[`src/IO.Ably.Shared/Realtime/Workflows/RealtimeState.cs`](src/IO.Ably.Shared/Realtime/Workflows/RealtimeState.cs)**
    - Uses `JObject.FromObject()` and `JArray.FromObject()`

33. **[`src/IO.Ably.Shared/AblyRealtime.cs`](src/IO.Ably.Shared/AblyRealtime.cs)**
    - Uses `JObject.FromObject()` for state serialization

---

## Migration Steps

### Phase 1: Preparation (1-2 days)

#### Step 1.1: Environment Setup
```bash
# Create a new branch for migration
git checkout -b feature/newtonsoft-13-migration

# Backup current state
git tag pre-newtonsoft-13-migration
```

#### Step 1.2: Dependency Analysis
- [ ] Review all NuGet package dependencies for compatibility with Newtonsoft.Json 13.0.1
- [ ] Check MsgPack.Cli (0.9.2) compatibility
- [ ] Verify System.Threading.Channels compatibility
- [ ] Confirm ILRepack.exe compatibility with Newtonsoft.Json 13.0.1

#### Step 1.3: Test Environment Preparation
- [ ] Ensure all test projects build successfully with current version
- [ ] Run full test suite and document baseline results
- [ ] Set up test coverage reporting
- [ ] Create performance baseline metrics

### Phase 2: Code Updates (2-3 days)

#### Step 2.1: Update Project Files

**Action Items:**
1. Update all `.csproj` files with new version
2. Update `packages.config` files
3. Update all `.nuspec` files
4. Run `dotnet restore` or NuGet package restore

**Verification:**
```bash
# For SDK-style projects
dotnet list package | grep Newtonsoft.Json

# Should show version 13.0.1
```

#### Step 2.2: Update JsonHelper.cs

**File:** [`src/IO.Ably.Shared/JsonHelper.cs`](src/IO.Ably.Shared/JsonHelper.cs)

**Changes:**
```csharp
private static JsonSerializerSettings GetJsonSettings()
{
    var res = new JsonSerializerSettings
    {
        Converters = new List<JsonConverter>
        {
            new DateTimeOffsetJsonConverter(),
            new CapabilityJsonConverter(),
            new TimeSpanJsonConverter(),
            new MessageExtrasConverter(),
        },
        DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
        NullValueHandling = NullValueHandling.Ignore,
        MaxDepth = null,  // ADD THIS LINE - Maintain unlimited depth behavior
    };
    return res;
}

private static object DeserializeObject(string value, Type type)
{
    JsonSerializer jsonSerializer = GetSerializer();
    // REMOVE THIS LINE - CheckAdditionalContent is deprecated
    // jsonSerializer.CheckAdditionalContent = true;

    using (JsonTextReader jsonTextReader = new JsonTextReader(new StringReader(value)))
    {
        return jsonSerializer.Deserialize(jsonTextReader, type);
    }
}
```

#### Step 2.3: Add JsonConstructor Attributes

Review and add `[JsonConstructor]` attributes to classes with multiple constructors:

1. **[`src/IO.Ably.Shared/Types/Message.cs`](src/IO.Ably.Shared/Types/Message.cs)**
2. **[`src/IO.Ably.Shared/Types/ErrorInfo.cs`](src/IO.Ably.Shared/Types/ErrorInfo.cs)**
3. **[`src/IO.Ably.Shared/TokenDetails.cs`](src/IO.Ably.Shared/TokenDetails.cs)**
4. **[`src/IO.Ably.Shared/TokenRequest.cs`](src/IO.Ably.Shared/TokenRequest.cs)**

#### Step 2.4: Update Exception Handling

Update all files that catch generic `Exception` when parsing JSON to also catch `JsonSerializationException` and `JsonException`:

1. **[`src/IO.Ably.Shared/Types/Message.cs`](src/IO.Ably.Shared/Types/Message.cs)** - Lines 188-199, 209-220
2. **[`src/IO.Ably.Shared/Types/ErrorInfo.cs`](src/IO.Ably.Shared/Types/ErrorInfo.cs)** - Lines 183-196
3. **[`src/IO.Ably.Shared/AblyAuth.cs`](src/IO.Ably.Shared/AblyAuth.cs)** - Around line 369

#### Step 2.5: Review Custom Converters

Test all 5 custom JsonConverter implementations:
- Verify `JToken.Load(reader)` behavior
- Ensure `WriteTo(writer)` methods work correctly
- Validate type checking with `JTokenType` enum
- Test with edge cases (null, empty, deeply nested)

#### Step 2.6: Platform-Specific Updates

For Unity, Xamarin, and .NET Framework platforms:
- [ ] Update ILRepack configuration to merge Newtonsoft.Json 13.0.1
- [ ] Test merged DLL functionality
- [ ] Verify no symbol conflicts

### Phase 3: Testing (3-4 days)

#### Step 3.1: Unit Testing

**Test Categories:**
1. **Serialization/Deserialization Tests**
   - All 33+ files with JSON attributes
   - Custom converter functionality
   - Deep nesting scenarios (>64 levels)

2. **JObject/JToken Operations**
   - Test all 160+ locations using JObject/JToken
   - Verify parsing behavior
   - Check exception handling

3. **Performance Tests**
   - Serialization/deserialization speed
   - Memory allocation patterns
   - GC pressure with large payloads

#### Step 3.2: Integration Testing

**Test Scenarios:**
1. **Message Operations**
   - Publishing/receiving with various data types
   - Message extras with delta encoding
   - Encrypted messages

2. **Authentication**
   - Token request/response
   - Capability parsing
   - Error responses

3. **Push Notifications**
   - Device registration with metadata
   - Push admin operations
   - Channel subscriptions

4. **Realtime Operations**
   - Connection state serialization
   - Presence operations
   - Channel state management

#### Step 3.3: Platform-Specific Testing

**Platforms to Test:**
- [ ] .NET Framework 4.6.2+ (with ILRepack)
- [ ] .NET Standard 2.0
- [ ] .NET 6.0, 7.0, 8.0, 9.0
- [ ] Xamarin.Android (with ILRepack)
- [ ] Xamarin.iOS (with ILRepack)
- [ ] Unity (with ILRepack)
- [ ] MAUI

#### Step 3.4: Security Testing

- [ ] Test with malformed JSON (DoS protection)
- [ ] Test deeply nested JSON (>1000 levels)
- [ ] Test large payloads (>100MB)
- [ ] Test regex timeout protection with complex JSONPath

### Phase 4: Documentation and Deployment (1 day)

#### Step 4.1: Update Documentation
- [ ] Update CHANGELOG.md
- [ ] Update README.md if necessary
- [ ] Document ILRepack process for Unity/Xamarin/.NET Framework
- [ ] Create migration guide for library consumers

#### Step 4.2: Deployment Checklist
- [ ] All tests passing on all platforms
- [ ] Performance within 10% of baseline
- [ ] No memory leaks detected
- [ ] ILRepack process documented
- [ ] NuGet packages ready

---

## Code Changes Required

### Summary of Required Changes

| Component | Files | Change Type | Priority | Estimated Effort |
|-----------|-------|-------------|----------|------------------|
| Project Files | 10+ | Version update | HIGH | 1 hour |
| packages.config | 5 | Version update | HIGH | 30 min |
| NuSpec files | 3+ | Version update | HIGH | 15 min |
| JsonHelper.cs | 1 | MaxDepth, CheckAdditionalContent | HIGH | 30 min |
| JsonConstructor | 4+ | Add attributes | HIGH | 1 hour |
| Exception Handling | 3+ | Update catch blocks | HIGH | 1 hour |
| Custom Converters | 5 | Test and verify | MEDIUM | 2 hours |
| JObject/JToken Usage | 30+ | Review and test | MEDIUM | 4 hours |
| Test Updates | Many | Update assertions | LOW | 4 hours |
| ILRepack Config | 3 | Update for Unity/Xamarin/.NET Framework | MEDIUM | 2 hours |

**Total Estimated Effort:** 2-3 days for code changes + 3-4 days for testing

---

## Testing Strategy

### Test Coverage Requirements

**Minimum Coverage:** 85% for modified code
**Target Coverage:** 95% for critical paths (serialization, custom converters)

### Critical Test Areas

1. **Custom Converters (5 files)**
   - Round-trip serialization
   - Edge cases (null, empty, invalid)
   - Performance benchmarks

2. **JObject/JToken Operations (160+ locations)**
   - Parsing behavior
   - Exception handling
   - Memory usage

3. **Classes with JsonConstructor**
   - Deserialization with multiple constructors
   - Parameter matching
   - Default value handling

4. **Deep Nesting**
   - Objects deeper than 64 levels
   - MaxDepth = null verification
   - Performance impact

5. **Security**
   - Malformed JSON handling
   - Large payload protection
   - Regex timeout protection

### Performance Benchmarks

Create benchmarks for:
- `JsonHelper.Serialize()` and `JsonHelper.Deserialize()`
- Custom converter performance
- JObject/JToken operations
- Deep object graphs

**Acceptance Criteria:**
- Performance within 10% of version 9.0.1
- No memory leaks
- GC pressure comparable or better

---

## Rollback Plan

### Rollback Triggers

Initiate rollback if:
1. Critical tests fail on any supported platform
2. Performance degradation >20%
3. Memory leaks detected
4. ILRepack issues with Unity/Xamarin/.NET Framework
5. Production issues within 48 hours of release

### Rollback Procedure

1. **Revert Code Changes**
   ```bash
   git checkout pre-newtonsoft-13-migration
   git checkout -b rollback/newtonsoft-9-restore
   ```

2. **Restore Package Versions**
   - Revert all project files to 9.0.1
   - Restore packages.config files
   - Revert nuspec files

3. **Verify Rollback**
   - Run full test suite
   - Verify all platforms build
   - Test ILRepack with old version

4. **Communication**
   - Update GitHub issue #1303
   - Document rollback reason
   - Plan remediation

---

## Platform-Specific Considerations

### Unity, Xamarin, and .NET Framework (ILRepack)

Since Unity, Xamarin, and .NET Framework will merge Newtonsoft.Json and Ably DLLs using ILRepack:

1. **Benefits:**
   - No binding redirect issues
   - Single DLL deployment
   - Simplified version management

2. **Testing Requirements:**
   - Verify ILRepack works with Newtonsoft.Json 13.0.1
   - Test merged DLL functionality
   - Check for symbol conflicts
   - Validate size of merged DLL

3. **ILRepack Configuration:**
   ```xml
   <!-- Example ILRepack command -->
   ILRepack.exe /out:IO.Ably.Merged.dll IO.Ably.dll Newtonsoft.Json.dll
   ```

### .NET Framework

- Uses ILRepack to merge DLLs (same as Unity and Xamarin)
- No binding redirects needed since DLLs are merged
- Single DLL deployment simplifies version management

### .NET Standard 2.0 / .NET 6+

- No special considerations
- Direct PackageReference update sufficient

---

## Known Issues and Workarounds

### Issue 1: CheckAdditionalContent Deprecation

**Solution:** Remove the property from JsonHelper.cs. Consider implementing custom validation if strict JSON validation is required.

### Issue 2: Deep Object Nesting

**Solution:** Set `MaxDepth = null` in JsonSerializerSettings to maintain backward compatibility.

### Issue 3: Exception Type Changes

**Solution:** Update catch blocks to handle both `JsonSerializationException` and legacy exception types.

### Issue 4: ILRepack Compatibility

**Solution:** Test thoroughly with merged DLLs. Consider alternative merging tools if issues arise.

---

## Success Criteria

### Must Have (Blocking)
- [ ] All project files updated to Newtonsoft.Json 13.0.1
- [ ] All unit tests passing (10,000+ tests)
- [ ] All integration tests passing
- [ ] No security vulnerabilities
- [ ] Performance within 10% of baseline
- [ ] ILRepack working for Unity/Xamarin/.NET Framework

### Should Have (Important)
- [ ] Code coverage ≥85% for modified code
- [ ] Documentation updated
- [ ] No new compiler warnings
- [ ] Memory usage comparable or better

### Nice to Have (Optional)
- [ ] Performance improvements documented
- [ ] Technical debt reduced
- [ ] Additional security tests added

---

## Appendix A: Complete File List

### Files with [JsonProperty] Attributes (30+ files)
- TokenResponse.cs
- Push/DeviceDetails.cs
- Rest/ChannelDetails.cs
- Push/PushChannelSubscription.cs
- TokenRequest.cs
- Statistics.cs
- TokenDetails.cs
- Realtime/RecoveryKeyContext.cs
- Types/ConnectionDetails.cs
- Types/PresenceMessage.cs
- Types/ProtocolMessage.cs
- Types/ErrorInfo.cs
- Types/Message.cs
- Types/AuthDetails.cs

### Files with [JsonIgnore] Attributes (6 files)
- Push/LocalDevice.cs
- Types/PresenceMessage.cs
- Types/Message.cs
- Types/MessageExtras.cs
- Types/ProtocolMessage.cs
- ClientOptions.cs

### Files with JObject/JToken Usage (33+ files)
- All files listed in Impact Assessment section

### Custom JsonConverter Implementations (5 files)
- MessageDataConverter.cs
- CapabilityJsonConverter.cs
- MessageExtrasConverter.cs
- TimeSpanJsonConverter.cs
- DateTimeOffsetJsonConverter.cs

---

## Appendix B: Testing Checklist

### Unit Tests
- [ ] All model serialization/deserialization
- [ ] Custom converter tests
- [ ] JsonConstructor attribute tests
- [ ] Exception handling tests
- [ ] Deep nesting tests (>64 levels)
- [ ] Null value handling tests

### Integration Tests
- [ ] REST API operations
- [ ] Realtime connections
- [ ] Push notifications
- [ ] Authentication flows
- [ ] Message encryption
- [ ] Channel operations

### Performance Tests
- [ ] Serialization benchmarks
- [ ] Deserialization benchmarks
- [ ] Memory allocation tests
- [ ] GC pressure tests
- [ ] Large payload tests (>100MB)
- [ ] Deep nesting performance