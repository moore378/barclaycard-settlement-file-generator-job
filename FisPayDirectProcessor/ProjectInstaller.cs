using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.ServiceProcess;
using System.Configuration;
using System.Configuration.Install;

namespace FisPayDirectProcessor
{
    // Provide the ProjectInstaller class which allows 
    // the service to be installed by the Installutil.exe tool
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller _process;
        private ServiceInstaller _service;

        public ProjectInstaller()
        {
            _process = new ServiceProcessInstaller();
            _process.Account = ServiceAccount.LocalSystem;
            _service = new ServiceInstaller();
            _service.ServiceName = Constants.SERVICE_NAME;
            Installers.Add(_process);
            Installers.Add(_service);
        }
    }
}
