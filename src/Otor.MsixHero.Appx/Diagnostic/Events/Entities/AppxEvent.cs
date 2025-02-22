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

namespace Otor.MsixHero.Appx.Diagnostic.Events.Entities
{
    public enum AppxEventType : byte
    {
        // https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventlevel?view=net-6.0
        None = 0,
        Critical = 1,
        Error = 2,
        Warning = 3,
        Information = 4,
        Verbose = 5,
    }

    public class AppxEvent
    {
        public DateTime DateTime { get; set; }

        public string Message { get; set; }
        
        public Guid? ActivityId { get; set; }

        public string PackageName { get; set; }

        public string FilePath { get; set; }

        public string User { get; set; }

        public AppxEventType Type { get; set; }

        public int ThreadId { get; set; }

        public string OpcodeDisplayName { get; set; }

        public string ErrorCode { get; set; }
        
        public string Source { get; set; }
    }
}