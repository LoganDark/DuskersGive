# DuskersGive

A proof-of-concept that Duskers is moddable just like any other Unity game.

---

[This Steam discussion](https://steamcommunity.com/app/254320/discussions/0/144512526672881009/) claims that modding Duskers is not possible. While there is no official mod loader or Workshop support (especially not on the GOG edition), Duskers is a Unity game written in C#, which means it is subject to C#'s super-high-level-ness.

Here is my four-hour journey from wanting to mod the game to having a fully functional mod loader and mod. Given more time you could definitely make a much more complex mod and even combine multiple mods for a very unique Duskers experience. My next mod will probably be adding a drone upgrade and exploring how to make it balanced and save/load.

---

I've always known Duskers is a Unity game. The window it pops up on startup is a sure sign, and so is the structure of the game files, so I started out by looking for generic Unity mod loaders. A quick search led me to Nexus Mods and the excellent [Unity Mod Manager](https://www.nexusmods.com/site/mods/21).

Unfortunately, Nexus Mods wants you to sign into an account to download anything, so I had to spend like 20 minutes trying to sign up and figuring out why my username was already taken by someone who was not me, then I had to use a password different than my regular one because even though mine is 23 characters and has symbols in it, the password form demanded uppercase letters as well... ended up saying 'screw it' and using Chrome's auto-generation because I don't care too much about that account.

Anyway, once I downloaded UMM and opened it up, I discovered that support had to be added to each game individually. So I opened up the [guide for adding a new game](https://wiki.nexusmods.com/index.php/How_to_add_new_game_(UMM)), and mostly shamefully stole the entry for Muck except I changed the main menu references to match Duskers.

I used [dnSpy](https://github.com/dnSpy/dnSpy/releases/) to verify that the EntryPoint was still present in Duskers (just so you don't spend as much time as I did trying to figure it out, the actual game code is inside of `Assembly-CSharp.dll`), and then used it to find the `MainMenu` class and its `Initialize` method.

I then promptly added that into `<StartingPoint>` and `<UIStartingPoint>`. I would recommend holding onto dnSpy as it will be your main reverse engineering tool while trying to figure out how the game works and how to perform certain actions.

dnSpy protips:
- You can ctrl+click on identifiers to open them in a new tab.
- Tabs can't be closed by ctrl+w, it's ctrl+f4, this is stupid and even using the close button with your mouse is less annoying.
- You can use ctrl+shift+r to "analyze" a method, which adds it to the Analyzer panel at the bottom and allows you to find out what calls it.
- Alt+left can take you back if you click without using ctrl

Here is the entry I settled on (proposal [here](https://github.com/newman55/unity-mod-manager/issues/85)). If it hasn't been added officially into UMM yet, add it manually to `UnityModManagerConfig.xml`, at the end of the top-level `<Config>` tag, then save and restart UMM:

```xml
<GameInfo Name="Duskers">
	<Folder>Duskers</Folder>
	<ModsDirectory>Mods</ModsDirectory>
	<ModInfo>Info.json</ModInfo>
	<GameExe>Duskers.exe</GameExe>
	<EntryPoint>[UnityEngine.UI.dll]UnityEngine.EventSystems.EventSystem.cctor:After</EntryPoint>
	<StartingPoint>[Assembly-CSharp.dll]MainMenu.Initialize:After</StartingPoint>
	<UIStartingPoint>[Assembly-CSharp.dll]MainMenu.Initialize:After</UIStartingPoint>
	<Comment>Definition by LoganDark</Comment>
	<MinimalManagerVersion>0.23.5</MinimalManagerVersion>
</GameInfo>
```

I selected Duskers in the dropdown, selected its folder, kept DoorstopProxy selected, and clicked Install. Easy enough. Once I loaded up Duskers, I was greeted with the Unity Mod Manager menu, which meant it was installed correctly. First try!! Next step was to get the mod itself set up. First, I created my json file, in `Duskers/Mods/DuskersGive/Info.json`:

```json
{
	"Id": "DuskersGive",
	"DisplayName": "Give",
	"Author": "LoganDark",
	"Version": "1.0.0",
	"EntryMethod": "DuskersGive.Main.Load"
}
```

Then, I set up the Visual Studio project. I had Visual Studio 2019 installed because I needed to do literally anything that has to do with executables (grr microsoft), so I created a new "Class Library (.NET Framework)". **I didn't know this at the time, but you have to use .NET 3.5 for Duskers.** More on that later. Luckily since I'm a total chad I have all of those libraries on my machine anyway, but you might have to fire up Visual Studio Installer just for .NET 3.5 if you don't have it already (sorry! this is a five-year-old game).

Then I added all of the assemblies from Duskers and UnityModManager:

- Duskers/Duskers_Data/Managed/Assembly-CSharp.dll (contains the Duskers code)
- Duskers/Duskers_Data/Managed/Assembly-CSharp-firstpass.dll (the tutorial said so)
- Duskers/Duskers_Data/Managed/UnityEngine.dll (the engine)
- Duskers/Duskers_Data/Managed/UnityEngine.UI.dll (if you want to manipulate UI elements)
- Duskers/Duskers_Data/Managed/UnityEngine.Networking.dll (because it was in the folder)
- UnityModManager/UnityModManager.dll
- UnityModManager/0Harmony.dll

I changed the `public class` into a `static class`, renamed it to `Main`, and added the Load and OnToggle methods. Then, I used dnSpy to guide my efforts in reversing the game. I found out where commands were registered. It was from some XML resource. I figured that I could just do the same thing but without an XML file, and it would work just fine.

Well, the issue that popped up is CommandHelper.GetCommands will give me a dummy list if I provide it a category that doesn't exist.

```cs
	public static List<CommandDefinition> GetCommands(string commandGroup)
	{
		if (CommandHelper._commandLookup == null)
		{
			CommandHelper.Initialize();
		}
		if (CommandHelper._commandLookup.ContainsKey(commandGroup))
		{
			return CommandHelper._commandLookup[commandGroup];
		}
		Debug.Log("GetCommands has no definitions for: " + commandGroup);
		return new List<CommandDefinition>();
	}
```

See how the new list is not added anywhere? That means any commands you put in there won't be recognized by the game. That's not good.

Because I didn't know what categories *did* exist, I didn't know which string to give to `GetCommands` to make it work. So I started searching for that XML file where the categories were being created from.

Long story short, I found it in `Duskers/Duskers_Data/resources.assets`. It's a packed file but you can just open it in Notepad++ and search for `commandGroup` to find the XML portion. It's not compressed or obfuscated in any way.

Here's an example snippet:

```xml
	<CommandContext commandGroup="DungeonManager">
		<CommandDefinition name="a" description="toggles specified airlock(s)" example="a1 a2" shortcut="true">
			<DetailedDescription message="&#x9;&#x9;Opens or closes one or more specified airlocks, so long as they are powered." format="SmallFont" />
		</CommandDefinition>
		
		<CommandDefinition name="open" description="opens specified door(s)" example="open d1 d2">
			<DetailedDescription message="&#x9;&#x9;'open all'      Open all powered doors" format="SmallFont" />
			<DetailedDescription message="&#x9;&#x9;'open r12 r13'  Open all of a room's doors" format="SmallFont" />
			<DetailedDescription message="&#x9;&#x9;'d1 d2'			'open' not required for door" format="SmallFont" />
		</CommandDefinition>

		...
```

And here's the code that loads it:

```cs
	private static void LoadCommandDefinitionLibrary()
	{
		TextAsset textAsset = (TextAsset)Resources.Load("Data/CommandDefinitions");
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(textAsset.text);
		XmlNodeList xmlNodeList = xmlDocument.SelectNodes("//CommandDefinitions/CommandContext");
		foreach (object obj in xmlNodeList)
		{
			XmlNode xmlNode = (XmlNode)obj;
			List<CommandDefinition> list = new List<CommandDefinition>();
			CommandHelper._commandLookup.Add(xmlNode.Attributes["commandGroup"].Value, list);
			foreach (object obj2 in xmlNode.ChildNodes)
			{
				XmlNode node = (XmlNode)obj2;
				CommandDefinition commandDefinitionFromXml = CommandHelper.GetCommandDefinitionFromXml(node);
				if (commandDefinitionFromXml != null)
				{
					list.Add(commandDefinitionFromXml);
				}
			}
		}
	}

	private static CommandDefinition GetCommandDefinitionFromXml(XmlNode node)
	{
		if (node.Attributes["name"] == null)
		{
			return null;
		}
		string targetNumberString = ConsoleCommandTarget.Undefined.ToString();
		if (node.Attributes["commandTarget"] != null && !string.IsNullOrEmpty(node.Attributes["commandTarget"].Value))
		{
			targetNumberString = node.Attributes["commandTarget"].Value;
		}
		CommandDefinition commandDefinition = new CommandDefinition(node.Attributes["name"].Value, (node.Attributes["description"] == null) ? string.Empty : node.Attributes["description"].Value, (node.Attributes["example"] == null) ? string.Empty : node.Attributes["example"].Value, targetNumberString, (node.Attributes["devCmd"] == null) ? "false" : node.Attributes["devCmd"].Value, (node.Attributes["internal"] == null) ? "false" : node.Attributes["internal"].Value, (node.Attributes["shortcut"] == null) ? "false" : node.Attributes["shortcut"].Value, (node.Attributes["tag"] == null) ? string.Empty : node.Attributes["tag"].Value, (node.Attributes["isAdvanced"] == null) ? string.Empty : node.Attributes["isAdvanced"].Value, (node.Attributes["hideFromManual"] == null) ? string.Empty : node.Attributes["hideFromManual"].Value, (node.Attributes["helpOnly"] == null) ? "false" : node.Attributes["helpOnly"].Value, (node.Attributes["hideFromAutoComplete"] == null) ? "false" : node.Attributes["hideFromAutoComplete"].Value);
		List<ConsoleMessage> list = new List<ConsoleMessage>();
		if (node.ChildNodes != null && node.ChildNodes.Count > 0)
		{
			foreach (object obj in node.ChildNodes)
			{
				XmlNode xmlNode = (XmlNode)obj;
				if (xmlNode.Name != "CommandUpgradeMod")
				{
					ConsoleMessage consoleMessageFromXml = CommandHelper.GetConsoleMessageFromXml(xmlNode);
					if (consoleMessageFromXml != null)
					{
						list.Add(consoleMessageFromXml);
					}
				}
				else
				{
					if (commandDefinition.ModList == null)
					{
						commandDefinition.ModList = new List<CommandMod>();
					}
					CommandMod item = new CommandMod(xmlNode.Attributes["name"].Value, (xmlNode.Attributes["description"] == null) ? string.Empty : xmlNode.Attributes["description"].Value, (xmlNode.Attributes["example"] == null) ? string.Empty : xmlNode.Attributes["example"].Value, (xmlNode.Attributes["symbol"] == null) ? string.Empty : xmlNode.Attributes["symbol"].Value);
					commandDefinition.ModList.Add(item);
				}
			}
		}
		commandDefinition.DetailedDescription.AddRange(list);
		return commandDefinition;
	}
```

`GetCommandDefinitionFromXml` is kind of ridiculous but it doesn't matter. Moral of the story is that the XML just tells regular imperative code what to do so I can just do the exact same thing and it'll work just fine. Now I know what the categories are named, but where are these commands executed? Clearly the XML doesn't say what they should do, only where and what they are.

DungeonManager was a hint. I took a look inside and found DungeonManager.ExecuteCommand, which has code like:

```cs
	public void ExecuteCommand(ExecutedCommand command, bool partOfMultiCommand)
	{
		string commandName = command.Command.CommandName;
		switch (commandName)
		{
		case "alias":
			command.Handled = true;
			// ...
			break;
		case "degauss":
			command.Handled = true;
			// ...
		case "static":
			command.Handled = true;
			// ...
			break;
		// ...
		}
	}
```

Looked perfect.

I found a debug command `tree` in there, and just for fun, I tried to add it back into the commands list when my mod was loaded to see if it would work:

```cs
static bool OnToggle(UnityModManager.ModEntry entry, bool active) {
	if (active) {
		command = new CommandDefinition("tree", "shows the comman tree", "tree", ConsoleCommandTarget.Undefined);
		CommandHelper.GetCommands("DungeonManager").Add(command);
	} else {
		CommandHelper.GetCommands("DungeonManager").Remove(command);
	}

	return true;
}
```

It did, no patching required. Now that I knew more about the command infrastructure in Duskers, I kept digging and seeking to make my own custom command, completely from scratch.

However, I couldn't find the `navigate` command in there. I wanted my command to behave like navigate where you could specify drones and it would execute for them independently. I kept looking around and found another `ExecuteCommand` inside of the `Drone` class (which I found by looking for implementers of the `ICommandable` interface):

```cs
	public void ExecuteCommand(ExecutedCommand command, bool partOfMultiCommand)
	{
		// <death check...>
		// <gather check...>
		// <stun check...>
		// <command chaining check...>
		string commandName2 = command.Command.CommandName;
		switch (commandName2)
		{
		case "navigate":
			command.Handled = true;
			// ...
		}
	}
```

So how this works is when you run something like `navigate 1 2 r1`, it actually runs `navigate r1` on both drones individually. That's why if you do something like `navigate 1 2 x`, it prints the error message twice. Since this matched the behavior of the base game, I didn't mind doing this myself.

I switched to the super long `CommandDefinition` constructor, named it `give`, and added a patch (that's the entire `OnExecuteCommand` class) to `Drone.ExecuteCommand` to do the execution. I used `DungeonManager.Instance.SendConsoleMessage` to print to console. It worked!

The way the game handles command execution actually lends perfectly to modding. You can slap a handler on the end and you don't need to mess with return values or anything. That's why my Postfix is so clean and doesn't have to do any manipulation.

But anyway, what should we do inside that function? It took me a while to find the registry for upgrade definitions. I did this by looking at how the crafting logic worked. It grabs things out of `DroneUpgradeFactory.UpgradeDefinitions`, so I can too.

All it took was some intellisense to discover the methods for adding upgrades to the drone. I could use the `DroneUpgradeFactory` to create a brand new instance of the upgrade and then call `Drone.AddDroneUpgrade` to add it to the drone. So that's exactly what I did.

I also switched to using `ConsoleWindow3.SendConsoleResponse` directly since that is how all of the `SendConsoleMessage` methods worked. It works pretty well.

Of course, throughout all of this I was building my DLL, dropping it inside of the `Duskers/Mods/DuskersGive` folder, and starting Duskers into the Drone Operator Training to test it.

## Possible errors

You can bring up the UMM menu with Ctrl+F10, and it also pops up on startup. If you get a red dot and a `!!!` next to your mod, then it failed to load. Go to log and see what it says.

### TargetInvocationException

If it says TargetInvocationException, then you probably used a .NET version other than 3.5, like I did. If you open the detailed log it will say something dumb like "System.TypeLoadException: Could not load type 'System.Func`3' from assembly 'mscorlib".

In the very top left of Visual Studio, to the left of Main.cs and Object Browser, there's your project name. Click on it and you'll find the project settings. This took far too long for me to find and was very annoying to find out. But then you can just change "Target framework" to ".NET Framework 3.5" and rebuild your DLL and put it in your mod folder and it should load. (Remember to delete the cached DLL every time you replace it.)

### Says "Internal error processing command!!" in chat

That means your command threw an exception of some kind. UMM hooks into the Unity logs (or so I would assume) so you should have the full error with stack trace in the detailed log.

## Conclusion

For me, modding Duskers was 66% starting from scratch and writing an entire mod, and 33% writing about the entire experience afterwards. I hope that this will inspire people to make mods for the game, since it definitely is possible.
