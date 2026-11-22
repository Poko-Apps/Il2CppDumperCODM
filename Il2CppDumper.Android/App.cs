using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Il2CppDumper.Mobile
{
    public class App : Application
    {
        protected override Window CreateWindow(IActivationState activationState)
        {
            return new Window(new NavigationPage(new MainPage()));
        }
    }
}
