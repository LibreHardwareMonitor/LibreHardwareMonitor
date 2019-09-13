// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;
using OpenHardwareMonitor.TaskScheduler;

namespace OpenHardwareMonitor.GUI
{
    public class StartupManager
    {
        private readonly TaskSchedulerClass _scheduler;
        private bool _startup;
        private const string REGISTRY_RUN = @"Software\Microsoft\Windows\CurrentVersion\Run";

        private bool IsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        public StartupManager()
        {
            int p = (int)Environment.OSVersion.Platform;
            if ((p == 4) || (p == 128))
            {
                _scheduler = null;
                IsAvailable = false;
                return;
            }

            if (IsAdministrator())
            {
                try
                {
                    _scheduler = new TaskSchedulerClass();
                    _scheduler.Connect(null, null, null, null);
                }
                catch
                {
                    _scheduler = null;
                }

                if (_scheduler != null)
                {
                    try
                    {
                        // check if the taskscheduler is running
                        IRunningTaskCollection _ = _scheduler.GetRunningTasks(0);
                        ITaskFolder folder = _scheduler.GetFolder("\\Open Hardware Monitor");
                        IRegisteredTask task = folder.GetTask("Startup");
                        _startup = (task != null) && (task.Definition.Triggers.Count > 0) &&
                            (task.Definition.Triggers[1].Type == TASK_TRIGGER_TYPE2.TASK_TRIGGER_LOGON) &&
                            (task.Definition.Actions.Count > 0) &&
                            (task.Definition.Actions[1].Type == TASK_ACTION_TYPE.TASK_ACTION_EXEC) &&
                            (task.Definition.Actions[1] is IExecAction) && (((IExecAction) task.Definition.Actions[1]).Path == Application.ExecutablePath);
                    }
                    catch (IOException)
                    {
                        _startup = false;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        _scheduler = null;
                    }
                    catch (COMException)
                    {
                        _scheduler = null;
                    }
                }
            }
            else
            {
                _scheduler = null;
            }

            if (_scheduler == null)
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_RUN))
                    {
                        _startup = false;
                        string value = (string) key?.GetValue("OpenHardwareMonitor");

                        if (value != null)
                            _startup = value == Application.ExecutablePath;
                    }
                    IsAvailable = true;
                }
                catch (SecurityException)
                {
                    IsAvailable = false;
                }
            }
            else
            {
                IsAvailable = true;
            }
        }

        private void CreateSchedulerTask()
        {
            ITaskDefinition definition = _scheduler.NewTask(0);
            definition.RegistrationInfo.Description = "This task starts the Open Hardware Monitor on Windows startup.";
            definition.Principal.RunLevel = TASK_RUNLEVEL.TASK_RUNLEVEL_HIGHEST;
            definition.Settings.DisallowStartIfOnBatteries = false;
            definition.Settings.StopIfGoingOnBatteries = false;
            definition.Settings.ExecutionTimeLimit = "PT0S";
            IExecAction action = (IExecAction)definition.Actions.Create(TASK_ACTION_TYPE.TASK_ACTION_EXEC);
            action.Path = Application.ExecutablePath;
            action.WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath);

            ITaskFolder root = _scheduler.GetFolder("\\");
            ITaskFolder folder;
            try
            {
                folder = root.GetFolder("Open Hardware Monitor");
            }
            catch (IOException)
            {
                folder = root.CreateFolder("Open Hardware Monitor", "");
            }
            folder.RegisterTaskDefinition("Startup", definition, (int)TASK_CREATION.TASK_CREATE_OR_UPDATE, null, null, TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN, "");
        }

        private void DeleteSchedulerTask()
        {
            ITaskFolder root = _scheduler.GetFolder("\\");
            try
            {
                ITaskFolder folder = root.GetFolder("Open Hardware Monitor");
                folder.DeleteTask("Startup", 0);
            }
            catch (IOException) { }
            try
            {
                root.DeleteFolder("Open Hardware Monitor", 0);
            }
            catch (IOException) { }
        }

        private void CreateRegistryRun()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(REGISTRY_RUN);
            key.SetValue("OpenHardwareMonitor", Application.ExecutablePath);
        }

        private void DeleteRegistryRun()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(REGISTRY_RUN);
            key.DeleteValue("OpenHardwareMonitor");
        }

        public bool IsAvailable { get; }

        public bool Startup
        {
            get
            {
                return _startup;
            }
            set
            {
                if (_startup != value)
                {
                    if (IsAvailable)
                    {
                        if (_scheduler != null)
                        {
                            if (value)
                                CreateSchedulerTask();
                            else
                                DeleteSchedulerTask();
                            _startup = value;
                        }
                        else
                        {
                            try
                            {
                                if (value)
                                    CreateRegistryRun();
                                else
                                    DeleteRegistryRun();
                                _startup = value;
                            }
                            catch (UnauthorizedAccessException)
                            {
                                throw new InvalidOperationException();
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
        }
    }
}
