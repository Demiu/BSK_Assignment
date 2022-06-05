
public static class StringExtension {
    public static bool PathContainsSubPath(this string super, string sub) {
        var combined = Path.Join(super, sub);
        var uriSuper = new Uri(super);
        var uriCombined = new Uri(combined);
        return uriSuper.IsBaseOf(uriCombined);
    }
}