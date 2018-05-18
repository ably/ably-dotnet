# Change Log

## [0.8.11](https://github.com/ably/ably-dotnet/tree/)

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
