using System;
using System.IO;

namespace LegacyBounce.Framework {
    public class StagedDeployTarget : Task
    {
        public string Name { get; private set; }

        public const string RemoteDeployStage = "remoteDeploy";
        public const string PackageStage = "package";
        public const string InvokeRemoteDeployStage = "invokeRemoteDeploy";
        public const string PackageInvokeRemoteDeployStage = "packageInvokeRemoteDeploy";
        public const string PackageDeployStage = "packageDeploy";
        public const string DeployStage = "deploy";

        public Parameter<string> Stage { get; private set; }

        [Dependency]
        private readonly Switch<string> Switch;

        public StagedDeployTarget(string name, Parameter<string> stage)
        {
            Name = name;
            Stage = stage;
            Switch = new Switch<string>(stage);
            PrepareDeploy = package => package;
            InvokeRemoteDeploy = package => package;
            Deploy = package => package;
        }

        private void SetupSwitch() {
            if (Package != null && Deploy != null) {
                var packageWithBounce = CopyBounceDirectoryIntoPackage(Package);

                Switch[PackageStage] = packageWithBounce;
                Switch[InvokeRemoteDeployStage] = GetInvokeRemoteDeploy(".");
                Switch[RemoteDeployStage] = Deploy(".");
                Switch[PackageInvokeRemoteDeployStage] = GetInvokeRemoteDeploy(packageWithBounce);
                Switch[PackageDeployStage] = Deploy(GetPreparedDeployPackage(Package));
                Switch[DeployStage] = Deploy(GetPreparedDeployPackage("."));
            }
        }

        private Task<string> GetPreparedDeployPackage(Task<string> package) {
            return package.WithDependencyOn(PrepareDeploy(package));
        }

        private IObsoleteTask GetInvokeRemoteDeploy(Task<string> package)
        {
            Task<string> packageAfterDeploy = GetPreparedDeployPackage(package);

            if (Deploy != null) {
                return InvokeRemoteDeploy(packageAfterDeploy);
            } else {
                return packageAfterDeploy;
            }
        }

        private Func<Task<string>, IObsoleteTask> _deploy;
        public Func<Task<string>, IObsoleteTask> Deploy {
            get { return _deploy; }
            set {
                _deploy = value;
                SetupSwitch();
            }
        }

        private Func<Task<string>, IObsoleteTask> _invokeRemoteDeploy;
        public Func<Task<string>, IObsoleteTask> InvokeRemoteDeploy {
            get { return _invokeRemoteDeploy; }
            set {
                _invokeRemoteDeploy = value;
                SetupSwitch();
            }
        }

        private Task<string> _package;
        public Task<string> Package {
            get { return _package; }
            set {
                _package = value;
                SetupSwitch();
            }
        }

        private Func<Task<string>, IObsoleteTask> _prepareDeploy;

        public Func<Task<string>, IObsoleteTask> PrepareDeploy {
            get { return _prepareDeploy; }
            set {
                _prepareDeploy = value;
                SetupSwitch();
            }
        }

        private Task<string> CopyBounceDirectoryIntoPackage(Task<string> package)
        {
            Task<string> copiedBounceDirectory = new Copy
            {
                FromPath = Path.GetDirectoryName(ObsoleteBounceRunner.TargetsPath),
                ToPath = package.SubPath("Bounce"),
                Excludes = Copy.SvnDirectories,
            }.ToPath;

            var deletedBeforeBounce = new Delete {Path = copiedBounceDirectory.SubPath("beforebounce.*")};

            return copiedBounceDirectory.SubPath("..").WithDependencyOn(deletedBeforeBounce);
        }

        public override bool IsLogged
        {
            get
            {
                return false;
            }
        }
    }
}