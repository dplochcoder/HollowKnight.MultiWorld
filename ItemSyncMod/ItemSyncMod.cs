﻿using ItemSyncMod.Menu;
using ItemSyncMod.Randomizer;
using MenuChanger.MenuElements;
using MenuChanger;
using Modding;
using RandomizerMod.RC;
using UnityEngine;
using UnityEngine.SceneManagement;
using ItemSyncMod.ICDL;

namespace ItemSyncMod
{
	public class ItemSyncMod : Mod, IGlobalSettings<GlobalSettings>, ILocalSettings<ItemSyncSettings>, IMenuMod
	{
		public static GlobalSettings GS { get; private set; } = new();
		public static ItemSyncSettings ISSettings { get; set; } = new();
		internal static BaseController Controller { get; set; }

        public static ClientConnection Connection;

		internal static bool RecentItemsInstalled = false;

		private static Dictionary<MenuPage, ItemSyncMenu> MenuInstances = new();

		public override string GetVersion()
		{
			string ver = "2.7.1";

#if (DEBUG)
			ver += "-Debug";
#endif
			return ver;
		}
		
		public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
		{
			base.Initialize();

			LogDebug("ItemSync Initializing...");

			UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnMainMenu;
			
			Connection = new();

			RandomizerMod.Menu.RandomizerMenuAPI.AddStartGameOverride(
				page => MenuInstances[page] = new(page),
				(RandoController rc, MenuPage landingPage, out BaseButton button) =>
				{
					var menu = MenuInstances[landingPage];
                    Controller = new ItemSyncRandoController(rc, menu);
					return menu.GetMenuButton(out button);
				});

            if (ModHooks.GetMod("ICDL Mod") is Mod) ICDLInterop.Hook(MenuInstances);

            On.GameManager.ContinueGame += (orig, self) =>
            {
                orig(self);

				foreach (var menu in MenuInstances.Values) menu.Dispose();
				MenuInstances.Clear();
            };

            RecentItemsInstalled = ModHooks.GetMod("RecentItems") is Mod;
        }

        private void OnMainMenu(Scene from, Scene to)
        {
			if (to.name != "Menu_Title") return;

			Controller?.SessionSyncUnload();
			Connection = new();
		}

		public void OnLoadGlobal(GlobalSettings s)
        {
			GS = s ?? new();
		}

        public GlobalSettings OnSaveGlobal()
        {
			return GS;
        }

        public void OnLoadLocal(ItemSyncSettings s)
        {
			ISSettings = s;

			if (ISSettings.IsItemSync)
            {
				Connection.Connect(ISSettings.URL);
				Controller.SessionSyncSetup();
            }
        }

        public ItemSyncSettings OnSaveLocal()
        {
			if (ISSettings.IsItemSync) Connection.NotifySave();

			return ISSettings;
        }

		public bool ToggleButtonInsideMenu => false;
		public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
			string[] recentItemsInfoOptions = { "Both", "Sender Only", "Area Only" };
			List<IMenuMod.MenuEntry> modMenuEntries = new()
			{
				new IMenuMod.MenuEntry
                {
					Name = "Corner Pop-up Info",
					Description = "Info shown for received items (in bottom left corner)",
					Values = new string[] { "With Sender", "Item Only" },
					Saver = opt => GS.CornerMessagePreference = opt == 0 ? GlobalSettings.InfoPreference.Both : GlobalSettings.InfoPreference.ItemOnly,
					Loader = () => (int)(GS.CornerMessagePreference == GlobalSettings.InfoPreference.Both ? GlobalSettings.InfoPreference.Both : GlobalSettings.InfoPreference.ItemOnly)
                }
			};

			if (RecentItemsInstalled)
				modMenuEntries.Add(new IMenuMod.MenuEntry
				{
					Name = "Recent Items Info",
					Description = "Info shown for received items (recent items)",
					Values = recentItemsInfoOptions,
					Saver = opt => GS.RecentItemsPreference = (GlobalSettings.InfoPreference)opt,
					Loader = () => (int)GS.RecentItemsPreference
				});

			return modMenuEntries;
        }
    }
}
