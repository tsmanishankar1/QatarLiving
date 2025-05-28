using MudBlazor;

public class CustomTheme : MudTheme
{
    public CustomTheme()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#FF7F38" // Your custom primary color
        };
    }
}