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
using System.Collections.Generic;
using System.ComponentModel;

namespace Otor.MsixHero.App.Mvvm.Changeable
{
    public interface IChangeable : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets a value indicating if this instance is "dirty", which means that its current value is different from the original one.
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// Gets a value indicating if this instance is "touched", which means that it has been changed even though the current value may be the same as te original one.
        /// </summary>
        bool IsTouched { get; }

        /// <summary>
        /// Commits the current value to the original one, thus promoting it. It resets the "dirty" and "touched" flag.
        /// </summary>
        void Commit();

        /// <summary>
        /// Resets this instance.
        /// </summary>
        /// <param name="resetType">The type of reset determining whether to reset the "touched" flag (hard reset) or not ("soft reset").</param>
        void Reset(ValueResetType resetType = ValueResetType.Hard);

        /// <summary>
        /// Touches this instance. Even if the value is equal to the original one, the property will be marked as "touched".
        /// </summary>
        void Touch();

        event EventHandler<ValueChangedEventArgs<bool>> IsDirtyChanged;

        event EventHandler<ValueChangedEventArgs<bool>> IsTouchedChanged;

        event EventHandler<EventArgs> Changed;
    }

    public interface IChangeable<T> : IChangeableValue
    {
        /// <summary>
        /// Gets the original value.
        /// </summary>
        T OriginalValue { get; }

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        T CurrentValue { get; set; }
    }

    public interface IValidatedChangeable<T> : IChangeable<T>, IValidatedChangeable
    {
        IReadOnlyCollection<Func<T, string>> Validators { get; set; }
    }
}