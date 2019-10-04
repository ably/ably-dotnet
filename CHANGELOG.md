# Change Log

## [Unreleased](https://github.com/ably/ably-dotnet/tree/1.1.14)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/1.1.13...1.1.14)

**Implemented enhancements:**

- TM3 Improvements [\#369](https://github.com/ably/ably-dotnet/issues/369)

**Fixed bugs:**

- Token Expiry error when QueryTime is set to true [\#374](https://github.com/ably/ably-dotnet/issues/374)

**Closed issues:**

- IO.Ably.AblyAuth RequestTokenAsync timeout exception not catchable [\#366](https://github.com/ably/ably-dotnet/issues/366)

**Merged pull requests:**

- Fix issue where incorrect time was used to check Token validity [\#376](https://github.com/ably/ably-dotnet/pull/376) ([marto83](https://github.com/marto83))
- Add fromEncoded and fromEncodedArray that accept json string [\#370](https://github.com/ably/ably-dotnet/pull/370) ([marto83](https://github.com/marto83))
- ClientId in options should not force token auth  \(RSA7e2\) [\#357](https://github.com/ably/ably-dotnet/pull/357) ([withakay](https://github.com/withakay))


[Full Changelog](https://github.com/ably/ably-dotnet/compare/1.1.12...1.1.13)

**Implemented enhancements:**

- Ensure request method accepts UPDATE, PATCH & DELETE verbs [\#246](https://github.com/ably/ably-dotnet/issues/246)
- Disable fallback hosts option - hot fix [\#216](https://github.com/ably/ably-dotnet/issues/216)
- Integrate a Linter/Code formatting tool into the build process [\#179](https://github.com/ably/ably-dotnet/issues/179)
- Ably dll and iOS app size increased [\#134](https://github.com/ably/ably-dotnet/issues/134)
- Ability to set the JsonSerializerSettings [\#92](https://github.com/ably/ably-dotnet/issues/92)
- Realtime: Presence [\#8](https://github.com/ably/ably-dotnet/issues/8)

**Fixed bugs:**

- Error from authUrl should give some indication that it's from the authUrl [\#252](https://github.com/ably/ably-dotnet/issues/252)
- AttachAsync\(\) doesn't timeout on connection state changes [\#117](https://github.com/ably/ably-dotnet/issues/117)

**Closed issues:**

- Channel didn't attach within the default timeout; Code: 50000, Intermittent [\#115](https://github.com/ably/ably-dotnet/issues/115)
- Update docs about using with Xamarin [\#77](https://github.com/ably/ably-dotnet/issues/77)

**Merged pull requests:**

- RunInBackground bug [\#367](https://github.com/ably/ably-dotnet/pull/367) ([withakay](https://github.com/withakay))

## [1.1.12](https://github.com/ably/ably-dotnet/tree/1.1.12) 

[Full Changelog](https://github.com/ably/ably-dotnet/compare/1.1.11...1.1.12)

**Fixed bugs:**

- Presence re-entry requirement change for 1.1 [\#361](https://github.com/ably/ably-dotnet/issues/361)
- Presence not entered on recovered connection [\#329](https://github.com/ably/ably-dotnet/issues/329)
- Investigate report of PublishAsync hanging/timing out [\#314](https://github.com/ably/ably-dotnet/issues/314)

**Merged pull requests:**

- Reenter after short disconnect [\#358](https://github.com/ably/ably-dotnet/pull/358) ([withakay](https://github.com/withakay))

## [1.1.10](https://github.com/ably/ably-dotnet/tree/1.1.10)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/1.1.9-beta1...1.1.10)

**Fixed bugs:**

- history.NextAsync\(\) throws 40000 exception [\#352](https://github.com/ably/ably-dotnet/issues/352)
- Ably 1.1.8 - The type initializer for 'IO.Ably.Defaults' threw an exception. [\#349](https://github.com/ably/ably-dotnet/issues/349)
- Crash in 1.1.6 on iOS & Android with Xamarin [\#346](https://github.com/ably/ably-dotnet/issues/346)

**Merged pull requests:**

- split link header values on comma to handle platform implementation differences [\#353](https://github.com/ably/ably-dotnet/pull/353) ([withakay](https://github.com/withakay))
- Presence not re-entering after connect disconnect fix [\#350](https://github.com/ably/ably-dotnet/pull/350) ([withakay](https://github.com/withakay))

## [1.1.9-beta1](https://github.com/ably/ably-dotnet/tree/1.1.9-beta1)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/1.1.8...1.1.9-beta1)

**Proposed pull requests:**

https://github.com/ably/ably-dotnet/pull/350

## [1.1.8](https://github.com/ably/ably-dotnet/tree/1.1.8)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/1.1.6...1.1.7)

**Fixed bugs:**

- NRE in IO.Ably.Transport.MsWebSocketTransport.ConnectAndStartListening and SemaphoreSlim.Wait [\#334](https://github.com/ably/ably-dotnet/issues/334)

**Merged pull requests:**

- Fix for issue \#346  [\#347](https://github.com/ably/ably-dotnet/pull/347) ([withakay](https://github.com/withakay))

## [1.1.6](https://github.com/ably/ably-dotnet/tree/1.1.6)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/1.1.5...1.1.6)

**Implemented enhancements:**

- Idempotent publishing is not enabled in the upcoming 1.1 release [\#275](https://github.com/ably/ably-dotnet/issues/275)
- Unity support [\#169](https://github.com/ably/ably-dotnet/issues/169)

**Fixed bugs:**

- JsonReader error on Xamarin [\#325](https://github.com/ably/ably-dotnet/issues/325)

**Closed issues:**

- NRE when switching networks [\#200](https://github.com/ably/ably-dotnet/issues/200)
- Prefer the American English spelling for Initialise\(d\) [\#191](https://github.com/ably/ably-dotnet/issues/191)

**Merged pull requests:**

- Transient Publishing [\#343](https://github.com/ably/ably-dotnet/pull/343) ([withakay](https://github.com/withakay))

## [1.1.5](https://github.com/ably/ably-dotnet/tree/1.1.5)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/1.1.4...1.1.5)

**Merged pull requests:**

- PublishAsync improvement [\#341](https://github.com/ably/ably-dotnet/pull/341) ([withakay](https://github.com/withakay))

## [1.1.4](https://github.com/ably/ably-dotnet/tree/1.1.4)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/1.1.3...1.1.4)

**Merged pull requests:**

- Dispose guarding [\#339](https://github.com/ably/ably-dotnet/pull/339) ([withakay](https://github.com/withakay))
- Issue 334 follow up [\#338](https://github.com/ably/ably-dotnet/pull/338) ([withakay](https://github.com/withakay))

## [1.1.3](https://github.com/ably/ably-dotnet/tree/1.1.3)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/1.1.2...1.1.3)

**Implemented enhancements:**

- Add test for JWT token [\#210](https://github.com/ably/ably-dotnet/issues/210)

**Fixed bugs:**

- Collection was modified in ConnectionManager [\#267](https://github.com/ably/ably-dotnet/issues/267)
- Socket is null in MsWebSocketTransport [\#264](https://github.com/ably/ably-dotnet/issues/264)

**Merged pull requests:**

- Fix for Issue 334, incorrectly disposing of a blocking collection [\#335](https://github.com/ably/ably-dotnet/pull/335) ([withakay](https://github.com/withakay))
- Add InnerException to ErrorInfo [\#328](https://github.com/ably/ably-dotnet/pull/328) ([withakay](https://github.com/withakay))
- Idempotent defaults to off for version \< 1.2 [\#327](https://github.com/ably/ably-dotnet/pull/327) ([withakay](https://github.com/withakay))
- update xunit and moq to latest versions [\#326](https://github.com/ably/ably-dotnet/pull/326) ([withakay](https://github.com/withakay))

## [1.1.2](https://github.com/ably/ably-dotnet/tree/1.1.2)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/1.1.1...1.1.2)

**Implemented enhancements:**

- Clean up Log.Debug calls [\#229](https://github.com/ably/ably-dotnet/issues/229)

**Closed issues:**

- presence myMembers map implementation [\#302](https://github.com/ably/ably-dotnet/issues/302)
- Test should verify message was processed [\#299](https://github.com/ably/ably-dotnet/issues/299)
- Presence \(v1.1\) if QueueMessages = false should raise an error [\#298](https://github.com/ably/ably-dotnet/issues/298)
- Unnecessary disconnect/connect cycle on token authentication [\#276](https://github.com/ably/ably-dotnet/issues/276)

**Merged pull requests:**

- HistoryRequestParams fix [\#323](https://github.com/ably/ably-dotnet/pull/323) ([withakay](https://github.com/withakay))
- Code cleanup [\#322](https://github.com/ably/ably-dotnet/pull/322) ([withakay](https://github.com/withakay))

## [1.1.1](https://github.com/ably/ably-dotnet/tree/1.1.1)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/1.1.0...1.1.1)

**Merged pull requests:**

- Fix for issue 313 [\#318](https://github.com/ably/ably-dotnet/pull/318) ([withakay](https://github.com/withakay))

**Fixed bugs:**

- Thread leak through disconnect/resume sequence [\#313](https://github.com/ably/ably-dotnet/issues/313)

## [1.1.0](https://github.com/ably/ably-dotnet/tree/1.1.0)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/1.1.0-beta1...1.1.0)

**Closed issues:**

- Release nugget still shows 0.8.11 [\#304](https://github.com/ably/ably-dotnet/issues/304)

**Merged pull requests:**

- RTL11 test regression fix [\#315](https://github.com/ably/ably-dotnet/pull/315) ([withakay](https://github.com/withakay))
- RTP16b fix [\#312](https://github.com/ably/ably-dotnet/pull/312) ([withakay](https://github.com/withakay))
- Fix for issue \#299.  [\#311](https://github.com/ably/ably-dotnet/pull/311) ([withakay](https://github.com/withakay))
- RTP17b test identification [\#310](https://github.com/ably/ably-dotnet/pull/310) ([withakay](https://github.com/withakay))
- TM3 [\#309](https://github.com/ably/ably-dotnet/pull/309) ([withakay](https://github.com/withakay))
- RTL15/a [\#308](https://github.com/ably/ably-dotnet/pull/308) ([withakay](https://github.com/withakay))
- modifications to ErrorInfo to meet TI4 & TI5 spec [\#307](https://github.com/ably/ably-dotnet/pull/307) ([withakay](https://github.com/withakay))
- JWT support [\#306](https://github.com/ably/ably-dotnet/pull/306) ([withakay](https://github.com/withakay))
- fix test for idempotent rest publishing for version 1.1 of spec. [\#305](https://github.com/ably/ably-dotnet/pull/305) ([withakay](https://github.com/withakay))
- RSA8e, RSA9h and RSA10j [\#301](https://github.com/ably/ably-dotnet/pull/301) ([withakay](https://github.com/withakay))

## [1.1.0-beta1](https://github.com/ably/ably-dotnet/tree/1.1.0-beta1)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/0.8.11...1.1.0-beta1)

**Fixed bugs:**

- Channel apparently successfully attaches, but OnAttachTimeout fires anyway [\#205](https://github.com/ably/ably-dotnet/issues/205)

**Closed issues:**

- .NET Core 2.0 confusion [\#266](https://github.com/ably/ably-dotnet/issues/266)
- Should the AblyRest class be an injected singleton? [\#260](https://github.com/ably/ably-dotnet/issues/260)
- readme: use authcallback, don't instantiate the lib with literal token [\#253](https://github.com/ably/ably-dotnet/issues/253)
- IO.Ably.Realtime.RealtimeChannel.PublishImpl [\#251](https://github.com/ably/ably-dotnet/issues/251)
- support for windows 7 in ably 0.8.11 [\#247](https://github.com/ably/ably-dotnet/issues/247)
- Presence [\#235](https://github.com/ably/ably-dotnet/issues/235)
- Presence sync doesn't complete on the first chance [\#204](https://github.com/ably/ably-dotnet/issues/204)
- There is already one outstanding 'SendAsync' call for this WebSocket instance  [\#101](https://github.com/ably/ably-dotnet/issues/101)

**Merged pull requests:**

- v1.1 release [\#300](https://github.com/ably/ably-dotnet/pull/300) ([withakay](https://github.com/withakay))
- Known limitations section in README [\#297](https://github.com/ably/ably-dotnet/pull/297) ([Srushtika](https://github.com/Srushtika))
- ChannelStateChange \(TH\) [\#296](https://github.com/ably/ably-dotnet/pull/296) ([withakay](https://github.com/withakay))
- RTL13a, RTL13b, RTL13c and RTL4f [\#295](https://github.com/ably/ably-dotnet/pull/295) ([withakay](https://github.com/withakay))
- RTL6c5 [\#294](https://github.com/ably/ably-dotnet/pull/294) ([withakay](https://github.com/withakay))
- RTN16b and RTN16f [\#293](https://github.com/ably/ably-dotnet/pull/293) ([withakay](https://github.com/withakay))
- RTN15g and subsections [\#292](https://github.com/ably/ably-dotnet/pull/292) ([withakay](https://github.com/withakay))
- RTN15h test fix [\#291](https://github.com/ably/ably-dotnet/pull/291) ([withakay](https://github.com/withakay))
- RTN15c5 [\#289](https://github.com/ably/ably-dotnet/pull/289) ([withakay](https://github.com/withakay))
- RTN15c4 [\#288](https://github.com/ably/ably-dotnet/pull/288) ([withakay](https://github.com/withakay))
- RTN15c3 [\#287](https://github.com/ably/ably-dotnet/pull/287) ([withakay](https://github.com/withakay))
- RTN15c2 [\#286](https://github.com/ably/ably-dotnet/pull/286) ([withakay](https://github.com/withakay))
- RTN15c1 [\#285](https://github.com/ably/ably-dotnet/pull/285) ([withakay](https://github.com/withakay))
- RTP16c [\#283](https://github.com/ably/ably-dotnet/pull/283) ([withakay](https://github.com/withakay))
- RTP16b [\#282](https://github.com/ably/ably-dotnet/pull/282) ([withakay](https://github.com/withakay))
- Test for RTP16a [\#281](https://github.com/ably/ably-dotnet/pull/281) ([withakay](https://github.com/withakay))
- Test cover for RTP5c3 [\#280](https://github.com/ably/ably-dotnet/pull/280) ([withakay](https://github.com/withakay))
- Tests for RTP5c2 [\#279](https://github.com/ably/ably-dotnet/pull/279) ([withakay](https://github.com/withakay))
- Test for RTP2e [\#278](https://github.com/ably/ably-dotnet/pull/278) ([withakay](https://github.com/withakay))
- Notes on how to reference a project from source. [\#277](https://github.com/ably/ably-dotnet/pull/277) ([withakay](https://github.com/withakay))
- Unity compatibility update [\#274](https://github.com/ably/ably-dotnet/pull/274) ([withakay](https://github.com/withakay))
- Merge RTL3c into RTN7c [\#273](https://github.com/ably/ably-dotnet/pull/273) ([withakay](https://github.com/withakay))
- Merge RTL3d into RTL3c [\#272](https://github.com/ably/ably-dotnet/pull/272) ([withakay](https://github.com/withakay))
- Rtn14a+rtn14b+rtn15h [\#271](https://github.com/ably/ably-dotnet/pull/271) ([withakay](https://github.com/withakay))
- Test tweaks for ci [\#270](https://github.com/ably/ably-dotnet/pull/270) ([withakay](https://github.com/withakay))
- RTP17 tests [\#269](https://github.com/ably/ably-dotnet/pull/269) ([withakay](https://github.com/withakay))
- RTP5a & RTP5b [\#265](https://github.com/ably/ably-dotnet/pull/265) ([withakay](https://github.com/withakay))
- properties should be placed before the constructor [\#263](https://github.com/ably/ably-dotnet/pull/263) ([withakay](https://github.com/withakay))
- RTP17 and RTP17a tests [\#262](https://github.com/ably/ably-dotnet/pull/262) ([withakay](https://github.com/withakay))
- RTP19, RTP19a and RTP6b [\#261](https://github.com/ably/ably-dotnet/pull/261) ([withakay](https://github.com/withakay))
- RTP11 and subsections [\#258](https://github.com/ably/ably-dotnet/pull/258) ([withakay](https://github.com/withakay))
- Test tweaks  [\#257](https://github.com/ably/ably-dotnet/pull/257) ([withakay](https://github.com/withakay))
- RTN19b [\#256](https://github.com/ably/ably-dotnet/pull/256) ([withakay](https://github.com/withakay))
- RTP3 [\#255](https://github.com/ably/ably-dotnet/pull/255) ([withakay](https://github.com/withakay))
- Update README with better AuthCallback example [\#254](https://github.com/ably/ably-dotnet/pull/254) ([withakay](https://github.com/withakay))
- ADded stylecop.json [\#250](https://github.com/ably/ably-dotnet/pull/250) ([withakay](https://github.com/withakay))
- RSL1k  - Idempotent rest publishing [\#249](https://github.com/ably/ably-dotnet/pull/249) ([withakay](https://github.com/withakay))
- Update README with not on PCLs [\#248](https://github.com/ably/ably-dotnet/pull/248) ([withakay](https://github.com/withakay))
- RTL2d tests [\#245](https://github.com/ably/ably-dotnet/pull/245) ([withakay](https://github.com/withakay))
- RTL11 & RTL11a [\#244](https://github.com/ably/ably-dotnet/pull/244) ([withakay](https://github.com/withakay))
- RTL2b, RTL2f, RTL2g, RTL12 & RTL3d [\#243](https://github.com/ably/ably-dotnet/pull/243) ([withakay](https://github.com/withakay))
- RTL3d [\#242](https://github.com/ably/ably-dotnet/pull/242) ([withakay](https://github.com/withakay))
- RTL3e [\#241](https://github.com/ably/ably-dotnet/pull/241) ([withakay](https://github.com/withakay))
- RTL3c [\#240](https://github.com/ably/ably-dotnet/pull/240) ([withakay](https://github.com/withakay))
- RTL2 Channel States and Events [\#239](https://github.com/ably/ably-dotnet/pull/239) ([withakay](https://github.com/withakay))
- RSC19 [\#238](https://github.com/ably/ably-dotnet/pull/238) ([withakay](https://github.com/withakay))
- Update README.md to cover .NET Core support [\#236](https://github.com/ably/ably-dotnet/pull/236) ([withakay](https://github.com/withakay))
- RTC8 and RTC8a subsections [\#234](https://github.com/ably/ably-dotnet/pull/234) ([withakay](https://github.com/withakay))
- RSA10a [\#233](https://github.com/ably/ably-dotnet/pull/233) ([withakay](https://github.com/withakay))
- RTN12d [\#232](https://github.com/ably/ably-dotnet/pull/232) ([withakay](https://github.com/withakay))
- RTN11b, RTN11c, RTN11d [\#230](https://github.com/ably/ably-dotnet/pull/230) ([withakay](https://github.com/withakay))
- RTN24, RTN21 and partial RTN4h [\#228](https://github.com/ably/ably-dotnet/pull/228) ([withakay](https://github.com/withakay))
- RTN22 and RTN22a [\#226](https://github.com/ably/ably-dotnet/pull/226) ([withakay](https://github.com/withakay))
- RTN7c Suspended [\#225](https://github.com/ably/ably-dotnet/pull/225) ([withakay](https://github.com/withakay))
- RSA4 small tweak [\#224](https://github.com/ably/ably-dotnet/pull/224) ([withakay](https://github.com/withakay))
- RTN15i [\#223](https://github.com/ably/ably-dotnet/pull/223) ([withakay](https://github.com/withakay))
- Release 0.8.11 [\#222](https://github.com/ably/ably-dotnet/pull/222) ([withakay](https://github.com/withakay))
- RTN14a, RTN14b, RTN15h [\#221](https://github.com/ably/ably-dotnet/pull/221) ([withakay](https://github.com/withakay))
- TR4 & AD1 [\#215](https://github.com/ably/ably-dotnet/pull/215) ([withakay](https://github.com/withakay))
- RTN4h, TA1, TA2, TA3, TA5 [\#214](https://github.com/ably/ably-dotnet/pull/214) ([withakay](https://github.com/withakay))
- RSA4c+d [\#212](https://github.com/ably/ably-dotnet/pull/212) ([withakay](https://github.com/withakay))
- AuthCallback return an AuthCallbackResult [\#203](https://github.com/ably/ably-dotnet/pull/203) ([withakay](https://github.com/withakay))
- RSA4a+b [\#201](https://github.com/ably/ably-dotnet/pull/201) ([withakay](https://github.com/withakay))


## [0.8.11](https://github.com/ably/ably-dotnet/tree/0.8.11)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/0.8.10...0.8.11)

**Fixed bugs:**

- Channel apparently successfully attaches, but OnAttachTimeout fires anyway [\#205](https://github.com/ably/ably-dotnet/issues/205)

**Merged pull requests:**

- Issue 205 [\#213](https://github.com/ably/ably-dotnet/pull/213) ([withakay](https://github.com/withakay))


## [0.8.10](https://github.com/ably/ably-dotnet/tree/0.8.10)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/0.8.9...0.8.10)

**Fixed bugs:**

- README is duplicated [\#184](https://github.com/ably/ably-dotnet/issues/184)
- Investigate report of System.Net.Http needing to be installed  [\#180](https://github.com/ably/ably-dotnet/issues/180)
- Use of task.ConfigureAwait\(false\) can give rise to out of order messages [\#143](https://github.com/ably/ably-dotnet/issues/143)
- 8.5 Android Presence+PresenceMap.EndSync\(\) [\#138](https://github.com/ably/ably-dotnet/issues/138)
- Channel state changes twice [\#125](https://github.com/ably/ably-dotnet/issues/125)
- App doesn't receive messages after a while [\#122](https://github.com/ably/ably-dotnet/issues/122)

**Closed issues:**

- Consider exposing the iOS and Android native SDKs as a NuGet package [\#202](https://github.com/ably/ably-dotnet/issues/202)
- Java.IO.IOException with Xamarin [\#172](https://github.com/ably/ably-dotnet/issues/172)

**Merged pull requests:**

- regex would only match single digit [\#220](https://github.com/ably/ably-dotnet/pull/220) ([withakay](https://github.com/withakay))
- Hotfix for issue 216 [\#217](https://github.com/ably/ably-dotnet/pull/217) ([withakay](https://github.com/withakay))
- updated Supported Platforms section in README [\#208](https://github.com/ably/ably-dotnet/pull/208) ([withakay](https://github.com/withakay))
- namespace was not updated when the project name was updated [\#207](https://github.com/ably/ably-dotnet/pull/207) ([withakay](https://github.com/withakay))
- Fix delay in initial presence sync [\#206](https://github.com/ably/ably-dotnet/pull/206) ([ashikns](https://github.com/ashikns))
- Deduplicated the README and merged edits between the two halves [\#185](https://github.com/ably/ably-dotnet/pull/185) ([withakay](https://github.com/withakay))

## [0.8.9](https://github.com/ably/ably-dotnet/tree/0.8.9)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/0.8.8...0.8.9)

**Closed issues:**

- v.87 The current state \[Suspended\] does not allow messages to be sent.; Code: 500 [\#171](https://github.com/ably/ably-dotnet/issues/171)

**Merged pull requests:**

- Update package deps [\#181](https://github.com/ably/ably-dotnet/pull/181) ([withakay](https://github.com/withakay))


## [0.8.8](https://github.com/ably/ably-dotnet/tree/0.8.8) (2018-02-06)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/0.8.7...0.8.8)

**Fixed bugs:**

- Channel options consistently being overridden on realtime channels \(v0.8.6\) [\#167](https://github.com/ably/ably-dotnet/issues/167)
- 0.8.4.8 - Unable to publish in detached or failed state [\#135](https://github.com/ably/ably-dotnet/issues/135)

**Merged pull requests:**

- Null check WebSocket Client [\#177](https://github.com/ably/ably-dotnet/pull/177) ([withakay](https://github.com/withakay))
- Circleci [\#176](https://github.com/ably/ably-dotnet/pull/176) ([withakay](https://github.com/withakay))
- Test and ci improvements [\#175](https://github.com/ably/ably-dotnet/pull/175) ([withakay](https://github.com/withakay))
- Use a shared project for tests to facilitate cross platform testing. [\#174](https://github.com/ably/ably-dotnet/pull/174) ([withakay](https://github.com/withakay))

## [0.8.7](https://github.com/ably/ably-dotnet/tree/0.8.6) (2017-12-22)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/0.8.7...HEAD)

**Implemented enhancements:**

- Add reauth capability [\#63](https://github.com/ably/ably-dotnet/issues/63)

**Fixed bugs:**

- After upgrade to 0.8.6, System.Net.WebSockets.WebSocketException [\#166](https://github.com/ably/ably-dotnet/issues/166)
- 0.8.5 Android connection issue [\#129](https://github.com/ably/ably-dotnet/issues/129)
- RealtimePresence possibly broken [\#114](https://github.com/ably/ably-dotnet/issues/114)

**Closed issues:**

- "The WebSocket protocol is not supported on this platform." on Windows 7 [\#165](https://github.com/ably/ably-dotnet/issues/165)
-  customer getting an exception from MessageWebSocketMessageReceivedEventArgs.GetDataReader\(\) in 0.8.6 beta 3 [\#164](https://github.com/ably/ably-dotnet/issues/164)

**Merged pull requests:**

- Fix duplicate Dependencies section in readme [\#170](https://github.com/ably/ably-dotnet/pull/170) ([withakay](https://github.com/withakay))
- fix for issue 167 [\#168](https://github.com/ably/ably-dotnet/pull/168) ([withakay](https://github.com/withakay))


## [0.8.6](https://github.com/ably/ably-dotnet/tree/0.8.6) (2017-11-28)

Changes since 0.8.5

**Implemented enhancements:**

- Add Release Process section to README [\#149](https://github.com/ably/ably-dotnet/issues/149)
- Per-instance loggers [\#33](https://github.com/ably/ably-dotnet/issues/33)
- Add documentation to README about which platforms are supported  [\#28](https://github.com/ably/ably-dotnet/issues/28)
- Populate message/presencemessage Id, ConnectionId, Timestamp [\#113](https://github.com/ably/ably-dotnet/issues/113)
- Fix HttpRequest & HttpRetry timeouts [\#95](https://github.com/ably/ably-dotnet/issues/95)
- Universal Windows Platform support [\#90](https://github.com/ably/ably-dotnet/issues/90)

**Fixed bugs:**

- System.Net.Http conflict [\#162](https://github.com/ably/ably-dotnet/issues/162)
- Xamarin 0.8.5+ System.NotImplementedException: The method or operation is not implemented.   at System.Net.Sockets.SocketAsyncEventArgs.FinishOperationAsyncFailur [\#161](https://github.com/ably/ably-dotnet/issues/161)
- iOS issue with MsWebsocket transport [\#151](https://github.com/ably/ably-dotnet/issues/151)
- Failure of history API call means channels no longer attach [\#116](https://github.com/ably/ably-dotnet/issues/116)
- Crash on android [\#109](https://github.com/ably/ably-dotnet/issues/109)
- When a Channel Fails to Resume After Disconnection, a Detached Event isn't Fired [\#108](https://github.com/ably/ably-dotnet/issues/108)
- Xamarin Android crash [\#96](https://github.com/ably/ably-dotnet/issues/96)
- Possible NewtonSoft incompatibility [\#91](https://github.com/ably/ably-dotnet/issues/91)
- authCallback does not support TokenRequest or token string [\#75](https://github.com/ably/ably-dotnet/issues/75)

**Closed issues:**

- Xamarin Null Reference Error - 0.8.6 - beta 2 [\#126](https://github.com/ably/ably-dotnet/issues/126)
- Concurrency issue in PresenceMap [\#124](https://github.com/ably/ably-dotnet/issues/124)
- Beta SDK depends on "Nito.AsyncEx" version="5.0.0-pre-02" [\#121](https://github.com/ably/ably-dotnet/issues/121)
- Failed to install Ably into a PCL project [\#120](https://github.com/ably/ably-dotnet/issues/120)
- Realtime chat example not working; issue with Nito.AsyncEX dep? [\#119](https://github.com/ably/ably-dotnet/issues/119)
- Request mac does not match [\#118](https://github.com/ably/ably-dotnet/issues/118)
- NullReference on iOS Xamarin [\#112](https://github.com/ably/ably-dotnet/issues/112)
- Token Authentication -  Unexpected error :Can not convert Object to String.; Code: 50000 [\#110](https://github.com/ably/ably-dotnet/issues/110)
- ConnectAsyncExtension.SocketConnectCompleted [\#105](https://github.com/ably/ably-dotnet/issues/105)
- ASP.NET Core support [\#93](https://github.com/ably/ably-dotnet/issues/93)
- Minor Crypto spec updates [\#38](https://github.com/ably/ably-dotnet/issues/38)

**Merged pull requests:**

- Remove websockets4net and Use System.Net.WebSockets exclusively  [\#163](https://github.com/ably/ably-dotnet/pull/163) ([withakay](https://github.com/withakay))
- Test improvements [\#160](https://github.com/ably/ably-dotnet/pull/160) ([withakay](https://github.com/withakay))
- Added presence features covering RTP2 and others [\#157](https://github.com/ably/ably-dotnet/pull/157) ([withakay](https://github.com/withakay))
- Use a regex to check the PATCH portion of the version string, [\#155](https://github.com/ably/ably-dotnet/pull/155) ([withakay](https://github.com/withakay))
- Update to allow building and packaging with Appveyor [\#154](https://github.com/ably/ably-dotnet/pull/154) ([withakay](https://github.com/withakay))
- Build script detects xunit runner [\#153](https://github.com/ably/ably-dotnet/pull/153) ([withakay](https://github.com/withakay))
- Update release process [\#152](https://github.com/ably/ably-dotnet/pull/152) ([withakay](https://github.com/withakay))
- Add Release Process [\#150](https://github.com/ably/ably-dotnet/pull/150) ([withakay](https://github.com/withakay))
- Use MSGPACK compiler flag to add/remove Protocol.MsgPack [\#148](https://github.com/ably/ably-dotnet/pull/148) ([withakay](https://github.com/withakay))
- Improve the XML Docs for AuthCallback [\#146](https://github.com/ably/ably-dotnet/pull/146) ([withakay](https://github.com/withakay))
- Make MsgPack optional and disabled by default [\#145](https://github.com/ably/ably-dotnet/pull/145) ([withakay](https://github.com/withakay))
- Test improvements & skip some tests. [\#144](https://github.com/ably/ably-dotnet/pull/144) ([withakay](https://github.com/withakay))
- simplified the message queue logic in ConnectionManager [\#142](https://github.com/ably/ably-dotnet/pull/142) ([withakay](https://github.com/withakay))
- Simplify now provider [\#140](https://github.com/ably/ably-dotnet/pull/140) ([withakay](https://github.com/withakay))
- Injectable logger [\#139](https://github.com/ably/ably-dotnet/pull/139) ([withakay](https://github.com/withakay))
- Injectable INowProvider [\#137](https://github.com/ably/ably-dotnet/pull/137) ([withakay](https://github.com/withakay))
- Updates to some test that rely on a delays [\#136](https://github.com/ably/ably-dotnet/pull/136) ([withakay](https://github.com/withakay))
- A fix to comply with RTL4b  [\#133](https://github.com/ably/ably-dotnet/pull/133) ([withakay](https://github.com/withakay))
- test AttachAwaitShouldtimeoutIfStateChanges would frequently fail [\#132](https://github.com/ably/ably-dotnet/pull/132) ([withakay](https://github.com/withakay))
- update the rest spec tests to use the InternalLogger [\#131](https://github.com/ably/ably-dotnet/pull/131) ([withakay](https://github.com/withakay))
- Removed obsolete assembly binding redirects from the Test project [\#130](https://github.com/ably/ably-dotnet/pull/130) ([withakay](https://github.com/withakay))
- Update README.md [\#128](https://github.com/ably/ably-dotnet/pull/128) ([withakay](https://github.com/withakay))
- Isolated logger for reliable testing [\#127](https://github.com/ably/ably-dotnet/pull/127) ([withakay](https://github.com/withakay))



## [0.8.6-beta4](https://github.com/ably/ably-dotnet/tree/0.8.6-beta4) (2017-11-24)
[Full Changelog](https://github.com/ably/ably-dotnet/compare/0.8.6-beta3...0.8.6-beta4)

**Implemented enhancements:**

- Add Release Process section to README [\#149](https://github.com/ably/ably-dotnet/issues/149)
- Per-instance loggers [\#33](https://github.com/ably/ably-dotnet/issues/33)
- Add documentation to README about which platforms are supported  [\#28](https://github.com/ably/ably-dotnet/issues/28)

**Fixed bugs:**

- System.Net.Http conflict [\#162](https://github.com/ably/ably-dotnet/issues/162)
- Xamarin 0.8.5+ System.NotImplementedException: The method or operation is not implemented.   at System.Net.Sockets.SocketAsyncEventArgs.FinishOperationAsyncFailur [\#161](https://github.com/ably/ably-dotnet/issues/161)
- iOS issue with MsWebsocket transport [\#151](https://github.com/ably/ably-dotnet/issues/151)

**Merged pull requests:**

- Remove websockets4net and Use System.Net.WebSockets exclusively  [\#163](https://github.com/ably/ably-dotnet/pull/163) ([withakay](https://github.com/withakay))
- Test improvements [\#160](https://github.com/ably/ably-dotnet/pull/160) ([withakay](https://github.com/withakay))
- Added presence features covering RTP2 and others [\#157](https://github.com/ably/ably-dotnet/pull/157) ([withakay](https://github.com/withakay))
- Use a regex to check the PATCH portion of the version string, [\#155](https://github.com/ably/ably-dotnet/pull/155) ([withakay](https://github.com/withakay))

## [0.8.6-beta3](https://github.com/ably/ably-dotnet/tree/0.8.6-beta3)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/0.8.4.6...0.8.6-beta3)

**Implemented enhancements:**

- Populate message/presencemessage Id, ConnectionId, Timestamp [\#113](https://github.com/ably/ably-dotnet/issues/113)
- Fix HttpRequest & HttpRetry timeouts [\#95](https://github.com/ably/ably-dotnet/issues/95)
- Universal Windows Platform support [\#90](https://github.com/ably/ably-dotnet/issues/90)

**Fixed bugs:**

- Failure of history API call means channels no longer attach [\#116](https://github.com/ably/ably-dotnet/issues/116)
- Crash on android [\#109](https://github.com/ably/ably-dotnet/issues/109)
- When a Channel Fails to Resume After Disconnection, a Detached Event isn't Fired [\#108](https://github.com/ably/ably-dotnet/issues/108)
- Xamarin Android crash [\#96](https://github.com/ably/ably-dotnet/issues/96)
- Possible NewtonSoft incompatibility [\#91](https://github.com/ably/ably-dotnet/issues/91)
- authCallback does not support TokenRequest or token string [\#75](https://github.com/ably/ably-dotnet/issues/75)

**Closed issues:**

- Xamarin Null Reference Error - 0.8.6 - beta 2 [\#126](https://github.com/ably/ably-dotnet/issues/126)
- Concurrency issue in PresenceMap [\#124](https://github.com/ably/ably-dotnet/issues/124)
- Beta SDK depends on "Nito.AsyncEx" version="5.0.0-pre-02" [\#121](https://github.com/ably/ably-dotnet/issues/121)
- Failed to install Ably into a PCL project [\#120](https://github.com/ably/ably-dotnet/issues/120)
- Realtime chat example not working; issue with Nito.AsyncEX dep? [\#119](https://github.com/ably/ably-dotnet/issues/119)
- Request mac does not match [\#118](https://github.com/ably/ably-dotnet/issues/118)
- NullReference on iOS Xamarin [\#112](https://github.com/ably/ably-dotnet/issues/112)
- Token Authentication -  Unexpected error :Can not convert Object to String.; Code: 50000 [\#110](https://github.com/ably/ably-dotnet/issues/110)
- ConnectAsyncExtension.SocketConnectCompleted [\#105](https://github.com/ably/ably-dotnet/issues/105)
- ASP.NET Core support [\#93](https://github.com/ably/ably-dotnet/issues/93)
- Minor Crypto spec updates [\#38](https://github.com/ably/ably-dotnet/issues/38)

**Merged pull requests:**

- Update to allow building and packaging with Appveyor [\#154](https://github.com/ably/ably-dotnet/pull/154) ([withakay](https://github.com/withakay))
- Build script detects xunit runner [\#153](https://github.com/ably/ably-dotnet/pull/153) ([withakay](https://github.com/withakay))
- Update release process [\#152](https://github.com/ably/ably-dotnet/pull/152) ([withakay](https://github.com/withakay))
- Add Release Process [\#150](https://github.com/ably/ably-dotnet/pull/150) ([withakay](https://github.com/withakay))
- Use MSGPACK compiler flag to add/remove Protocol.MsgPack [\#148](https://github.com/ably/ably-dotnet/pull/148) ([withakay](https://github.com/withakay))
- Improve the XML Docs for AuthCallback [\#146](https://github.com/ably/ably-dotnet/pull/146) ([withakay](https://github.com/withakay))
- Make MsgPack optional and disabled by default [\#145](https://github.com/ably/ably-dotnet/pull/145) ([withakay](https://github.com/withakay))
- Test improvements & skip some tests. [\#144](https://github.com/ably/ably-dotnet/pull/144) ([withakay](https://github.com/withakay))
- simplified the message queue logic in ConnectionManager [\#142](https://github.com/ably/ably-dotnet/pull/142) ([withakay](https://github.com/withakay))
- Simplify now provider [\#140](https://github.com/ably/ably-dotnet/pull/140) ([withakay](https://github.com/withakay))
- Injectable logger [\#139](https://github.com/ably/ably-dotnet/pull/139) ([withakay](https://github.com/withakay))
- Injectable INowProvider [\#137](https://github.com/ably/ably-dotnet/pull/137) ([withakay](https://github.com/withakay))
- Updates to some test that rely on a delays [\#136](https://github.com/ably/ably-dotnet/pull/136) ([withakay](https://github.com/withakay))
- A fix to comply with RTL4b  [\#133](https://github.com/ably/ably-dotnet/pull/133) ([withakay](https://github.com/withakay))
- test AttachAwaitShouldtimeoutIfStateChanges would frequently fail [\#132](https://github.com/ably/ably-dotnet/pull/132) ([withakay](https://github.com/withakay))
- update the rest spec tests to use the InternalLogger [\#131](https://github.com/ably/ably-dotnet/pull/131) ([withakay](https://github.com/withakay))
- Removed obsolete assembly binding redirects from the Test project [\#130](https://github.com/ably/ably-dotnet/pull/130) ([withakay](https://github.com/withakay))
- Update README.md [\#128](https://github.com/ably/ably-dotnet/pull/128) ([withakay](https://github.com/withakay))
- Isolated logger for reliable testing [\#127](https://github.com/ably/ably-dotnet/pull/127) ([withakay](https://github.com/withakay))


## [0.8.5]

Upgraded Websocket transport with one that's compatible with netstandard1.3. 


## [0.8.4.2]

Bug fix release. Added extra logging. 

**Fixed bugs:**

- (partly) Xamarin Android crash [\#96](https://github.com/ably/ably-dotnet/issues/96)

## [0.8.4](https://github.com/ably/ably-dotnet/tree/0.8.4) (2016-06-27)
[Full Changelog](https://github.com/ably/ably-dotnet/compare/0.8.3...0.8.4)

**Breaking changes**

- Renamed `StatsDataRequestQuery` to `StatsRequestParams`
- Updated properties of `ConnectionDetailsMessage`, `ErrorInfo`, `Message`, `PresenceMessage` to use the standard pascal casing used throughout .Net

**Implemented enhancements:**

- Added syncronous methods for `AblyAuth`, `AblyRest` and `RestChannel`
- Added the ability to specify the `LogLevel` and `LogHandler` through the ably `ClientOptions`
- Added extra logging when sending and receiving Http requests
- Added a `None` Log level to completely switch off logging
- Improved realtime presence handling
- Include version info in requests [\#82](https://github.com/ably/ably-dotnet/issues/82)
- Sync REST methods [\#80](https://github.com/ably/ably-dotnet/issues/80)
- Interoperability tests [\#83](https://github.com/ably/ably-dotnet/issues/83)
- Add documentation to README about which platforms are supported [\#28](https://github.com/ably/ably-dotnet/issues/28)

**Fixed bugs:**

- Various inconsistencies following review of documentation [\#81](https://github.com/ably/ably-dotnet/issues/81)
- Possible minor issues [\#68](https://github.com/ably/ably-dotnet/issues/68)
- Fixed the MsgPack dependency issue

## [0.8.3](https://github.com/ably/ably-dotnet/tree/0.8.3) (2016-06-27)
[Full Changelog](https://github.com/ably/ably-dotnet/compare/v0.8.2-beta...0.8.3)

**Breaking changes**

- `ConnectionStateType` renamed to `ConnectionState`
- `PaginatedResult<>`. `NextQuery` renamed to `NextDataQuery`. `FirstQuery` -> `FirstDataQuery`

**Implemented enhancements:**

- `PaginatedResult<>` now supports `NextAsync` method which simplifies getting the results for the next page
- `Connection` and `RealtimeChannel` implement IEventEmitter interface and now match the ably [IDL](http://docs.ably.io/client-lib-development-guide/features/#idl)

**Fixed bugs:**

- NullReferenceException in ConnectionManager.cs [\#72](https://github.com/ably/ably-dotnet/issues/72) 

## [v0.8.2-beta](https://github.com/ably/ably-dotnet/tree/v0.8.2-beta) (2016-06-10)
[Full Changelog](https://github.com/ably/ably-dotnet/compare/v0.7.2...v0.8.2-beta)

**Implemented enhancements:**

- Add generic timeout for HTTP / Websocket requests [\#39](https://github.com/ably/ably-dotnet/issues/39)
- Add native async .NET support [\#26](https://github.com/ably/ably-dotnet/issues/26)
- Switch arity of auth methods [\#17](https://github.com/ably/ably-dotnet/issues/17)
- RealtimeClient [\#11](https://github.com/ably/ably-dotnet/issues/11)
- Realtime: Connection [\#10](https://github.com/ably/ably-dotnet/issues/10)
- Realtime: Channel [\#9](https://github.com/ably/ably-dotnet/issues/9)

**Fixed bugs:**

- Realtime constructor is blocking [\#41](https://github.com/ably/ably-dotnet/issues/41)
- Add generic timeout for HTTP / Websocket requests [\#39](https://github.com/ably/ably-dotnet/issues/39)
- Do not persist authorise attributes force & timestamp  [\#34](https://github.com/ably/ably-dotnet/issues/34)
- Class & namespace naming [\#29](https://github.com/ably/ably-dotnet/issues/29)
- Token Authentication not connecting [\#25](https://github.com/ably/ably-dotnet/issues/25)
- 0.8.x spec finalisation [\#20](https://github.com/ably/ably-dotnet/issues/20)
- Xamarin support [\#18](https://github.com/ably/ably-dotnet/issues/18)
- Switch arity of auth methods [\#17](https://github.com/ably/ably-dotnet/issues/17)

**Closed issues:**

- useBinaryProtocol=true memory leak? [\#66](https://github.com/ably/ably-dotnet/issues/66)
- Collection was modified exception [\#64](https://github.com/ably/ably-dotnet/issues/64)
- Attaching to a channel fails. [\#62](https://github.com/ably/ably-dotnet/issues/62)
- Custom AuthUrl gets called twice. [\#58](https://github.com/ably/ably-dotnet/issues/58)
- Presence timestamp init issue [\#56](https://github.com/ably/ably-dotnet/issues/56)
- UseBinaryProtocol causes presence data to become null [\#54](https://github.com/ably/ably-dotnet/issues/54)

**Merged pull requests:**

- Specs/realtime connection [\#57](https://github.com/ably/ably-dotnet/pull/57) ([marto83](https://github.com/marto83))
- Fix/issue 54 msgpack serialization [\#55](https://github.com/ably/ably-dotnet/pull/55) ([marto83](https://github.com/marto83))
- Specs/crypto [\#53](https://github.com/ably/ably-dotnet/pull/53) ([marto83](https://github.com/marto83))
- Specs/rest presence [\#52](https://github.com/ably/ably-dotnet/pull/52) ([marto83](https://github.com/marto83))
- Specs/rest channel [\#51](https://github.com/ably/ably-dotnet/pull/51) ([marto83](https://github.com/marto83))
- Specs/channels [\#50](https://github.com/ably/ably-dotnet/pull/50) ([marto83](https://github.com/marto83))
- Specs/auth [\#49](https://github.com/ably/ably-dotnet/pull/49) ([marto83](https://github.com/marto83))
- Specs/rest client [\#48](https://github.com/ably/ably-dotnet/pull/48) ([marto83](https://github.com/marto83))
- Validate rest idl [\#47](https://github.com/ably/ably-dotnet/pull/47) ([marto83](https://github.com/marto83))
- Fix tests and build [\#46](https://github.com/ably/ably-dotnet/pull/46) ([marto83](https://github.com/marto83))
- Standardise on 'initialized' for channel/connection state etc [\#42](https://github.com/ably/ably-dotnet/pull/42) ([SimonWoolf](https://github.com/SimonWoolf))
- Portable libs [\#35](https://github.com/ably/ably-dotnet/pull/35) ([Const-me](https://github.com/Const-me))
- RSC7a and RSC3 [\#31](https://github.com/ably/ably-dotnet/pull/31) ([Const-me](https://github.com/Const-me))
- Xamarin [\#30](https://github.com/ably/ably-dotnet/pull/30) ([yavor87](https://github.com/yavor87))

## [v0.7.2](https://github.com/ably/ably-dotnet/tree/v0.7.2) (2015-12-04)
[Full Changelog](https://github.com/ably/ably-dotnet/compare/v0.7.1...v0.7.2)

## [v0.7.1](https://github.com/ably/ably-dotnet/tree/v0.7.1) (2015-12-03)
**Implemented enhancements:**

- Add support for Nuget [\#14](https://github.com/ably/ably-dotnet/issues/14)
- Realtime: Channels [\#12](https://github.com/ably/ably-dotnet/issues/12)
- Spec validation [\#7](https://github.com/ably/ably-dotnet/issues/7)
- API changes Apr 2015 [\#4](https://github.com/ably/ably-dotnet/issues/4)

**Fixed bugs:**

- Token TTL  [\#22](https://github.com/ably/ably-dotnet/issues/22)
- TokenDetails serialisation [\#21](https://github.com/ably/ably-dotnet/issues/21)
- API changes Apr 2015 [\#4](https://github.com/ably/ably-dotnet/issues/4)

**Closed issues:**

- PaginatedResource API change  [\#3](https://github.com/ably/ably-dotnet/issues/3)

**Merged pull requests:**

- Fixed \#21. Forced TokenDetails dates to be serialized as milliseconds… [\#24](https://github.com/ably/ably-dotnet/pull/24) ([marto83](https://github.com/marto83))
- Realtime connection management redone [\#16](https://github.com/ably/ably-dotnet/pull/16) ([yavor87](https://github.com/yavor87))
- Token authentication for Realtime client [\#15](https://github.com/ably/ably-dotnet/pull/15) ([yavor87](https://github.com/yavor87))
- Realtime client [\#13](https://github.com/ably/ably-dotnet/pull/13) ([yavor87](https://github.com/yavor87))
- Realtime client initial [\#2](https://github.com/ably/ably-dotnet/pull/2) ([yavor87](https://github.com/yavor87))



\* *This Change Log was automatically generated by [github_changelog_generator](https://github.com/skywinder/Github-Changelog-Generator)*
