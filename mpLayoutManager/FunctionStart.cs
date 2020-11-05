namespace mpLayoutManager
{
    using System;
    using System.Drawing;
    using System.Reflection;
    using System.Windows.Forms;
    using System.Windows.Forms.Integration;
    using Autodesk.AutoCAD.Runtime;
    using Autodesk.AutoCAD.Windows;
    using ModPlus;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

    public class FunctionStart : IExtensionApplication
    {
        private const string LangItem = "mpLayoutManager";

        private static PaletteSet _paletteSet;

        private void _paletteSet_Load(object sender, PalettePersistEventArgs e)
        {
            e.ConfigurationSection.ReadProperty("mpLayoutManager", 22.3);
        }

        private void _paletteSet_Save(object sender, PalettePersistEventArgs e)
        {
            e.ConfigurationSection.WriteProperty("mpLayoutManager", 32.3);
        }

        public static void AddToMpPalette(bool show)
        {
            PaletteSet mpPaletteSet = MpPalette.MpPaletteSet;
            if (mpPaletteSet != null)
            {
                bool flag = false;
                foreach (Palette palette in mpPaletteSet)
                {
                    if (palette.Name.Equals(Language.GetItem(LangItem, "h8")))
                    {
                        flag = true;
                    }
                }

                if (!flag)
                {
                    LmPalette lmPalette = new LmPalette();
                    mpPaletteSet.Add(Language.GetItem(LangItem, "h8"), new ElementHost
                    {
                        AutoSize = true,
                        Dock = DockStyle.Fill,
                        Child = lmPalette
                    });
                    if (show)
                    {
                        mpPaletteSet.Visible = true;
                    }
                }
            }

            if (_paletteSet != null)
            {
                _paletteSet.Visible = false;
            }
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains("ModPlus_"))
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                Start();
            }

            return null;
        }

        public void Initialize()
        {
            var loadLayoutManager = bool.TryParse(UserConfigFile.GetValue("mpLayoutManager", "AutoLoad"), out bool b) & b;
            var addLayoutManagerToMpPalette = bool.TryParse(UserConfigFile.GetValue("mpLayoutManager", "AddToMpPalette"), out b) & b;
            if (loadLayoutManager & !addLayoutManagerToMpPalette)
            {
                Start();
            }
            else if (loadLayoutManager & addLayoutManagerToMpPalette)
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
        }

        public static void RemoveFromMpPalette(bool fromSettings)
        {
            PaletteSet mpPaletteSet = MpPalette.MpPaletteSet;
            if (mpPaletteSet != null)
            {
                int num = 0;
                while (num < mpPaletteSet.Count)
                {
                    if (!mpPaletteSet[num].Name.Equals(Language.GetItem(LangItem, "h8")))
                    {
                        num++;
                    }
                    else
                    {
                        mpPaletteSet.Remove(num);
                        break;
                    }
                }
            }

            if (_paletteSet != null)
            {
                _paletteSet.Visible = true;
            }
            else if (fromSettings)
            {
                if (AcApp.DocumentManager.MdiActiveDocument != null)
                {
                    AcApp.DocumentManager.MdiActiveDocument.SendStringToExecute("_MPLAYOUTMANAGER ", true, false, false);
                }
            }
        }

        [CommandMethod("ModPlus", "mpLayoutManager", CommandFlags.Modal)]
        public void Start()
        {
#if !Debug
            Statistic.SendCommandStarting(new ModPlusConnector());
#endif
            try
            {
                if (!(!bool.TryParse(UserConfigFile.GetValue("mpLayoutManager", "AddToMpPalette"), out bool b) | b))
                {
                    RemoveFromMpPalette(false);
                    if (_paletteSet != null)
                    {
                        _paletteSet.Visible = true;
                    }
                    else
                    {
                        _paletteSet = new PaletteSet("MP:" + Language.GetItem(LangItem, "h8"), "mpLayoutManager", new Guid("CC48331E-B912-44DF-B592-D5EF66D7673E"));
                        _paletteSet.Load += _paletteSet_Load;
                        _paletteSet.Save += _paletteSet_Save;
                        LmPalette lmPalette = new LmPalette();
                        ElementHost elementHost = new ElementHost()
                        {
                            AutoSize = true,
                            Dock = DockStyle.Fill,
                            Child = lmPalette
                        };
                        _paletteSet.Add("MP:" + Language.GetItem(LangItem, "h8"), elementHost);
                        _paletteSet.Style = PaletteSetStyles.ShowCloseButton | PaletteSetStyles.ShowPropertiesMenu | PaletteSetStyles.ShowAutoHideButton;
                        _paletteSet.MinimumSize = new Size(100, 300);
                        _paletteSet.DockEnabled = DockSides.Right | DockSides.Left;
                        _paletteSet.Visible = true;
                    }
                }
                else
                {
                    if (_paletteSet != null)
                    {
                        _paletteSet.Visible = false;
                    }

                    AddToMpPalette(true);
                }
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        public void Terminate()
        {
        }
    }
}