using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refunder
{
    class Options
    {
        [Option('m', "monetra", Required = true, HelpText = "Monetra host name or IP address")]
        public string MonetraHost { get; set; }

        [Option('p', "port", DefaultValue = 8444, HelpText = "Monetra port")]
        public int MonetraPort { get; set; }

        [Option('c', "connection-string", Required = true)]
        public string ConnectionString { get; set; }

        [Option('n', "non-interactive")]
        public bool NonInteractive { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
