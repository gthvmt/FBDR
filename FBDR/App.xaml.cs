using Prism.Ioc;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FBDR
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell() => Container.Resolve<Views.Shell>();

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<Models.Options>();
            //throw new NotImplementedException();
            containerRegistry.RegisterForNavigation<Views.MainView>();
            containerRegistry.RegisterForNavigation<Views.OptionsView>();
        }

        protected override void Initialize()
        {
            base.Initialize();
            var options = Container.Resolve<Models.Options>();
            Directory.CreateDirectory(Path.GetDirectoryName(options.FilePath));
            if (File.Exists(options.FilePath))
            {
                var loadedOptions = Models.Serializer.ReadFromBinaryFile<Models.Options>(options.FilePath);
                foreach (var property in typeof(Models.Options).GetProperties().Where(p => p.CanWrite))
                {
                    property.SetValue(options, property.GetValue(loadedOptions, null), null);
                }
            }
            else
            {
                options.ResetAll();
            }

            //Container.Resolve<IRegionManager>().RegisterViewWithRegion<Views.MainView>(RegionNames.ContentRegion);
            Container.Resolve<IRegionManager>().RegisterViewWithRegion<Views.StatusBar>(RegionNames.StatusBarRegion);
            Container.Resolve<IRegionManager>().RequestNavigate(RegionNames.ContentRegion, nameof(Views.MainView));
        }
    }
}
