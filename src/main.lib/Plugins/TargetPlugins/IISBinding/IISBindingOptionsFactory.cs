﻿using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISBindingOptionsFactory : TargetPluginOptionsFactory<IISBinding, IISBindingOptions>
    {
        public override bool Hidden => !_iisClient.HasWebSites;
        private readonly IIISClient _iisClient;
        private readonly IISBindingHelper _helper;
        private readonly ILogService _log;
        private readonly IArgumentsService _arguments;

        public IISBindingOptionsFactory(
            ILogService log, IIISClient iisClient,
            IISBindingHelper helper, IArgumentsService arguments)
        {
            _iisClient = iisClient;
            _helper = helper;
            _log = log;
            _arguments = arguments;
        }

        public async override Task<IISBindingOptions> Aquire(IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISBindingOptions();
            var bindings = _helper.GetBindings(_arguments.MainArguments.HideHttps).Where(x => !x.Hidden);
            if (!bindings.Any())
            {
                _log.Error($"No sites with named bindings have been configured in IIS. Add one or choose '{ManualOptions.DescriptionText}'.");
                return null;
            }
            var chosenTarget = inputService.ChooseFromList(
                "Choose binding",
                bindings,
                x => Choice.Create(x),
                "Abort");
            if (chosenTarget != null)
            {
                ret.SiteId = chosenTarget.SiteId;
                ret.Host = chosenTarget.HostUnicode;
                return ret;
            }
            else
            {
                return null;
            }
        }

        public async override Task<IISBindingOptions> Default()
        {
            var ret = new IISBindingOptions();
            var args = _arguments.GetArguments<IISBindingArguments>();
            var hostName = _arguments.TryGetRequiredArgument(nameof(args.Host), args.Host).ToLower();
            var rawSiteId = args.SiteId;
            var filterSet = _helper.GetBindings(false);
            if (!string.IsNullOrEmpty(rawSiteId))
            {
                if (long.TryParse(rawSiteId, out var siteId))
                {
                    filterSet = filterSet.Where(x => x.SiteId == siteId).ToList();
                }
                else
                {
                    _log.Error("Invalid SiteId {siteId}", rawSiteId);
                    return null;
                }
            }
            var chosenTarget = filterSet.Where(x => x.HostUnicode == hostName || x.HostPunycode == hostName).FirstOrDefault();
            if (chosenTarget != null)
            {
                ret.SiteId = chosenTarget.SiteId;
                ret.Host = chosenTarget.HostUnicode;
                return ret;
            }
            else
            {
                return null;
            }
        }
    }
}