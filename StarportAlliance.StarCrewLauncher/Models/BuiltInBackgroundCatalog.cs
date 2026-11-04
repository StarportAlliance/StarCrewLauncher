namespace StarportAlliance.StarCrewLauncher.Models;

internal static class BuiltInBackgroundCatalog
{
    public static IReadOnlyList<BuiltInBackground> All { get; } =
    [
        new(
            "FungleSunset",
            "Appearance_BackgroundImageFungleSunset",
            "ms-appx:///Assets/BuiltinBackground/FungleSunset.png"
        ),
        new(
            "CrewmateDuo",
            "Appearance_BackgroundImageCrewmateDuo",
            "ms-appx:///Assets/BuiltinBackground/CrewmateDuo.jpg"
        ),
        new(
            "ImpostorMonth",
            "Appearance_BackgroundImageImpostorMonth",
            "ms-appx:///Assets/BuiltinBackground/ImpostorMonth.webp"
        ),
        new(
            "CrewmateRoam",
            "Appearance_BackgroundImageCrewmateRoam",
            "ms-appx:///Assets/BuiltinBackground/CrewmateRoam.jpg"
        ),
    ];

    public static BuiltInBackground Default => All[0];

    public static BuiltInBackground Resolve(string? id) =>
        All.FirstOrDefault(background =>
            string.Equals(background.Id, id, StringComparison.OrdinalIgnoreCase)
        ) ?? Default;
}
