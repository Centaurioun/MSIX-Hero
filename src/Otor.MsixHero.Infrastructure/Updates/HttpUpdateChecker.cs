﻿// MSIX Hero
// Copyright (C) 2024 Marcin Otorowski
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// Full notice:
// https://github.com/marcinotorowski/msix-hero/blob/develop/LICENSE.md

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Otor.MsixHero.Infrastructure.Updates
{
    public class HttpUpdateChecker : IUpdateChecker
    {
        public async Task<UpdateCheckResult> CheckForNewVersion(Version currentVersion)
        {
            var lastDefinition = await this.GetUpdateDefinition().ConfigureAwait(false);

            if (!Version.TryParse(lastDefinition.LastVersion ?? string.Empty, out var lastVersion))
            {
                throw new FormatException($"Version {lastDefinition.LastVersion} could not be parsed as a version string.");
            }

            return new UpdateCheckResult(currentVersion, lastVersion, lastDefinition.Released)
            {
                BlogUrl = lastDefinition.BlogUrl,
                Changes = lastDefinition.Changes
            };
        }

        public Task<UpdateCheckResult> CheckForNewVersion()
        {
            var assemblyVersion = FileVersionInfo.GetVersionInfo((Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).Location).ProductVersion;
            if (!Version.TryParse(assemblyVersion, out var version))
            {
                return Task.FromException<UpdateCheckResult>(new FormatException(string.Format(Resources.Localization.Infrastructure_WrongVersion_Format, version)));
            }

            return this.CheckForNewVersion(version);
        }

        private async Task<UpdateDefinition> GetUpdateDefinition()
        {
#pragma warning disable SYSLIB0014
            var webRequest = WebRequest.CreateHttp("https://msixhero.net/update.json");
#pragma warning restore SYSLIB0014
            using var webResponse = await webRequest.GetResponseAsync().ConfigureAwait(false);
            await using var stream = webResponse.GetResponseStream();
            if (stream == null)
            {
                throw new InvalidOperationException("Could not get information about the update.");
            }

            using var stringReader = new StreamReader(stream);
            var json = await stringReader.ReadToEndAsync().ConfigureAwait(false);
            var deserialized = JsonConvert.DeserializeObject<UpdateDefinition>(json);
            return deserialized;
        }
    }
}
