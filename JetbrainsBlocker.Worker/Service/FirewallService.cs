using JetbrainsBlocker.Worker.Exceptions;
using JetbrainsBlocker.Worker.Types;
using WindowsFirewallHelper;

namespace JetbrainsBlocker.Worker.Service;

internal sealed class FirewallService(ILogger<FirewallService> logger)
{
    public IEnumerable<IFirewallRule> FindRuleByFilePath(string filePath, FirewallDirection? direction = null)
    {
        if (!FirewallManager.TryGetInstance(out var firewall))
        {
            throw new FirewallInstanceUnavailableException();
        }

        var ruleName = new FirewallRuleName(filePath);
        var rules = firewall.Rules.Where(rule => rule.FriendlyName == ruleName);

        return direction is not null ? rules.Where(rule => rule.Direction == direction) : rules;
    }

    public bool IsRuleExists(string filePath, FirewallDirection? direction = null)
    {
        return FindRuleByFilePath(filePath, direction).Any();
    }

    public void CreateRule(string filePath, IEnumerable<IAddress> addresses,
        FirewallDirection direction = FirewallDirection.Outbound)
    {
        if (!FirewallManager.TryGetInstance(out var firewall))
        {
            throw new FirewallInstanceUnavailableException();
        }

        var rule = firewall.CreateApplicationRule(
            FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public,
            new FirewallRuleName(filePath),
            FirewallAction.Block,
            filePath
        );

        rule.RemoteAddresses = addresses.ToArray();
        rule.Direction = direction;

        logger.LogInformation(
            "Create firewall rule for application at {AppPath}, to drop {Direction} calls to {IpList}",
            filePath,
            direction.ToString("G"),
            addresses);

        firewall.Rules.Add(rule);
    }

    public void UpdateRuleAddresses(string filePath, IEnumerable<IAddress> addresses)
    {
        var rules = FindRuleByFilePath(filePath);
        var addr = addresses as IAddress[] ?? addresses.ToArray();

        foreach (var rule in rules)
        {
            rule.RemoteAddresses = addr;
        }
    }

    public void ClearBlockerRules()
    {
        if (!FirewallManager.TryGetInstance(out var firewall))
        {
            throw new FirewallInstanceUnavailableException();
        }

        var oldRules = firewall.Rules.Where(rule => rule.Name.StartsWith(FirewallRuleName.Prefix));
        foreach (var rule in oldRules)
        {
            firewall.Rules.Remove(rule);
        }
    }
}
