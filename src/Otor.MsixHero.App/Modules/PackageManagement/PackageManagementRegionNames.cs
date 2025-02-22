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

namespace Otor.MsixHero.App.Modules.PackageManagement
{
    public static class PackageManagementRegionNames
    {
        private const string Base = "msix-hero-packages-";

        public const string Master = Base + "list";

        public const string Details = Base + "details";

        public const string PackageExpert = Base + "package-expert";
        
        public const string PopupFilter = Base + "popup-filter";

        public const string PackageExpertContent = Base + "package-expert-content";
    }
}
