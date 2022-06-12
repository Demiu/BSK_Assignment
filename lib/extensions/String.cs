
public static class StringExtension 
{
    public static bool PathContainsSubPath(this string super, string sub) {
        if (!super.EndsWith('/')) { // TODO replace '/' with a constant
            super = $"{super}/";
        }
        var combined = Path.Combine(super, sub);
        var uriSuper = new Uri(super);
        var uriCombined = new Uri(combined);
        return uriSuper.IsBaseOf(uriCombined);
    }
}