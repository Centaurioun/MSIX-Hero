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
using System.Linq;

namespace Otor.MsixHero.App.Mvvm.Changeable
{
    public class ChangeableContainer : NotifyPropertyChanged, IDisposable, IValidatedContainerChangeable, IDataErrorInfo
    {
        // ReSharper disable once InconsistentNaming
        protected bool displayValidationErrors = true;
        private readonly HashSet<IChangeable> children = new HashSet<IChangeable>();
        private readonly HashSet<IChangeable> touchedChildren = new HashSet<IChangeable>();
        private readonly HashSet<IValidatedChangeable> invalidChildren = new HashSet<IValidatedChangeable>();
        private readonly HashSet<IChangeable> dirtyChildren = new HashSet<IChangeable>();
        private bool isDirty;
        private bool isTouched;
        private bool suppressListening;
        private bool isValidated;
        private string validationMessage;

        public ChangeableContainer() : this(true)
        {
        }

        public ChangeableContainer(params IChangeable[] initialChildren) : this(true, initialChildren)
        {
        }

        public ChangeableContainer(bool isValidated, params IChangeable[] initialChildren)
        {
            this.isValidated = isValidated;
       
            foreach (var item in initialChildren)
            {
                if (!this.children.Add(item))
                {
                    continue;
                }

                item.Changed += this.OnSubItemChanged;

                if (item is IChangeableValue valueItem)
                {
                    valueItem.ValueChanged += this.OnSubItemValueChanged;
                }

                if (item is IValidatedChangeable changeableValue)
                {
                    changeableValue.ValidationStatusChanged += this.OnSubItemValidationStatusChanged;
                }

                item.IsDirtyChanged += this.OnSubItemDirtyChanged;
                item.IsTouchedChanged += this.OnSubItemTouchedChanged;

                if (item.IsTouched)
                {
                    this.touchedChildren.Add(item);
                }

                if (item.IsDirty)
                {
                    this.dirtyChildren.Add(item);
                }

                if (item is IValidatedChangeable validatedItem)
                {
                    this.isValidated = true;

                    if (!validatedItem.IsValid)
                    {
                        this.invalidChildren.Add(validatedItem);
                    }
                }
            }

            this.isDirty = this.dirtyChildren.Any();
            this.isTouched = this.touchedChildren.Any();

            if (this.isValidated)
            {
                var validationArgs = new ContainerValidationArgs(this.invalidChildren.Any() ? this.invalidChildren.First().ValidationMessage : null);
                this.CustomValidation?.Invoke(this, validationArgs);
                this.validationMessage = validationArgs.IsValid ? null : validationArgs.ValidationMessage;
                this.OnPropertyChanged(nameof(IsValid));
            }
        }

        public event EventHandler<EventArgs> Changed;

        public string Error => this.ValidationMessage;

        public string this[string columnName] => null;

        public virtual bool DisplayValidationErrors
        {
            get => this.displayValidationErrors;
            set
            {
                this.SetField(ref this.displayValidationErrors, value);

                foreach (var item in this.children.OfType<IValidatedChangeable>())
                {
                    item.DisplayValidationErrors = value;
                }
            }
        }

        public bool IsDirty
        {
            get => this.isDirty;
            private set
            {
                if (!this.SetField(ref this.isDirty, value))
                {
                    return;
                }
                
                this.IsDirtyChanged?.Invoke(this, new ValueChangedEventArgs<bool>(value));
            }
        }

        public bool IsTouched
        {
            get => this.isTouched;
            private set
            {
                if (!this.SetField(ref this.isTouched, value))
                {
                    return;
                }
                
                this.IsTouchedChanged?.Invoke(this, new ValueChangedEventArgs<bool>(value));
            }
        }

        public IReadOnlyCollection<IChangeable> TouchedChildren => this.touchedChildren;

        public IReadOnlyCollection<IChangeable> DirtyChildren => this.dirtyChildren;

        public IReadOnlyCollection<IValidatedChangeable> InvalidChildren => this.invalidChildren;
        
        public event EventHandler<ValueChangedEventArgs<bool>> IsDirtyChanged;

        public event EventHandler<ValueChangedEventArgs<bool>> IsTouchedChanged;

        public event EventHandler<ValueChangedEventArgs<string>> ValidationStatusChanged;

        public IReadOnlyCollection<IChangeable> GetChildren()
        {
            return this.children;
        }

