using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;
using WindowsFirewallHelper.COMInterop;
public class Program {
	const string AccountUrl = "account.jetbrains.com";
	private static bool IsAdministrationRules() {
		try {
			using (WindowsIdentity identity = WindowsIdentity.GetCurrent()) {
				return (new WindowsPrincipal(identity)).IsInRole(WindowsBuiltInRole.Administrator);
			}
		} catch {
			return false;
		}
	}
	public static int Main (string[] args) {
		// check if we're on windows
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Error("This program requires administrative privileges.");
			Error("Press any key to exit...");
			return 196;
			// Do something
		}
		if (!IsAdministrationRules()) {
			//Console.ForegroundColor = ConsoleColor.Red;
			Error("This program requires administrative privileges.");
			Error("Press any key to exit...");
			Console.ReadKey();
			return 5;
		}
		
		Console.WriteLine("Enter the path to the rider executable. Make sure to include the .exe extension.");
		string? riderPath = Console.ReadLine();
		while (!File.Exists(riderPath) || !riderPath.EndsWith(".exe")) {
			Error("Invalid path, try again. Make sure to include the .exe extension.");
			riderPath = Console.ReadLine();
		}
		Console.ForegroundColor = ConsoleColor.White;
		try {
			// clean up 
			// this is to ensure that we don't have multiple rules with the same name, and bloat the firewall rules
			var oldRule = FirewallManager.Instance.Rules.FirstOrDefault(r => r.Name == "JetbrainsBlocker");
			if (oldRule != null) {
				FirewallManager.Instance.Rules.Remove(oldRule);
			}
			// create a new rule
			var newAppRule = FirewallManager.Instance.CreateApplicationRule(
				// ensure this rule is applied to all profiles
				FirewallProfiles.Private | FirewallProfiles.Public | FirewallProfiles.Domain,
				// give it a name 
				"JetbrainsBlocker",
				// block rider
				FirewallAction.Block,
				// set the path to the rider executable
				riderPath);

			// add IP addresses to block
			IPAddress[] ipAddresses = GetIPs();
			IAddress[] addresses = new IAddress[ipAddresses.Length];
			for (int i = 0; i < ipAddresses.Length; i++) {
				addresses[i] = new SingleIP(ipAddresses[i]);
			}

			newAppRule.RemoteAddresses = addresses;
			// make it an outbound rule
			newAppRule.Direction = FirewallDirection.Outbound;
			// add it
			FirewallManager.Instance.Rules.Add(newAppRule);
			
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("Successfully blocked JetBrains.");
			Console.WriteLine("Press any key to exit...");
			Console.ReadKey();
			Console.ForegroundColor = ConsoleColor.White;
			return 0;
		}
		catch (Exception e) {
			Error("An error occurred while blocking JetBrains.");
			Error("== Error ==");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(e.Message);
			Console.ForegroundColor = ConsoleColor.White;
			return 5;
		}
	}

	static void Error(string message) {
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine(message);
		Console.ForegroundColor = ConsoleColor.White;
	}
	
	static IPAddress[] GetIPs() {
		IPHostEntry hostEntry = Dns.GetHostEntry(AccountUrl);
		return hostEntry.AddressList.ToArray();
	}
}