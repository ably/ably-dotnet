﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Tests.Infrastructure;
using Xunit;

namespace IO.Ably.Tests
{
    public class TaskUtilsTests
    {
        class MockBagroundService
        {
            public static async Task FailingTask()
            {
                await Task.Delay(100);
                throw new Exception();
            }
        }

        [Fact(Skip="Even this test crashes the test process on Circle CI")]
        public async Task FailingTaskShouldHaveExceptionHandled()
        {
            var tsc = new TaskCompletionAwaiter(500);
            TaskUtils.RunInBackground(MockBagroundService.FailingTask(), exception =>
            {
                tsc.SetCompleted();
            });

            var result = await tsc.Task;
            result.Should().BeTrue();
        }

        [Fact(Skip = "Маnual test only. Otherwise it crashes the test host process on the CI server")]
        public async Task FailingTaskWrappingAsyncWillNotHaveExceptionHandled()
        {
            /*
             *  This test demonstrates that wrapping a method that throws an Exception will cause the handler
             *  to not be assigned at the continuation for the inner task and as such the Exception will not be handled.
             *  DO NOT USE IN PRODUCTION CODE.
             */
            var tsc = new TaskCompletionAwaiter(500);
            TaskUtils.RunInBackground(async () => await MockBagroundService.FailingTask(), exception =>
            {
                tsc.SetCompleted();
            });

            var result = await tsc.Task;
            result.Should().BeFalse();
        }
    }
}