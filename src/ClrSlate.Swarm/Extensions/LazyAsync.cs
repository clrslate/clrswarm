/*
 * Copyright 2025 ClrSlate Tech labs Private Limited
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

﻿namespace ClrSlate.Swarm.Extensions;

public class LazyAsync<TValue>(Func<Task<TValue>> valueFactory)
{
    private readonly Lazy<Task<TValue>> _lazy = new(valueFactory);

    public Task<TValue> ValueAsync => _lazy.Value;

    public TValue Value {
        get {
            try {
                // Get the ValueAsync task and wait for it synchronously
                var task = ValueAsync;

                // For already completed tasks, this is very efficient
                if (task.IsCompletedSuccessfully) {
                    return task.Result;
                }

                // For tasks that aren't complete, block and wait
                // This will propagate any exceptions that occurred during the task
                return task.GetAwaiter().GetResult();
            }
            catch (AggregateException ae) {
                // Unwrap AggregateException to preserve the original exception
                if (ae.InnerExceptions.Count == 1) {
                    throw ae.InnerException!;
                }
                throw;
            }
        }
    }

    public bool IsValueCreated => _lazy.IsValueCreated;
}
