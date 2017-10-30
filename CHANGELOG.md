# Change Log

## [0.8.6-beta3](https://github.com/ably/ably-dotnet/tree/0.8.6-beta3)

[Full Changelog](https://github.com/ably/ably-dotnet/compare/0.8.4.6...HEAD)

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
