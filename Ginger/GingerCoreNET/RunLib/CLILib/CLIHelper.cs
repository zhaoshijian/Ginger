#region License
/*
Copyright © 2014-2019 European Support Limited

Licensed under the Apache License, Version 2.0 (the "License")
you may not use this file except in compliance with the License.
You may obtain a copy of the License at 

http://www.apache.org/licenses/LICENSE-2.0 

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS, 
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
See the License for the specific language governing permissions and 
limitations under the License. 
*/
#endregion

using amdocs.ginger.GingerCoreNET;
using Amdocs.Ginger.Common;
using Ginger.AnalyzerLib;
using Ginger.Run;
using Ginger.SourceControl;
using GingerCore;
using GingerCore.Environments;
using GingerCoreNET.SourceControl;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Amdocs.Ginger.CoreNET.RunLib.CLILib
{
    public enum eCLIType
    {
        Config,Dynamic,Script,Arguments
    }

    public class CLIHelper : INotifyPropertyChanged
    {
        public string Solution;
        public string Env;
        public string Runset;
        public string SourceControlURL;
        public string SourcecontrolUser;
        public string sourceControlPass;
        public eAppReporterLoggingLevel AppLoggingLevel;

        bool mShowAutoRunWindow; // default is false except in ConfigFile which is true to keep backward compatibility        
        public bool ShowAutoRunWindow
        {
            get
            {
                return mShowAutoRunWindow;
            }
            set
            {
                mShowAutoRunWindow = value;
                OnPropertyChanged(nameof(ShowAutoRunWindow));
            }
        }

        bool mDownloadUpgradeSolutionFromSourceControl;
        public bool DownloadUpgradeSolutionFromSourceControl
        {
            get
            {
                return mDownloadUpgradeSolutionFromSourceControl;
            }
            set
            {
                mDownloadUpgradeSolutionFromSourceControl = value;
                OnPropertyChanged(nameof(DownloadUpgradeSolutionFromSourceControl));
            }

        }

        bool mRunAnalyzer;
        public bool RunAnalyzer
        {
            get
            {
                return mRunAnalyzer;
            }
            set
            {
                mRunAnalyzer = value;
                OnPropertyChanged(nameof(RunAnalyzer));
            }
        }

        string mTestArtifactsFolder;
        public string TestArtifactsFolder
        {
            get
            {
                return mTestArtifactsFolder;
            }
            set
            {
                mTestArtifactsFolder = value;
                OnPropertyChanged(nameof(TestArtifactsFolder));
            }
        }


        RunsetExecutor mRunsetExecutor;
        //UserProfile WorkSpace.Instance.UserProfile;
        RunSetConfig mRunSetConfig;

        public bool LoadSolution()
        {
            try
            {
                Reporter.ToLog(eLogLevel.INFO, "Loading Solution...");
                // SetDebugLevel();//disabling because it is overwriting the UserProfile setting for logging level
                DownloadSolutionFromSourceControl();
                return OpenSolution();
            }
            catch (Exception ex)
            {
                Reporter.ToLog(eLogLevel.ERROR, "Unexpected error occurred while Loading the Solution", ex);
                return false;
            }
        }

        public bool LoadRunset(RunsetExecutor runsetExecutor)
        {
            try
            {
                Reporter.ToLog(eLogLevel.INFO, string.Format("Loading {0}", GingerDicser.GetTermResValue(eTermResKey.RunSet)));
                mRunsetExecutor = runsetExecutor;
                if (mRunsetExecutor.RunSetConfig == null)
                {
                    SelectRunset();
                }
                else
                {
                    mRunSetConfig = mRunsetExecutor.RunSetConfig;
                }
                SelectEnv();
                mRunSetConfig.RunWithAnalyzer = RunAnalyzer;
                HandleAutoRunWindow();
                return true;
            }
            catch (Exception ex)
            {
                Reporter.ToLog(eLogLevel.ERROR, string.Format("Unexpected error occurred while loading the {0}", GingerDicser.GetTermResValue(eTermResKey.RunSet)), ex);
                return false;
            }
        }

        public void PostExecution()
        {
            if (ShowAutoRunWindow)
            {
                RepositoryItemHelper.RepositoryItemFactory.WaitForAutoRunWindowClose();
            }
        }

        public bool PrepareRunsetForExecution()
        {
            try
            {
                Reporter.ToLog(eLogLevel.INFO, string.Format("Preparing {0} for Execution", GingerDicser.GetTermResValue(eTermResKey.RunSet)));

                if (!ShowAutoRunWindow)
                {
                    Reporter.ToLog(eLogLevel.INFO, string.Format("Loading {0} Runners", GingerDicser.GetTermResValue(eTermResKey.RunSet)));
                    mRunsetExecutor.InitRunners();
                }

                if (mRunSetConfig.RunWithAnalyzer)
                {
                    Reporter.ToLog(eLogLevel.INFO, string.Format("Running {0} Analyzer", GingerDicser.GetTermResValue(eTermResKey.RunSet)));
                    AnalyzerUtils analyzerUtils = new AnalyzerUtils();
                    if (analyzerUtils.AnalyzeRunset(mRunSetConfig, true))
                    {
                        Reporter.ToLog(eLogLevel.WARN, string.Format("Stopping {0} execution due to Analyzer issues", GingerDicser.GetTermResValue(eTermResKey.RunSet)));
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Reporter.ToLog(eLogLevel.ERROR, string.Format("Unexpected error occurred while preparing {0} for Execution", GingerDicser.GetTermResValue(eTermResKey.RunSet)), ex);
                return false;
            }
        }

        internal void SetTestArtifactsFolder()
        {
            if (!string.IsNullOrEmpty(mTestArtifactsFolder))
            {
                WorkSpace.Instance.TestArtifactsFolder = mTestArtifactsFolder;
            }
        }

        private void SetDebugLevel()
        {
            Reporter.AppLoggingLevel = AppLoggingLevel;
        }

        private void HandleAutoRunWindow()
        {
            if (ShowAutoRunWindow)
            {
                Reporter.ToLog(eLogLevel.INFO, "Showing Auto Run Window");
                RepositoryItemHelper.RepositoryItemFactory.ShowAutoRunWindow();
            }
            else
            {
                Reporter.ToLog(eLogLevel.INFO, "Not Showing Auto Run Window");
            }
        }


        private void SelectRunset()
        {
            Reporter.ToLog(eLogLevel.INFO, string.Format("Selected {0}: '{1}'", GingerDicser.GetTermResValue(eTermResKey.RunSet), Runset));
            ObservableList<RunSetConfig> RunSets = WorkSpace.Instance.SolutionRepository.GetAllRepositoryItems<RunSetConfig>();
            mRunSetConfig = RunSets.Where(x => x.Name.ToLower().Trim() == Runset.ToLower().Trim()).FirstOrDefault();
            if (mRunSetConfig != null)
            {
                mRunsetExecutor.RunSetConfig = mRunSetConfig;
            }
            else
            {
                Reporter.ToLog(eLogLevel.ERROR, string.Format("Failed to find matching {0} in the Solution", GingerDicser.GetTermResValue(eTermResKey.RunSet)));
                Reporter.ToUser(eUserMsgKey.CannotRunShortcut);
                // TODO: throw
                // return false;
            }
        }

        private void SelectEnv()
        {
            Reporter.ToLog(eLogLevel.DEBUG, "Selected Environment: '" + Env + "'");
            if (String.IsNullOrEmpty(Env) == false)
            {
                ProjEnvironment env = WorkSpace.Instance.SolutionRepository.GetAllRepositoryItems<ProjEnvironment>().Where(x => x.Name.ToLower().Trim() == Env.ToLower().Trim()).FirstOrDefault();
                if (env != null)
                {
                    mRunsetExecutor.RunsetExecutionEnvironment = env;
                    return;
                }
                else
                {
                    if (Env != "Default")
                    {
                        Reporter.ToLog(eLogLevel.ERROR, string.Format("Failed to find the Environment '{0}' in the Solution", Env));
                        throw new Exception(string.Format("Failed to find the Environment '{0}' in the Solution", Env));
                    }
                }
            }

            if (mRunsetExecutor.RunsetExecutionEnvironment == null && (String.IsNullOrEmpty(Env) || Env == "Default"))
            {
                if (WorkSpace.Instance.SolutionRepository.GetAllRepositoryItems<ProjEnvironment>().Count > 0)
                {
                    mRunsetExecutor.RunsetExecutionEnvironment = WorkSpace.Instance.SolutionRepository.GetAllRepositoryItems<ProjEnvironment>().First();
                    Reporter.ToLog(eLogLevel.INFO, "Auto Selected the default Environment: '" + mRunsetExecutor.RunsetExecutionEnvironment.Name + "'");
                }
                else
                {
                    Reporter.ToLog(eLogLevel.ERROR, "Cannot auto select default environment since solution do not contain any Environment, please add Environment to the Solution");
                    throw new Exception("Failed to find any Environment in the Solution");
                }
            }
        }

        private void DownloadSolutionFromSourceControl()
        {
            if (SourceControlURL != null && SourcecontrolUser != "" && sourceControlPass != null)
            {
                Reporter.ToLog(eLogLevel.INFO, "Downloading/updating Solution from source control");
                if (!SourceControlIntegration.DownloadSolution(Solution))
                {
                    Reporter.ToLog(eLogLevel.ERROR, "Failed to Download/update Solution from source control");
                }
            }
        }

        internal void SetSourceControlPassword(string value)
        {
            //Reporter.ToLog(eLogLevel.DEBUG, "Selected SourceControlPassword: '" + value + "'");//we should not show the password in log
            WorkSpace.Instance.UserProfile.SourceControlPass = value;
            sourceControlPass = value;
        }

        internal void PasswordEncrypted(string value)
        {
            Reporter.ToLog(eLogLevel.DEBUG, "PasswordEncrypted: '" + value + "'");
            string pswd = WorkSpace.Instance.UserProfile.SourceControlPass;
            if (value == "Y" || value == "true" || value == "True")
            {
                try
                {
                    pswd = EncryptionHandler.DecryptwithKey(WorkSpace.Instance.UserProfile.SourceControlPass);
                }
                catch(Exception ex)
                {
                    string mess = ex.Message; //To avoid warning of ex not used
                    Reporter.ToLog(eLogLevel.ERROR, "Failed to decrypt the source control password");//not showing ex details for not showing the password by mistake in log
                }
            }

            if (WorkSpace.Instance.UserProfile.SourceControlType == SourceControlBase.eSourceControlType.GIT && pswd == "")
            {
                pswd = "Test";
            }

            WorkSpace.Instance.UserProfile.SourceControlPass = pswd;
            sourceControlPass = pswd;
        }

        internal void SourceControlProxyPort(string value)
        {
            if (value == "")
            {
                WorkSpace.Instance.UserProfile.SolutionSourceControlConfigureProxy = false;
            }
            else
            {
                WorkSpace.Instance.UserProfile.SolutionSourceControlConfigureProxy = true;
            }

            Reporter.ToLog(eLogLevel.DEBUG, "Selected SourceControlProxyPort: '" + value + "'");
            WorkSpace.Instance.UserProfile.SolutionSourceControlProxyPort = value;
        }

        internal void SourceControlProxyServer(string value)
        {
            Reporter.ToLog(eLogLevel.DEBUG, "Selected SourceControlProxyServer: '" + value + "'");
            if (value == "")
            {
                WorkSpace.Instance.UserProfile.SolutionSourceControlConfigureProxy = false;
            }
            else
            {
                WorkSpace.Instance.UserProfile.SolutionSourceControlConfigureProxy = true;
            }

            if (value != "" && !value.ToUpper().StartsWith("HTTP://"))
            {
                value = "http://" + value;
            }

            WorkSpace.Instance.UserProfile.SolutionSourceControlProxyAddress = value;
        }

        internal void SetSourceControlUser(string value)
        {
            Reporter.ToLog(eLogLevel.DEBUG, "Selected SourceControlUser: '" + value + "'");
            if (WorkSpace.Instance.UserProfile.SourceControlType == SourceControlBase.eSourceControlType.GIT && value == "")
            {
                value = "Test";
            }

            WorkSpace.Instance.UserProfile.SourceControlUser = value;
            SourcecontrolUser = value;
        }

        internal void SetSourceControlURL(string value)
        {
            Reporter.ToLog(eLogLevel.DEBUG, "Selected SourceControlUrl: '" + value + "'");
            if (WorkSpace.Instance.UserProfile.SourceControlType == SourceControlBase.eSourceControlType.SVN)
            {
                if (!value.ToUpper().Contains("/SVN") && !value.ToUpper().Contains("/SVN/"))
                {
                    value = value + "svn/";
                }
                if (!value.ToUpper().EndsWith("/"))
                {
                    value = value + "/";
                }
            }
            WorkSpace.Instance.UserProfile.SourceControlURL = value;
            SourceControlURL = value;
        }

        internal void SetSourceControlType(string value)
        {
            Reporter.ToLog(eLogLevel.DEBUG, "Selected SourceControlType: '" + value + "'");
            if (value.Equals("GIT"))
            {
                WorkSpace.Instance.UserProfile.SourceControlType = SourceControlBase.eSourceControlType.GIT;
            }
            else if (value.Equals("SVN"))
            {
               WorkSpace.Instance.UserProfile.SourceControlType = SourceControlBase.eSourceControlType.SVN;
            }
            else
            {
                WorkSpace.Instance.UserProfile.SourceControlType = SourceControlBase.eSourceControlType.None;
            }
        }

        private bool OpenSolution()
        {
            try
            {
                return WorkSpace.Instance.OpenSolution(Solution);
            }
            catch (Exception ex)
            {
                Reporter.ToLog(eLogLevel.ERROR, "Failed to load the Solution");
                Reporter.ToLog(eLogLevel.ERROR, $"Method - {MethodBase.GetCurrentMethod().Name}, Error - {ex.Message}", ex);
                // TODO: throw
                return false;
            }
        }

        public void CloseSolution()
        {
            try
            {
                WorkSpace.Instance.CloseSolution();
            }
            catch (Exception ex)
            { 
                Reporter.ToLog(eLogLevel.ERROR, "Unexpected Error occurred while closing the Solution", ex);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
