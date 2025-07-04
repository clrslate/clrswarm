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

namespace ClrSlate.Swarm.Services.StdioCommandHandlers;

public class NpxCommandHandler : IStdioCommandHandler
{
    public bool CanHandle(string commandName) => string.Equals(commandName, "npx", StringComparison.OrdinalIgnoreCase);

    public async Task HandleAsync(string[]? args)
    {
        //if (args is not { Length: > 0 }) return;
        //var packageName = args.FirstOrDefault(arg => !string.IsNullOrWhiteSpace(arg) && !arg.StartsWith("-"));
        //if (string.IsNullOrEmpty(packageName)) return;
        //var upgradeResult = await Cli.Wrap("npm")
        //    .WithArguments(new[] { "update", "-g", packageName })
        //    .WithValidation(CommandResultValidation.None)
        //    .ExecuteBufferedAsync();
        //if (upgradeResult.ExitCode != 0)
        //{
        //    var installResult = await Cli.Wrap("npm")
        //        .WithArguments(new[] { "install", "-g", packageName })
        //        .WithValidation(CommandResultValidation.None)
        //        .ExecuteBufferedAsync();
        //    if (installResult.ExitCode != 0)
        //    {
        //        throw new InvalidOperationException($"Failed to install or upgrade npm package '{packageName}':\n{installResult.StandardError}");
        //    }
        //}
    }
}
