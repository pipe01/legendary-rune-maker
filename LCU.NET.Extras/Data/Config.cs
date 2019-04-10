using Anotar.Log4Net;
using Legendary_Rune_Maker.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;

namespace Legendary_Rune_Maker.Data
{
	public class Config
	{
		const string DEFAULT_PROVIDER = "RiftGG";
		public static Config Current { get => Container.Value; set => Container.Value = value; }
		public static Container<Config> Container { get; } = new Container<Config> (Load ());

		private const string FilePath = "config.json";
		private const int LatestVersion = 14;

		public static readonly string[] AvailableLanguages = new[]
		{
			"en-US",
			"es-ES"
		};

		public int ConfigVersion { get; set; } = LatestVersion;

		public bool CheckUpdatesBeforeStartup => false;
		public string CultureName { get; set; }
		public bool MinimizeToTaskbar { get; set; } = true;

		public bool AutoAccept { get; set; } = true;

		public bool UploadOnLock => true;
		public bool LoadRunesOnLock { get; set; } = true;
		public bool LoadSpellsOnLock { get; set; } = true;
		public string LockLoadProvider => DEFAULT_PROVIDER;

		public bool AutoPickChampion { get; set; }
		public bool DisablePickChampion { get; set; } = true;

		public bool AutoBanChampion { get; set; }
		public bool DisableBanChampion { get; set; } = true;

		public bool AutoPickSumms { get; set; }
		public bool DisablePickSumms { get; set; } = true;

		public bool SetItemSet => false;
		public string ItemSetProvider => DEFAULT_PROVIDER;
		public bool KeepItemSets { get; set; }
		public string LastItemSetUid { get; set; }

		public int LastRunePageId { get; set; }

		public bool ShowSkillOrder => false;
		public string SkillOrderProvider => DEFAULT_PROVIDER;

		public int DelayBeforeAction { get; set; } = 3000;
		public int DelayBeforeAcceptReady { get; set; } = 4000;
		public int DelayBeforeIntentSet { get; set; } = 4000;


		public Dictionary<Position, int> ChampionsToPick { get; set; } = new Dictionary<Position, int> {
			[Position.Fill] = 0,
			[Position.Top] = 0,
			[Position.Jungle] = 0,
			[Position.Mid] = 0,
			[Position.Bottom] = 0,
			[Position.Support] = 0
		};
		public Dictionary<Position, int> ChampionsToBan { get; set; } = new Dictionary<Position, int> {
			[Position.Fill] = 0,
			[Position.Top] = 0,
			[Position.Jungle] = 0,
			[Position.Mid] = 0,
			[Position.Bottom] = 0,
			[Position.Support] = 0
		};
		public Dictionary<Position, int[]> SpellsToPick { get; set; } = new Dictionary<Position, int[]> {
			[Position.Fill] = new int[2],
			[Position.Top] = new int[2],
			[Position.Jungle] = new int[2],
			[Position.Mid] = new int[2],
			[Position.Bottom] = new int[2],
			[Position.Support] = new int[2]
		};

		[JsonIgnore]
		public CultureInfo Culture => new CultureInfo (this.CultureName);


		public void Save ()
		{
			LogTo.Info ("Saving config");
			LogTo.Debug (() => JsonConvert.SerializeObject (this));

			try {
				File.WriteAllText (FilePath, JsonConvert.SerializeObject (this, Formatting.Indented));
			} catch {
				throw;
			}
		}

		public Config Clone ()
		{
			return JsonConvert.DeserializeObject<Config> (JsonConvert.SerializeObject (this));
		}

		public static void Reload () => Current = Load ();

		private static Config Load ()
		{
			Config c;

			try {
				if (!File.Exists (FilePath)) {
					c = new Config ();
				} else {
					c = JsonConvert.DeserializeObject<Config> (File.ReadAllText (FilePath));
				}
			} catch {
				throw;
			}

			if (c.CultureName == null)
				c.CultureName = CultureInfo.CurrentCulture.Name;

			if (c.ConfigVersion < LatestVersion)
				c.ConfigVersion = LatestVersion;

			c.Save ();
			return c;
		}
	}
}