        public virtual void Commit()
        {
            try
            {
                this.suppressListening = true;

                foreach (var item in this.TouchedChildren)
                {
                    item.Commit();
                }

                this.IsTouched = false;
                this.IsDirty = false;
                
                if (this.isValidated)
                {
                    var validationArgs = new ContainerValidationArgs(this.invalidChildren.Any() ? this.invalidChildren.First().ValidationMessage : null);
                    this.CustomValidation?.Invoke(this, validationArgs);
                    this.ValidationMessage = validationArgs.IsValid ? null : validationArgs.ValidationMessage;
                }
                else
                {
                    this.ValidationMessage = null;
                }
            }
            finally
            {
                this.suppressListening = false;
            }
        }

        public void Reset(ValueResetType resetType = ValueResetType.Hard)
        {
            try
            {
                this.suppressListening = true;

                var isAnyTouched = false;

                foreach (var item in this.TouchedChildren)
                {
                    item.Reset(resetType);
                    isAnyTouched |= item.IsTouched;
                }

                this.IsTouched = isAnyTouched;
                this.IsDirty = false;

                if (this.isValidated)
                {
                    var validationArgs = new ContainerValidationArgs(this.invalidChildren.Any() ? this.invalidChildren.First().ValidationMessage : null);
                    this.CustomValidation?.Invoke(this, validationArgs);
                    this.ValidationMessage = validationArgs.IsValid ? null : validationArgs.ValidationMessage;
                }
                else
                {
                    this.ValidationMessage = null;
                }
            }
            finally
            {
                this.suppressListening = false;
            }
        }

        public void Touch()
        {
            this.IsTouched = true;
        }

        public void AddChild(IChangeable item)
        {
            this.AddChildren(item);
        }

        public void RemoveChild(IChangeable child)
        {
            this.RemoveChildren(child);
        }

        public void RemoveChildren(params IChangeable[] childrenToRemove)
        {
            foreach (var item in childrenToRemove)
            {
                if (!this.children.Remove(item))
                {
                    continue;
                }

                item.Changed -= this.OnSubItemChanged;

                item.IsDirtyChanged -= this.OnSubItemDirtyChanged;
                item.IsTouchedChanged -= this.OnSubItemTouchedChanged;

                item.IsDirtyChanged -= this.OnSubItemDirtyChanged;
                item.IsTouchedChanged -= this.OnSubItemTouchedChanged;

                if (item is IValidatedChangeable changeableValue)
                {
                    changeableValue.ValidationStatusChanged -= this.OnSubItemValidationStatusChanged;
                }

                this.touchedChildren.Remove(item);
                this.dirtyChildren.Remove(item);

                // ReSharper disable once InvertIf
                if (item is IValidatedChangeable validatedItem)
                {
                    if (this.invalidChildren.Remove(validatedItem))
                    {
                        var otherInvalid = this.invalidChildren.FirstOrDefault();
                        if (otherInvalid == null)
                        {
                            this.ValidationMessage = null;
                        }
                        else
                        {
                            this.ValidationMessage = otherInvalid.ValidationMessage;
                        }
                    }
                }
            }

            this.IsDirty = this.dirtyChildren.Any();
            this.IsTouched = this.touchedChildren.Any();
        }

        public void AddChildren(params IChangeable[] childrenToAdd)
        {
            foreach (var item in childrenToAdd)
            {
                if (!this.children.Add(item))
                {
                    continue;
                }

                item.Changed += this.OnSubItemChanged;
                item.IsDirtyChanged -= this.OnSubItemDirtyChanged;
                item.IsTouchedChanged -= this.OnSubItemTouchedChanged;

                item.IsDirtyChanged += this.OnSubItemDirtyChanged;
                item.IsTouchedChanged += this.OnSubItemTouchedChanged;

                if (item is IValidatedChangeable changeableValue)
                {
                    changeableValue.ValidationStatusChanged += this.OnSubItemValidationStatusChanged;
                }

                if (item.IsTouched)
                {
                    this.touchedChildren.Add(item);
                }

                if (item.IsDirty)
                {
                    this.dirtyChildren.Add(item);
                }

                // ReSharper disable once InvertIf
                if (item is IValidatedChangeable validatedItem)
                {
                    validatedItem.DisplayValidationErrors = this.DisplayValidationErrors;
                    var wasValidated = this.isValidated;
                    this.isValidated = true;

                    if (!validatedItem.IsValid)
                    {
                        this.invalidChildren.Add(validatedItem);

                        if (this.ValidationMessage == null)
                        {
                            this.ValidationMessage = validatedItem.ValidationMessage;
                        }
                    }

                    if (wasValidated != this.isValidated)
                    {
                        this.OnPropertyChanged(nameof(IsValidated));
                    }
                }
            }

            this.IsDirty |= this.dirtyChildren.Any();
            this.IsTouched |= this.touchedChildren.Any();
        }

