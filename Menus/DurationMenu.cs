using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

namespace CS2_SimpleAdmin.Menus
{
	public static class DurationMenu
	{
		public static Tuple<string, int>[] _durations = new[]
		{
			new Tuple<string, int>("1 minute", 60),
			new Tuple<string, int>("5 minutes", 60 * 5),
			new Tuple<string, int>("15 minutes", 60 * 15),
			new Tuple<string, int>("1 hour", 60 * 60),
			new Tuple<string, int>("1 day", 60 * 60 * 24),
			new Tuple<string, int>("Permanent", 0)
		};

		public static void OpenMenu(CCSPlayerController admin, string menuName, CCSPlayerController player, Action<CCSPlayerController, CCSPlayerController, int> onSelectAction)
		{
			CenterHtmlMenu menu = new CenterHtmlMenu(menuName);

			foreach (Tuple<string, int> duration in _durations)
			{
				string optionName = duration.Item1;
				menu.AddMenuOption(optionName, (_, _) => { onSelectAction?.Invoke(admin, player, duration.Item2); });
			}

			MenuManager.OpenCenterHtmlMenu(CS2_SimpleAdmin.Instance, admin, menu);
		}
	}
}
