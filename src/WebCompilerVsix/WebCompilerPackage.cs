﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using WebCompiler;
using WebCompilerVsix.Commands;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace WebCompilerVsix
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidCompilerPackageString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class WebCompilerPackage : Package
    {
        public const string Version = "1.4.166";
        public static DTE2 _dte;
        public static Package Package;
        private SolutionEvents _events;

        protected override void Initialize()
        {
            _dte = GetService(typeof(DTE)) as DTE2;
            Package = this;

            Telemetry.SetDeviceName(_dte.Edition);
            Logger.Initialize(this, Constants.VSIX_NAME);

            Events2 events = _dte.Events as Events2;
            _events = events.SolutionEvents;
            _events.AfterClosing += () => { ErrorList.CleanAllErrors(); };
            _events.ProjectRemoved += (project) => { ErrorList.CleanAllErrors(); };

            CreateConfig.Initialize(this);
            Recompile.Initialize(this);
            CompileOnBuild.Initialize(this);
            RemoveConfig.Initialize(this);
            CompileAllFiles.Initialize(this);
            CleanOutputFiles.Initialize(this);

            base.Initialize();
        }

        public static bool IsDocumentDirty(string documentPath, out IVsPersistDocData persistDocData)
        {
            var serviceProvider = new ServiceProvider((IServiceProvider)_dte);

            IVsHierarchy vsHierarchy;
            uint itemId, docCookie;
            VsShellUtilities.GetRDTDocumentInfo(
                serviceProvider, documentPath, out vsHierarchy, out itemId, out persistDocData, out docCookie);
            if (persistDocData != null)
            {
                int isDirty;
                persistDocData.IsDocDataDirty(out isDirty);
                return isDirty == 1;
            }

            return false;
        }
    }

    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution)]
    public sealed class WebCompilerInitPackage : Package
    {
        public static Dispatcher _dispatcher;
        public static DTE2 _dte;

        protected override void Initialize()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _dte = GetService(typeof(DTE)) as DTE2;

            WebCompiler.CompilerService.Initializing += (s, e) => { StatusText("Installing updated versions of the web compilers..."); };
            WebCompiler.CompilerService.Initialized += (s, e) => { StatusText("Done installing the web compilers"); };

            // Delay execution until VS is idle.
            _dispatcher.BeginInvoke(new Action(() =>
            {
                // Then execute in a background thread.
                System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                {
                    try
                    {
                        WebCompiler.CompilerService.Initialize();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                });
            }), DispatcherPriority.ApplicationIdle, null);
        }

        public static void StatusText(string message)
        {
            WebCompilerInitPackage._dispatcher.BeginInvoke(new Action(() =>
            {
                _dte.StatusBar.Text = message;
            }), DispatcherPriority.ApplicationIdle, null);
        }
    }
}