        public void Dispose()
        {
            foreach (var item in this.children)
            {
                item.IsDirtyChanged -= this.OnSubItemDirtyChanged;
                item.IsTouchedChanged -= this.OnSubItemTouchedChanged;

                if (item is IValidatedChangeable changeableValue)
                {
                    changeableValue.ValidationStatusChanged -= this.OnSubItemValidationStatusChanged;
                }
            }

            this.children.Clear();
            this.dirtyChildren.Clear();
            this.invalidChildren.Clear();
            this.touchedChildren.Clear();

            this.IsDirty = false;
            this.IsTouched = false;

            this.ValidationMessage = null;
            this.isValidated = false;
            this.OnPropertyChanged(nameof(isValidated));
        }

        public virtual string ValidationMessage
        {
            get => this.validationMessage;
            protected set
            {
                var validationArgs = new ContainerValidationArgs(value);
                this.CustomValidation?.Invoke(this, validationArgs);

                value = validationArgs.IsValid ? null : validationArgs.ValidationMessage;
                if (!this.SetField(ref this.validationMessage, value))
                {
                    return;
                }

                this.OnPropertyChanged(nameof(IsValid));
                var validationStatusChanged = this.ValidationStatusChanged;
                validationStatusChanged?.Invoke(this, new ValueChangedEventArgs<string>(value));

                this.OnPropertyChanged(nameof(Error));
            }
        }

        public bool IsValidated
        {
            get => this.isValidated;
            set
            {
                if (!this.SetField(ref this.isValidated, value))
                {
                    return;
                }

                this.invalidChildren.Clear();

                foreach (var item in this.children.OfType<IValidatedChangeable>())
                {
                    if (item.IsValidated == value)
                    {
                        continue;
                    }

                    item.IsValidated = value;
                }

                if (value)
                {
                    foreach (var item in this.children.OfType<IValidatedChangeable>().Where(item => !item.IsValid))
                    {
                        this.invalidChildren.Add(item);
                    }

                    this.ValidationMessage = this.invalidChildren.FirstOrDefault()?.ValidationMessage;
                }
                else
                {
                    this.ValidationMessage = null;
                }
            }
        }

        public bool IsValid => string.IsNullOrEmpty(this.ValidationMessage);

        public event EventHandler<ContainerValidationArgs> CustomValidation;

        private void OnSubItemValidationStatusChanged(object sender, ValueChangedEventArgs<string> e)
        {
            if (this.suppressListening)
            {
                return;
            }
            
            if (string.IsNullOrEmpty(e.NewValue))
            {
                this.invalidChildren.Remove((IValidatedChangeable)sender);
            }
            else
            {
                this.invalidChildren.Add((IValidatedChangeable)sender);
            }

            this.ValidationMessage = this.invalidChildren.FirstOrDefault()?.ValidationMessage;
        }

        private void OnSubItemValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (this.isValidated)
            {
                var validationArgs = new ContainerValidationArgs(this.invalidChildren.Any() ? this.invalidChildren.First().ValidationMessage : null);
                this.CustomValidation?.Invoke(this, validationArgs);
                this.ValidationMessage = validationArgs.IsValid ? null : validationArgs.ValidationMessage;
            }

            this.Changed?.Invoke(this, EventArgs.Empty);
        }

        private void OnSubItemChanged(object sender, EventArgs e)
        {
            if (this.isValidated)
            {
                var validationArgs = new ContainerValidationArgs(this.invalidChildren.Any() ? this.invalidChildren.First().ValidationMessage : null);
                this.CustomValidation?.Invoke(this, validationArgs);
                this.ValidationMessage = validationArgs.IsValid ? null : validationArgs.ValidationMessage;
            }

            this.Changed?.Invoke(this, EventArgs.Empty);
        }

        private void OnSubItemDirtyChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            if (this.suppressListening)
            {
                return;
            }

            if (e.NewValue)
            {
                this.IsDirty = true;
                this.dirtyChildren.Add((IChangeable)sender);
            }
            else
            {
                this.dirtyChildren.Remove((IChangeable)sender);
                this.IsDirty = this.dirtyChildren.Any(d => d.IsDirty);
            }
        }

        private void OnSubItemTouchedChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            if (this.suppressListening)
            {
                return;
            }

            if (e.NewValue)
            {
                this.IsTouched = true;
                this.touchedChildren.Add((IChangeable)sender);
            }
            else
            {
                this.touchedChildren.Remove((IChangeable)sender);
                this.IsTouched = this.touchedChildren.Any(d => d.IsTouched);
            }
        }
    }
}