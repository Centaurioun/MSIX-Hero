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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Toolkit.Uwp.Notifications;
using Ookii.Dialogs.Wpf;
using Otor.MsixHero.App.Helpers.Dialogs;
using Otor.MsixHero.App.Helpers.Interop;
using Otor.MsixHero.Infrastructure.Services;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace Otor.MsixHero.App.Services
{
    public class InteractionService : IInteractionService
    {
        private readonly SynchronizationContext _context;

        public InteractionService()
        {
            this._context = SynchronizationContext.Current;
        }

        public InteractionResult Confirm(string body, string title = null, InteractionType type = InteractionType.Asterisk, InteractionButton buttons = InteractionButton.OK)
        {
            var targetType = (MessageBoxImage)(int)type;
            var targetButtons = (MessageBoxButton)(int)buttons;

            // ReSharper disable once AssignNullToNotNullAttribute
            var result = MessageBox.Show(GetActiveWindow(), body, title ?? "MSIX Hero", targetButtons, targetType);
            return (InteractionResult)(int)result;
        }

        public Task ShowToast(string title, string message)
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show();

            return Task.CompletedTask;
        }

        public int ShowMessage(string body, IReadOnlyCollection<string> buttons, string title = null, string extendedInfo = null, InteractionResult systemButtons = InteractionResult.None)
        {
            var taskDialog = new TaskDialog
            {
                MainIcon = TaskDialogIcon.Information,
                ButtonStyle = TaskDialogButtonStyle.CommandLinks
            };

            foreach (var item in buttons)
            {
                var btn = new TaskDialogButton(ButtonType.Custom)
                {
                    Text = item
                };
                
                taskDialog.Buttons.Add(btn);
            }

            if (systemButtons != InteractionResult.None)
            {
                var bts = new[] { InteractionResult.Yes, InteractionResult.No, InteractionResult.OK, InteractionResult.Cancel, InteractionResult.Close };
                foreach (var bt in bts)
                {
                    if ((systemButtons & bt) == bt)
                    {
                        taskDialog.Buttons.Add(new TaskDialogButton((ButtonType)(int)bt));
                    }
                }
            }

            taskDialog.CenterParent = true;
            taskDialog.Content = body;

            if (!string.IsNullOrEmpty(extendedInfo))
            {
                taskDialog.ExpandedInformation = extendedInfo;
            }

            taskDialog.WindowTitle = title ?? "MSIX Hero";

            int clickedIndex = -1;
            // ReSharper disable once ConvertToLocalFunction
            EventHandler<TaskDialogItemClickedEventArgs> handler = (_, args) =>
            {
                var taskDialogButton = args.Item as TaskDialogButton;
                if (taskDialogButton == null)
                {
                    return;
                }

                clickedIndex = taskDialog.Buttons.IndexOf(taskDialogButton);
            };

            try
            {
                taskDialog.ButtonClicked += handler;

                if (this._context == null)
                {
                    taskDialog.ShowDialog(GetActiveWindow());
                }

                var dispatcher = Application.Current.Dispatcher;
                if (dispatcher != null)
                {
                    dispatcher.Invoke(() =>
                    {
                        this._context.Send(
                            _ => taskDialog.ShowDialog(GetActiveWindow()),
                            null);
                    },
                        DispatcherPriority.SystemIdle);
                }
            }
            finally
            {
                taskDialog.ButtonClicked -= handler;
            }

            return clickedIndex;
        }

        public InteractionResult ShowError(string body, Exception exception, InteractionResult buttons = InteractionResult.Close)
        {
#if DEBUG
            var extended = exception.ToString();
#else
            string extended = body != exception.Message ? exception.Message : null;
            if (exception.InnerException != null)
            {
                if (!string.IsNullOrEmpty(extended))
                {
                    extended += System.Environment.NewLine;
                    extended += exception.GetBaseException().Message;
                }
            }
#endif

            return this.ShowError(body, buttons, extendedInfo: extended);
        }

        public InteractionResult ShowError(string body, InteractionResult buttons = InteractionResult.Close, string title = null, string extendedInfo = null)
        {
            var taskDialog = new TaskDialog
            {
                MainIcon = TaskDialogIcon.Error,
                ButtonStyle = TaskDialogButtonStyle.Standard
            };

            if (buttons.HasFlag(InteractionResult.Retry))
            {
                taskDialog.Buttons.Add(new TaskDialogButton(ButtonType.Retry));
            }

            if (buttons.HasFlag(InteractionResult.OK))
            {
                taskDialog.Buttons.Add(new TaskDialogButton(ButtonType.Ok));
            }

            if (buttons.HasFlag(InteractionResult.Yes))
            {
                taskDialog.Buttons.Add(new TaskDialogButton(ButtonType.Yes));
            }

            if (buttons.HasFlag(InteractionResult.No))
            {
                taskDialog.Buttons.Add(new TaskDialogButton(ButtonType.No));
            }

            if (buttons.HasFlag(InteractionResult.Cancel))
            {
                taskDialog.Buttons.Add(new TaskDialogButton(ButtonType.Cancel));
            }

            if (buttons.HasFlag(InteractionResult.Close))
            {
                taskDialog.Buttons.Add(new TaskDialogButton(ButtonType.Close));
            }

            taskDialog.CenterParent = true;
            taskDialog.Content = body;

            if (!string.IsNullOrEmpty(extendedInfo))
            {
                taskDialog.ExpandedInformation = extendedInfo;
            }

            taskDialog.WindowTitle = title ?? "MSIX Hero - " + Resources.Localization.Dialogs_Error_Title;

            if (this._context == null)
            {
                return (InteractionResult)(int)taskDialog.ShowDialog(GetActiveWindow()).ButtonType;
            }

            var result = 0;

            var dispatcher = Application.Current.Dispatcher;
            if (dispatcher != null)
            {
                dispatcher.Invoke(() =>
                {
                    this._context.Send(
                        _ => result = (int)taskDialog.ShowDialog(GetActiveWindow()).ButtonType,
                        null);
                },
                DispatcherPriority.SystemIdle);
            }

            return (InteractionResult)result;
        }

        public InteractionResult ShowInfo(string body, InteractionResult buttons = InteractionResult.Close, string title = null, string extendedInfo = null)
        {
            var taskDialog = new TaskDialog
            {
                MainIcon = TaskDialogIcon.Information,
                ButtonStyle = TaskDialogButtonStyle.Standard
            };

            if (buttons.HasFlag(InteractionResult.Retry))
            {
                taskDialog.Buttons.Add(new TaskDialogButton(ButtonType.Retry));
            }

            if (buttons.HasFlag(InteractionResult.OK))
            {
                taskDialog.Buttons.Add(new TaskDialogButton(ButtonType.Ok));
            }

            if (buttons.HasFlag(InteractionResult.Yes))
            {
                taskDialog.Buttons.Add(new TaskDialogButton(ButtonType.Yes));
            }

            if (buttons.HasFlag(InteractionResult.No))
            {
                taskDialog.Buttons.Add(new TaskDialogButton(ButtonType.No));
            }

            if (buttons.HasFlag(InteractionResult.Cancel))
            {
                taskDialog.Buttons.Add(new TaskDialogButton(ButtonType.Cancel));
            }

            if (buttons.HasFlag(InteractionResult.Close))
            {
                taskDialog.Buttons.Add(new TaskDialogButton(ButtonType.Close));
            }

            taskDialog.CenterParent = true;
            taskDialog.Content = body;

            if (!string.IsNullOrEmpty(extendedInfo))
            {
                taskDialog.ExpandedInformation = extendedInfo;
            }

            taskDialog.WindowTitle = title ?? "MSIX Hero";

            if (this._context == null)
            {
                return (InteractionResult)(int)taskDialog.ShowDialog(GetActiveWindow()).ButtonType;
            }

            var result = 0;

            var dispatcher = Application.Current.Dispatcher;
            if (dispatcher != null)
            {
                dispatcher.Invoke(() =>
                {
                    this._context.Send(
                        _ => result = (int)taskDialog.ShowDialog(GetActiveWindow()).ButtonType,
                        null);
                },
                DispatcherPriority.SystemIdle);
            }

            return (InteractionResult)result;
        }

        public bool SelectFile(out string selectedFile)
        {
            return this.SelectFile(new FileDialogSettings(), out selectedFile);
        }

        public bool SelectFile(FileDialogSettings settings, out string selectedFile)
        {
            if (!SelectFile(settings, false, out var selection))
            {
                selectedFile = null;
                return false;
            }

            selectedFile = selection.FirstOrDefault();
            return true;
        }

        public bool SaveFile(FileDialogSettings settings, out string selectedFile)
        {
            var dlg = new SaveFileDialog
            {
                Filter = PrepareFilterString(settings.Filter)
            };

            if (settings.InitialSelection != null)
            {
                var fileInfo = new FileInfo(settings.InitialSelection);
                if (fileInfo.Directory?.Exists == true)
                {
                    dlg.InitialDirectory = fileInfo.Directory.FullName;
                }

                dlg.FileName = fileInfo.Name;
            }

            dlg.CheckFileExists = false;
            var result = dlg.ShowDialog(GetActiveWindow()) == true;
            selectedFile = dlg.FileName;
            return result;
        }

        public bool SaveFile(out string selectedFile)
        {
            return this.SaveFile(new FileDialogSettings(), out selectedFile);
        }
                
        public bool SelectFiles(FileDialogSettings settings, out string[] selectedFiles)
        {
            return SelectFile(settings, true, out selectedFiles);
        }
        
        public bool SelectFiles(out string[] selectedFiles)
        {
            return SelectFile(new FileDialogSettings(), true, out selectedFiles);
        }

        public bool SelectFolder(string initialFolder, out string selectedFolder)
        {
            var dlg = new VistaFolderBrowserDialog
            {
                SelectedPath = initialFolder
            };

            var result = dlg.ShowDialog(GetActiveWindow()) == true;
            selectedFolder = dlg.SelectedPath;
            return result;
        }

        public bool SelectFolder(out string selectedFolder)
        {
            return this.SelectFolder(null, out selectedFolder);
        }

        private static bool SelectFile(FileDialogSettings fileDialogSettings, bool withMultiSelection, out string[] selectedFiles)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = PrepareFilterString(fileDialogSettings.Filter)
            };

            if (fileDialogSettings.InitialSelection != null)
            {
                var fileInfo = new FileInfo(fileDialogSettings.InitialSelection);
                if (fileInfo.Directory?.Exists == true)
                {
                    openFileDialog.InitialDirectory = fileInfo.Directory.FullName;
                }

                openFileDialog.FileName = fileInfo.FullName;
            }

            if (!string.IsNullOrEmpty(fileDialogSettings.DialogTitle))
            {
                openFileDialog.Title = fileDialogSettings.DialogTitle;
            }    
            
            openFileDialog.CheckFileExists = true;
            openFileDialog.Multiselect = withMultiSelection;

            var result = openFileDialog.ShowDialog(GetActiveWindow()) == true;
            selectedFiles = openFileDialog.FileNames;
            return result;
        }

        private static string PrepareFilterString(string filterString)
        {
            if (string.IsNullOrEmpty(filterString))
            {
                return new DialogFilterBuilder().WithAll();
            }

            return filterString;
        }
        
        private static Window GetActiveWindow()
        {
            var ptrActiveWindow = User32.GetActiveWindow();
            var activeWindow = Application.Current.Windows.OfType<Window>().SingleOrDefault(window => new WindowInteropHelper(window).Handle == ptrActiveWindow) ?? Application.Current.MainWindow;
            return activeWindow;
        }
    }
}