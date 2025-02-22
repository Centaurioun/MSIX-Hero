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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Otor.MsixHero.App.Hero.Commands.Containers;
using Otor.MsixHero.App.Hero.Executor;
using Otor.MsixHero.App.Mvvm.Progress;
using Otor.MsixHero.Appx.Packaging.SharedPackageContainer;
using Otor.MsixHero.Appx.Packaging.SharedPackageContainer.Entities;

namespace Otor.MsixHero.App.Hero.Handlers;

public class GetSharedPackageContainersHandler : IRequestHandler<GetSharedPackageContainersCommand, IList<SharedPackageContainer>>
{
    private readonly IAppxSharedPackageContainerService _containerService;
    private readonly IMsixHeroCommandExecutor _commandExecutor;
    private readonly IBusyManager _busyManager;

    public GetSharedPackageContainersHandler(
        IMsixHeroCommandExecutor commandExecutor,
        IBusyManager busyManager,
        IAppxSharedPackageContainerService containerService)
    {
        this._containerService = containerService;
        this._commandExecutor = commandExecutor;
        this._busyManager = busyManager;
    }

    public async Task<IList<SharedPackageContainer>> Handle(GetSharedPackageContainersCommand request, CancellationToken cancellationToken)
    {
        var context = this._busyManager.Begin(OperationType.ContainerLoading);
        try
        {
            context.Progress = 10;
            context.Message = Resources.Localization.Containers_PleaseWait;

            var selected = this._commandExecutor.ApplicationState.Containers.SelectedContainer?.Name;

            var all = await this._containerService.GetAll(cancellationToken).ConfigureAwait(false);
            this._commandExecutor.ApplicationState.Containers.AllContainers.Clear();
            this._commandExecutor.ApplicationState.Containers.AllContainers.AddRange(all);
            context.Progress = 100;

            if (selected == null)
            {
                return all;
            }

            var newSelected = all.FirstOrDefault(a => a.Name == selected);
            await this._commandExecutor.Invoke(this, new SelectSharedPackageContainerCommand(newSelected), cancellationToken).ConfigureAwait(false);

            return all;
        }
        finally
        {
            this._busyManager.End(context);
        }
    }
}