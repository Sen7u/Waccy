using Inkboard.Domain.Rules;

namespace Inkboard.Tests.Domain;

public class IgnoreRulesTests
{
    [Fact]
    public void ShouldSkip_MatchesIgnoredApp_CaseInsensitive()
    {
        var rules = new IgnoreRules(["keepass", "1Password"]);

        Assert.True(rules.ShouldSkip("KeePass"));
        Assert.False(rules.ShouldSkip("notepad"));
        Assert.False(rules.ShouldSkip(null));
    }
}
