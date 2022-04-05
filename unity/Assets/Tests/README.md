# Unity Tests

## Notes on the Edit and Play mode tests

There is intentional duplication of code between the `EditMode` and `PlayMode` test suites.  This duplication will be removed once version 2.0.1 of the Unity Test Framwork is available in a LTS (Long Term Support) release, see [Combine Edit Mode and Play Mode tests](https://docs.unity3d.com/Packages/com.unity.test-framework@2.0/manual/whats-new.html#combine-edit-mode-and-play-mode-tests)

The test implementations add a dependency on the [Cysharp UniTask](https://github.com/Cysharp/UniTask) machinery. This is due to lack of `async` in the supported versions of Unity. This dependency can also be removed once version 2.0.1 of the Unity Test Framework is available in a LTS release, see [Async tests](https://docs.unity3d.com/Packages/com.unity.test-framework@2.0/manual/whats-new.html#async-tests)

